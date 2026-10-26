"""Exercise build-tool boundaries using disposable files, never a live installation.

Run with: python -m unittest discover -s tests/python -v
The installer and network are mocked. All fixtures remain beneath ignored out/.
"""

import contextlib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import shutil
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

ROOT = Path(__file__).resolve().parents[2]


def load_tool(name, relative):
    spec = importlib.util.spec_from_file_location(name, ROOT / relative)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


package = load_tool("keyjaya_package", "package.py")
docs = load_tool("keyjaya_docs", "tools/check_docs.py")
sources = load_tool("keyjaya_sources", "tools/fetch_hindi_sources.py")
verify = load_tool("keyjaya_verify", "tools/verify_build.py")


class FixtureTest(unittest.TestCase):
    def setUp(self):
        output = ROOT / "out/python-tests"
        output.mkdir(parents=True, exist_ok=True)
        temporary = tempfile.TemporaryDirectory(dir=output)
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.write(
            "assets/distribution-exclusions.txt",
            (ROOT / "assets/distribution-exclusions.txt").read_bytes(),
        )

    def write(self, relative, data):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        if isinstance(data, str):
            data = data.encode("utf-8")
        path.write_bytes(data)
        return path


class ImportTests(unittest.TestCase):
    def test_renderer_help_does_not_require_pillow(self):
        renderer = load_tool("keyjaya_renderer_help", "tools/render_readme_demo.py")
        with (
            patch.dict(sys.modules, {"PIL": None}),
            contextlib.redirect_stdout(io.StringIO()),
        ):
            with self.assertRaises(SystemExit) as stopped:
                renderer.main(["--help"])
        self.assertEqual(stopped.exception.code, 0)

    def test_imports_do_not_parse_arguments_write_output_or_require_pillow(self):
        paths = [
            "package.py",
            "tools/audit_project.py",
            "tools/check_docs.py",
            "tools/fetch_hindi_sources.py",
            "tools/verify_build.py",
            "tools/render_readme_demo.py",
        ]
        output = io.StringIO()
        with (
            contextlib.redirect_stdout(output),
            patch.object(sys, "argv", ["tool", "--invalid"]),
            patch.dict(sys.modules, {"PIL": None}),
            patch(
                "pathlib.Path.write_bytes",
                side_effect=AssertionError("Import wrote bytes"),
            ),
            patch(
                "pathlib.Path.write_text",
                side_effect=AssertionError("Import wrote text"),
            ),
            patch(
                "urllib.request.urlopen",
                side_effect=AssertionError("Import used network"),
            ),
            patch(
                "subprocess.run", side_effect=AssertionError("Import launched process")
            ),
        ):
            for index, path in enumerate(paths):
                with self.subTest(path=path):
                    load_tool("keyjaya_import_" + str(index), path)
        self.assertEqual(output.getvalue(), "")


