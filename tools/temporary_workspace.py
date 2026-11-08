"""Owned scratch space for the Python installer verifier; no installed-profile access."""
import hashlib
import json
import os
import re
from pathlib import Path
import shutil
import uuid
import zipfile


def sha(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def safe_path(root, path):
    root, path = Path(os.path.abspath(root)), Path(os.path.abspath(path))
    if not path.is_relative_to(root / "out") or path == root / "out":
        raise ValueError("Scratch path must be below out")
    for parent in [path, *path.parents]:
        if parent == root:
            break
        if parent.exists() or parent.is_symlink():
            if parent.is_symlink() or getattr(parent.stat(follow_symlinks=False), "st_file_attributes", 0) & 0x400:
                raise ValueError("Scratch path contains a link")
    return path


def files(root, directory):
    safe_path(root, directory)
    pending = [Path(directory)]
    while pending:
        for item in pending.pop().iterdir():
            if item.is_symlink() or getattr(item.stat(follow_symlinks=False), "st_file_attributes", 0) & 0x400:
                raise ValueError("Scratch tree contains a link")
            if item.is_dir():
                pending.append(item)
            else:
                yield item


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


class Workspace:
    def __init__(self, root, keep=False, sources=()):
        self.root = Path(root).absolute()
        self.id = uuid.uuid4().hex
        self.token = uuid.uuid4().hex
        self.keep = keep
        self.sources = list(sources)
        self.path = safe_path(self.root, self.root / "out/w" / self.id[:8])
        self.evidence = safe_path(self.root, self.root / "out/reports/package" / self.id)

    def __enter__(self):
        self.path.mkdir(parents=True)
        self.evidence.mkdir(parents=True)
        self.lease = (self.path / ".lease").open("xb+")
        self.owner = dict(schema="keyjaya-work-1", kind="package", id=self.id,
                          token=self.token, pid=os.getpid(), started_ticks=0, keep=self.keep)
        write_json(self.path / "owner.json", self.owner)
        write_json(self.evidence / "inputs.json", {
            str(p.relative_to(self.root)): sha(p) for p in self.sources
        })
        return self

    def snapshot(self):
        candidates = sorted(files(self.root, self.path), key=lambda p: (not bool({"config", "state"} & set(p.parts)), str(p)))
        candidates = [(p, "source/" + p.relative_to(self.root).as_posix()) for p in self.sources] + [
            (p, "work/" + p.relative_to(self.path).as_posix()) for p in candidates
        ]
        inventory, total = [], 0
        with zipfile.ZipFile(self.evidence / "failure.zip", "w", zipfile.ZIP_DEFLATED) as bundle:
            for p, name in candidates:
                size = p.stat().st_size
                capture = size <= 1024**2 and total + size <= 16 * 1024**2 and p.suffix.lower() not in {
                    ".exe", ".dll", ".pdb", ".dat", ".idx", ".gz", ".zip"
                } and p.name not in {"owner.json", ".lease"}
                row = dict(path=name, bytes=size, captured=capture)
                if capture:
                    row["sha256"] = sha(p)
                    bundle.write(p, name)
                    total += size
                inventory.append(row)
            bundle.writestr("inventory.json", json.dumps(inventory).encode())
        with zipfile.ZipFile(self.evidence / "failure.zip") as bundle:
            for row in inventory:
                if row["captured"] and hashlib.sha256(bundle.read(row["path"])).hexdigest() != row["sha256"]:
                    raise ValueError("Failure snapshot differs")

    def __exit__(self, error_type, error, traceback):
        success, cleanup, problem = error is None, "kept", None
        try:
            safe_path(self.root, self.path)
            owner = json.loads(safe_path(self.root, self.path / "owner.json").read_text(encoding="utf-8"))
            if owner != self.owner:
                raise ValueError("Workspace ownership changed")
            if error is not None:
                write_json(self.evidence / "failure.json", dict(error=str(error)))
                self.snapshot()
            list(files(self.root, self.path))
            self.lease.close()
            if not self.keep:
                shutil.rmtree(self.path)
                cleanup = "removed"
        except Exception as cleanup_error:
            cleanup, problem = "preserved_after_error", str(cleanup_error)
        finally:
            self.lease.close()
        from datetime import datetime, timezone
        inventory = {p.relative_to(self.evidence).as_posix(): sha(p) for p in files(self.root, self.evidence) if p.parent != self.evidence or p.name not in {"report.json", "report.sha256"}}
        write_json(self.evidence / "report.json", dict(
            schema="keyjaya-report-1", kind="package", id=self.id, finished=True,
            success=success, pinned=self.keep, finished_utc=datetime.now(timezone.utc).isoformat(),
            cleanup=cleanup, cleanup_error=problem, files=inventory,
        ))
        (self.evidence / "report.sha256").write_text(sha(self.evidence / "report.json"), encoding="utf-8")
        self.prune()
        if problem and success:
            raise RuntimeError("Temporary workspace preserved: " + problem)
        return False

    def prune(self):
        reports = []
        for p in self.evidence.parent.iterdir():
            try:
                if re.fullmatch(r"[a-f0-9]{32}", p.name) is None:
                    continue
                safe_path(self.root, p)
                list(files(self.root, p))
                if sha(p / "report.json") != (p / "report.sha256").read_text().strip().lower():
                    continue
                report = json.loads((p / "report.json").read_text(encoding="utf-8"))
                if report["schema"] == "keyjaya-report-1" and report["kind"] == "package" and report["id"] == p.name and report["finished"] and not report["pinned"] and not (p / ".keep").exists():
                    reports.append((p, report))
            except (OSError, ValueError, KeyError):
                continue
        reports.sort(key=lambda item: item[1]["finished_utc"], reverse=True)
        keep = {p for p, _ in reports[:5]}
        failure = next((p for p, r in reports if not r["success"]), None)
        if failure is not None:
            keep.add(failure)
        for p, report in reports:
            if p in keep:
                continue
            try:
                actual = {f.relative_to(p).as_posix(): sha(f) for f in files(self.root, p) if f.parent != p or f.name not in {"report.json", "report.sha256"}}
                if actual == {name: digest.lower() for name, digest in report["files"].items()}:
                    shutil.rmtree(p)
            except (OSError, ValueError):
                # Modified or linked evidence is never implicitly adopted.
                continue
