"""Verify payload bytes and installation behavior in a fresh fixture beneath out/.

Running this tool executes the installer in its --test-install mode, with a fresh
target beneath out/. It never uses the installed profile, starts the resident,
or registers startup. --help does not execute an installer.
"""

import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parent.parent
NOTICES = ("LICENSE",)


def require(condition: bool, message: str) -> None:
    """Keep safety/integrity checks active even when Python runs with -O."""
    if not condition:
        raise ValueError(message)


def package_hashes(app: Path) -> dict[str, str]:
    return {
        path.relative_to(app).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest()
        for path in app.rglob("*")
        if path.is_file()
    }


def source_only_files(root: Path) -> frozenset[str]:
    # Load packaging policy from this checkout, not from an arbitrary module on PATH.
    import importlib.util

    spec = importlib.util.spec_from_file_location(
        "keyjaya_payload_policy", ROOT / "package.py"
    )
    policy = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(policy)
    normalized = policy.distribution_exclusions(root)
    # Packaging compares case-insensitively; rollback fixtures must use the
    # original manifest spelling, just as a real older installation does.
    paths = frozenset(
        line.strip()
        for line in (root / "assets/distribution-exclusions.txt")
        .read_text(encoding="utf-8-sig")
        .splitlines()
        if line.strip() and not line.lstrip().startswith("#")
    )
    require(
        {name.casefold() for name in paths} == normalized,
        "Distribution policy changed during verification",
    )
    return paths


def verify_payload(root: Path, app: Path, payload: Path) -> dict[str, str]:
    """Prove inventory and contents before executing an isolated installation."""
    files = package_hashes(app)
    excluded = {name.casefold() for name in source_only_files(root)}
    require(
        not excluded.intersection(name.casefold() for name in files),
        "Source-only files in application tree",
    )
    with zipfile.ZipFile(payload) as archive:
        require(set(archive.namelist()) == set(files), "Package inventory mismatch")
        for name, digest in files.items():
            require(hashlib.sha256(archive.read(name)).hexdigest() == digest, name)
    require(
        {name for name in files if name.startswith("engine/")}
        == {"engine/keyjaya-app.exe", "engine/keyjaya-app.exe.config"},
        "Unexpected engine inventory",
    )
    for name in NOTICES:
        require(
            (app / name).read_bytes() == (root / name).read_bytes(),
            "Missing or changed notice: " + name,
        )
    for name in files:
        require(
            not name.startswith(("logs/", "state/", "backups/")),
            "Private package data: " + name,
        )
    return files


def fresh_target(root: Path, requested: Path) -> Path:
    """Refuse existing fixtures and every target outside the ignored output tree."""
    target = (root / requested).resolve()
    output = (root / "out").resolve()
    require(
        target.is_relative_to(output) and target != output,
        "Verification target must stay below out/.",
    )
    require(
        not target.exists(),
        "Preserve an existing install fixture; choose a fresh verification directory.",
    )
    return target


def install_once(installer: Path, target: Path, fail_after_swap: bool = False) -> None:
    arguments = [str(installer), "--test-install", str(target)]
    if fail_after_swap:
        arguments.append("--fail-after-swap")
    result = subprocess.run(
        arguments,
        capture_output=True,
        text=True,
        timeout=90,
    )
    message = result.stdout + result.stderr
    require(result.returncode == (1 if fail_after_swap else 0), message)
    if fail_after_swap:
        require(
            "Test-only failure after installation swap" in message,
            "Installer failed before the rollback checkpoint: " + message,
        )


def verify_build(root: Path, requested: Path) -> dict[str, object]:
    """Verify clean install, edited-trigger upgrade, and exact failure rollback."""
    root = root.resolve()
    app = root / "out/app"
    version = (root / "VERSION").read_text().strip()
    installer = root / f"out/KeyJaya-Setup-v{version}.exe"
    files = verify_payload(root, app, root / "out/payload.zip")
    target = fresh_target(root, requested)
    target.parent.mkdir(parents=True, exist_ok=True)
    require(
        target.resolve().is_relative_to((root / "out").resolve()),
        "Verification target escaped out/.",
    )

    install_once(installer, target)
    require(
        (target / "VERSION").read_text().strip() == version,
        "Installed version mismatch",
    )
    require(
        (target / "engine/keyjaya-app.exe.config").read_bytes()
        == (app / "engine/keyjaya-app.exe.config").read_bytes(),
        "Installed Framework configuration mismatch",
    )
    for name in NOTICES:
        require(
            (target / name).read_bytes() == (root / name).read_bytes(),
            "Installed notice mismatch: " + name,
        )

    triggers = target / "config/short-triggers.yml"
    triggers.write_text(
        triggers.read_text(encoding="utf-8-sig")
        + "\n  - trigger: 'LOCALVERIFY'\n    replace: 'Keep this fixture edit'\n",
        encoding="utf-8",
    )
    before = triggers.read_bytes()
    install_once(installer, target)
    require(triggers.read_bytes() == before, "Upgrade changed user triggers")
    # Seed obsolete files only in this freshly created fixture. The failing
    # installer must retire them and then restore their exact bytes and markers.
    for relative in source_only_files(root) | {
        "assets/short-trigger-additions.yml",
        "data/short-triggers.tsv",
    }:
        path = target / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(("Previous factory file: " + relative).encode("utf-8"))
    rollback_before = package_hashes(target)
    install_once(installer, target, fail_after_swap=True)
    require(
        package_hashes(target) == rollback_before,
        "Rollback changed installation files or migration markers",
    )
    summary = {
        "version": version,
        "package_files": len(files),
        "fresh_install": True,
        "upgrade_preserves_trigger_edit": True,
        "rollback_restores_all_files": True,
        "rollback_files_verified": len(rollback_before),
        "live_installation": False,
        "failures": 0,
    }
    (target.parent / "build-results.json").write_text(
        json.dumps(summary, indent=2), encoding="utf-8"
    )
    print(
        f"PASS {len(files)} packaged file hashes; isolated clean install, edited-trigger upgrade and {len(rollback_before)} rollback file hashes; live app untouched."
    )
    return summary


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--target",
        type=Path,
        default=ROOT / "out/verification/install",
        help="Fresh disposable installation directory beneath out/. Existing directories are refused.",
    )
    args = parser.parse_args(argv)
    verify_build(ROOT, args.target)


if __name__ == "__main__":
    main()