class PackageTests(FixtureTest):
    def prepare(self, version="1.2.3"):
        self.write("src/Core.cs", 'class Version { const string Current = "1.2.3"; }')
        self.write("release/VERSION", version)
        self.write("release/engine/keyjaya-app.exe", b"fixture engine")

    def test_payload_inventory_bytes_and_exclusions(self):
        self.prepare()
        self.write("release/config/short-triggers.yml", "matches: []")
        denied = [
            "logs/run.log",
            "state/preferences",
            "backups/old.yml",
            "config/personal-triggers.yml",
            "config/personal_triggers.yml",
            "data/english-guard.txt",
            "data/hindi-build-counts.txt",
            "config/settings.ini.bak",
            "config/temp.tmp",
            "config/x.before-1",
            "config/x.tmp-1",
            "config/x.backup-1/inside.yml",
        ]
        for name in denied:
            self.write("release/" + name, "excluded fixture")
        archive = self.root / "payload.zip"
        package.create_payload(self.root / "release", archive, self.root)
        expected = ["VERSION", "config/short-triggers.yml", "engine/keyjaya-app.exe"]
        with zipfile.ZipFile(archive) as payload:
            self.assertEqual(set(payload.namelist()), set(expected))
            for name in expected:
                self.assertEqual(
                    payload.read(name), (self.root / "release" / name).read_bytes()
                )

    def test_version_mismatch_does_not_create_archive(self):
        self.prepare("9.9.9")
        archive = self.root / "payload.zip"
        with self.assertRaisesRegex(ValueError, "version"):
            package.create_payload(self.root / "release", archive, self.root)
        self.assertFalse(archive.exists())

    def test_source_only_notices_omitted_and_keyjaya_license_retained(self):
        self.prepare()
        self.write("release/LICENSE", "KeyJaya copyright and MIT notice")
        excluded = package.distribution_exclusions(self.root)
        for relative in excluded:
            self.write("release/" + relative, "source-only fixture")
        archive = self.root / "payload.zip"
        package.create_payload(self.root / "release", archive, self.root)
        with zipfile.ZipFile(archive) as payload:
            self.assertFalse(
                excluded.intersection(name.casefold() for name in payload.namelist())
            )
            self.assertEqual(
                payload.read("LICENSE"), b"KeyJaya copyright and MIT notice"
            )

    def test_distribution_policy_cannot_remove_runtime_or_user_files(self):
        for invalid in [
            "LICENSE",
            "config/short-triggers.yml",
            "engine/keyjaya-app.exe",
            "../outside.txt",
            "data/../config/settings.ini",
            "data/conversion.dat",
            "data/conversion.IDX",
            "data/english.hash",
            "data/words.tsv",
            "data/..",
            "data/name\tbad.txt",
            "data/C:notice.txt",
            "data/notice?.txt",
            "data/conversion.dat.",
            "data/ENGLIS~1.HAS",
            "data/NOTICE.txt\nDATA/notice.TXT",
        ]:
            with self.subTest(invalid=invalid):
                self.write("assets/distribution-exclusions.txt", invalid + "\n")
                with self.assertRaises(ValueError):
                    package.distribution_exclusions(self.root)

    def test_missing_source_version_is_explicit_error(self):
        self.prepare()
        self.write("src/Core.cs", "// no version")
        with self.assertRaisesRegex(ValueError, "version"):
            package.create_payload(
                self.root / "release", self.root / "payload.zip", self.root
            )


class DocumentationTests(FixtureTest):
    def test_relative_encoded_and_external_links(self):
        self.write("docs/a file.md", "target")
        self.write(
            "README.md",
            "[local](docs/a%20file.md#part) [web](https://example.com) [anchor](#here)",
        )
        self.write("out/ignored.md", "[bad](missing)")
        self.assertEqual(docs.check_markdown_links(self.root), 1)

    def test_missing_link_is_rejected(self):
        self.write("README.md", "[bad](missing.md)")
        with self.assertRaisesRegex(ValueError, "Broken link"):
            docs.check_markdown_links(self.root)

    def test_escaping_link_is_rejected(self):
        self.write("README.md", "[bad](../outside.md)")
        with self.assertRaisesRegex(ValueError, "Nonportable link"):
            docs.check_markdown_links(self.root)

    def test_supported_image_headers(self):
        self.write(
            "docs/images/test.png",
            b"\x89PNG\r\n\x1a\n" + b"\0" * 8 + struct.pack(">II", 20, 30),
        )
        self.write(
            "docs/images/test.gif", b"GIF89a" + struct.pack("<HH", 40, 50) + b";"
        )
        self.assertEqual(docs.check_image_headers(self.root), 2)

    def test_invalid_image_headers_are_rejected(self):
        for name, data in [
            ("bad.png", b"short"),
            ("bad.gif", b"GIF89a"),
            ("bad.png", b"\x89PNG\r\n\x1a\n" + b"\0" * 8 + struct.pack(">II", 0, 30)),
            ("bad.gif", b"GIF89a" + struct.pack("<HH", 40, 50)),
        ]:
            with self.subTest(name=name, data=data):
                path = self.write("docs/images/" + name, data)
                with self.assertRaises(ValueError):
                    docs.check_image_headers(self.root)
                path.unlink()


class HindiSourceTests(FixtureTest):
    def manifest(self, **changes):
        data = b"pinned dictionary fixture"
        item = {
            "name": "hi_IN.dic",
            "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest(),
            "url": "https://raw.githubusercontent.com/LibreOffice/dictionaries/abc/hi_IN/hi_IN.dic",
        }
        item.update(changes)
        self.write(
            "data/hindi-source-manifest.json",
            json.dumps({"commit": "abc", "files": [item]}),
        )
        return data

    def test_check_missing_source_makes_no_writes_or_network_requests(self):
        self.manifest()
        with patch.object(
            sources.urllib.request, "urlopen", side_effect=AssertionError("network")
        ):
            with self.assertRaises(SystemExit):
                sources.ensure_sources(self.root, check_only=True)
        self.assertFalse((self.root / "third_party").exists())

    def test_check_valid_source_preserves_bytes(self):
        data = self.manifest()
        path = self.write("third_party/hindi/hi_IN.dic", data)
        before = path.stat().st_mtime_ns
        with patch.object(
            sources.urllib.request, "urlopen", side_effect=AssertionError("network")
        ):
            self.assertEqual(sources.ensure_sources(self.root, check_only=True), 1)
        self.assertEqual(path.read_bytes(), data)
        self.assertEqual(path.stat().st_mtime_ns, before)

    def test_rejects_unpinned_url_and_nonlocal_filename(self):
        for changes in [
            {"url": "https://example.com/unpinned"},
            {"name": "../outside.dic"},
        ]:
            self.manifest(**changes)
            with (
                self.subTest(changes=changes),
                patch.object(sources.urllib.request, "urlopen") as network,
            ):
                with self.assertRaises(ValueError):
                    sources.ensure_sources(self.root)
                network.assert_not_called()

    def test_fetch_verifies_before_replacing(self):
        data = self.manifest()
        path = self.write("third_party/hindi/hi_IN.dic", b"old bytes")
        for response in [b"truncated", b"x" * len(data)]:
            with (
                self.subTest(response=response),
                patch.object(
                    sources.urllib.request, "urlopen", return_value=io.BytesIO(response)
                ),
            ):
                with self.assertRaises(ValueError):
                    sources.ensure_sources(self.root)
            self.assertEqual(path.read_bytes(), b"old bytes")
        with patch.object(
            sources.urllib.request, "urlopen", return_value=io.BytesIO(data)
        ):
            self.assertEqual(sources.ensure_sources(self.root), 1)
        self.assertEqual(path.read_bytes(), data)
        self.assertFalse(path.with_name(path.name + ".download-tmp").exists())


class BuildVerificationTests(FixtureTest):
    def prepare_payload(self):
        self.write("VERSION", "1.2.3")
        self.write("src/Core.cs", 'class Version { const string Current = "1.2.3"; }')
        self.write("out/app/VERSION", "1.2.3")
        self.write("out/app/engine/keyjaya-app.exe", b"not executable")
        self.write("out/app/engine/keyjaya-app.exe.config", "framework settings")
        self.write("out/app/config/short-triggers.yml", "matches: []\n")
        for name in verify.NOTICES:
            self.write(name, "fixture notice " + name)
            self.write("out/app/" + name, "fixture notice " + name)
        self.repack()

    def repack(self):
        package.create_payload(
            self.root / "out/app", self.root / "out/payload.zip", self.root
        )

    def test_payload_hashes_detect_tampered_bytes(self):
        self.prepare_payload()
        self.assertEqual(
            len(
                verify.verify_payload(
                    self.root, self.root / "out/app", self.root / "out/payload.zip"
                )
            ),
            5,
        )
        self.write("out/app/VERSION", "changed")
        with self.assertRaises(ValueError):
            verify.verify_payload(
                self.root, self.root / "out/app", self.root / "out/payload.zip"
            )

    def test_private_state_and_extra_engine_files_are_rejected(self):
        self.prepare_payload()
        extra = self.write("out/app/engine/obsolete.dll", b"obsolete")
        self.repack()
        with self.assertRaisesRegex(ValueError, "engine inventory"):
            verify.verify_payload(
                self.root, self.root / "out/app", self.root / "out/payload.zip"
            )
        extra.unlink()
        self.write("out/app/state/private.txt", "private")
        self.repack()
        with self.assertRaises(ValueError):
            verify.verify_payload(
                self.root, self.root / "out/app", self.root / "out/payload.zip"
            )

    def test_source_only_file_in_staging_is_rejected(self):
        self.prepare_payload()
        self.write("out/app/THIRD_PARTY_NOTICES.md", "source-only fixture")
        self.repack()
        with self.assertRaisesRegex(ValueError, "Source-only"):
            verify.verify_payload(
                self.root, self.root / "out/app", self.root / "out/payload.zip"
            )

    def test_retirement_fixture_preserves_manifest_path_casing(self):
        paths = verify.source_only_files(self.root)
        self.assertIn("THIRD_PARTY_NOTICES.md", paths)
        self.assertNotIn("third_party_notices.md", paths)

    def test_fresh_target_refuses_existing_and_escaping_paths(self):
        (self.root / "out/existing").mkdir(parents=True)
        for target in [
            Path("out"),
            Path("outside"),
            Path("out/../../escape"),
            Path("out/existing"),
        ]:
            with self.subTest(target=target), self.assertRaises(ValueError):
                verify.fresh_target(self.root, target)
        self.assertEqual(
            verify.fresh_target(self.root, Path("out/fresh")), self.root / "out/fresh"
        )

    def test_clean_install_and_upgrade_with_mock_installer(self):
        self.prepare_payload()

        def install_fixture(installer, target, fail_after_swap=False):
            if not target.exists():
                shutil.copytree(self.root / "out/app", target)

        with patch.object(
            verify, "install_once", side_effect=install_fixture
        ) as installer:
            result = verify.verify_build(self.root, Path("out/install-fixture"))
        self.assertEqual(installer.call_count, 3)
        self.assertTrue(result["upgrade_preserves_trigger_edit"])
        self.assertTrue(result["rollback_restores_all_files"])
        self.assertFalse(result["live_installation"])
        self.assertIn(
            "LOCALVERIFY",
            (self.root / "out/install-fixture/config/short-triggers.yml").read_text(),
        )

    def test_incomplete_rollback_is_reported(self):
        self.prepare_payload()

        def broken_rollback(installer, target, fail_after_swap=False):
            if not target.exists():
                shutil.copytree(self.root / "out/app", target)
            if fail_after_swap:
                (target / "state").mkdir(exist_ok=True)
                (target / "state/leaked-migration.flag").write_text("not rolled back")

        with patch.object(verify, "install_once", side_effect=broken_rollback):
            with self.assertRaisesRegex(ValueError, "Rollback changed"):
                verify.verify_build(self.root, Path("out/install-fixture"))

    def test_fault_must_reach_the_post_swap_checkpoint(self):
        import subprocess

        for code, message, valid in [
            (1, "Test-only failure after installation swap", True),
            (1, "Earlier setup error", False),
            (0, "Test-only failure after installation swap", False),
        ]:
            with (
                self.subTest(code=code, message=message),
                patch.object(
                    verify.subprocess,
                    "run",
                    return_value=subprocess.CompletedProcess([], code, "", message),
                ),
            ):
                if valid:
                    verify.install_once(
                        Path("fixture.exe"), Path("out/fixture"), fail_after_swap=True
                    )
                else:
                    with self.assertRaises(ValueError):
                        verify.install_once(
                            Path("fixture.exe"),
                            Path("out/fixture"),
                            fail_after_swap=True,
                        )

    def test_failed_upgrade_is_reported(self):
        self.prepare_payload()

        def overwrite_fixture(installer, target):
            shutil.copytree(self.root / "out/app", target, dirs_exist_ok=True)

        with patch.object(verify, "install_once", side_effect=overwrite_fixture):
            with self.assertRaisesRegex(ValueError, "Upgrade changed"):
                verify.verify_build(self.root, Path("out/install-fixture"))

    def test_target_guard_stays_active_under_python_optimization(self):
        path = ROOT / "tools/verify_build.py"
        namespace = {"__file__": str(path), "__name__": "optimized_verifier"}
        # optimize=2 removes assert statements just as -OO does, without starting
        # another interpreter or depending on a particular Windows launcher.
        exec(
            compile(path.read_text(encoding="utf-8"), str(path), "exec", optimize=2),
            namespace,
        )
        with self.assertRaisesRegex(ValueError, "Verification target must stay below"):
            namespace["fresh_target"](self.root, Path("outside"))


if __name__ == "__main__":
    unittest.main()
