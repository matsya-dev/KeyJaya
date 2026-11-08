"""Exercise cleanup against synthetic project roots, including both Windows shells."""
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("keyjaya_temporary", ROOT / "tools/temporary_workspace.py")
temporary = importlib.util.module_from_spec(spec)
spec.loader.exec_module(temporary)


class WorkspaceTests(unittest.TestCase):
    def setUp(self):
        parent = ROOT / "out/python-tests"
        parent.mkdir(parents=True, exist_ok=True)
        fixture = tempfile.TemporaryDirectory(dir=parent)
        self.addCleanup(fixture.cleanup)
        self.root = Path(fixture.name)

    def test_success_and_failure_clean_only_owned_scratch(self):
        outside = self.root / "source.txt"
        outside.write_text("preserved")
        with temporary.Workspace(self.root) as work:
            (work.path / "extra-test-artifacts").mkdir()
            (work.path / "extra-test-artifacts/copied.dat").write_bytes(b"copy")
        self.assertFalse(work.path.exists())
        self.assertTrue((work.evidence / "report.json").is_file())
        with self.assertRaisesRegex(ValueError, "test failure"):
            with temporary.Workspace(self.root) as failed:
                (failed.path / "config").mkdir()
                (failed.path / "config/settings.ini").write_text("unique settings")
                (failed.path / "copy.dat").write_bytes(b"regenerable")
                raise ValueError("test failure")
        self.assertFalse(failed.path.exists())
        with zipfile.ZipFile(failed.evidence / "failure.zip") as z:
            self.assertEqual(z.read("work/config/settings.ini"), b"unique settings")
            self.assertNotIn("work/copy.dat", z.namelist())
        self.assertEqual(outside.read_text(), "preserved")

    def test_keep_artifacts_is_explicit_and_reports_are_bounded(self):
        with temporary.Workspace(self.root, keep=True) as kept:
            (kept.path / "inspect.txt").write_text("keep")
        self.assertTrue(kept.path.is_dir())
        for _ in range(9):
            with temporary.Workspace(self.root):
                pass
        self.assertLessEqual(len(list(kept.evidence.parent.iterdir())), 6)
        self.assertTrue(kept.evidence.is_dir())

    def test_changed_owner_and_escape_are_refused(self):
        with self.assertRaisesRegex(RuntimeError, "ownership"):
            with temporary.Workspace(self.root) as work:
                marker = json.loads((work.path / "owner.json").read_text())
                marker["token"] = "different"
                (work.path / "owner.json").write_text(json.dumps(marker))
        self.assertTrue(work.path.exists())
        for path in [self.root / "out", self.root / "out/../source"]:
            with self.assertRaises(ValueError):
                temporary.safe_path(self.root, path)

    def test_annotated_evidence_is_preserved(self):
        with temporary.Workspace(self.root) as work:
            pass
        path = work.evidence / "report.json"
        marker = json.loads(path.read_text())
        marker["user_note"] = "preserve this investigation"
        path.write_text(json.dumps(marker))
        for _ in range(8):
            with temporary.Workspace(self.root):
                pass
        self.assertTrue(path.exists())

    def test_user_added_nested_report_is_preserved(self):
        with temporary.Workspace(self.root) as work:
            pass
        extra = work.evidence / "notes/report.json"
        extra.parent.mkdir()
        extra.write_text("manually added evidence")
        for _ in range(8):
            with temporary.Workspace(self.root):
                pass
        self.assertEqual(extra.read_text(), "manually added evidence")

    def test_runner_compiler_failure_retains_error_and_cleans_fixture(self):
        shell = shutil.which("powershell")
        if shell is None:
            self.skipTest("Windows PowerShell unavailable")
        for name in ["test.ps1", "tools/workspace.ps1"]:
            target = self.root / name
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(ROOT / name, target)
        for name, text in {
            "build-common.ps1": "$compiler=Join-Path $PSScriptRoot 'bad-compiler.ps1'; $runtime=@(); $references=@()",
            "bad-compiler.ps1": "Write-Output 'deliberate compiler failure'; $global:LASTEXITCODE=17",
            "tests/suites.json": '["EllipsisTests"]',
            "tests/csharp/EllipsisTests.cs": "class Dummy {}",
            "tests/fixtures/english-guard.txt": "English",
            "assets/short-trigger-defaults.yml": "matches: []",
            "defaults/settings.ini": "preserve failure configuration",
            "data/manifest.json": "{}",
        }.items():
            p = self.root / name
            p.parent.mkdir(parents=True, exist_ok=True)
            p.write_text(text)
        result = subprocess.run(
            [shell, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(self.root / "test.ps1"), "-Suite", "EllipsisTests"],
            capture_output=True, text=True, timeout=60,
            env={k: v for k, v in os.environ.items() if k.lower() != "psmodulepath"},
        )
        self.assertNotEqual(result.returncode, 0)
        latest = json.loads((self.root / "out/tests/latest.json").read_text())
        evidence = Path(latest["evidence"])
        self.assertIn("deliberate compiler failure", (evidence / "EllipsisTests-compiler.txt").read_text())
        self.assertFalse(Path(latest["workspace"]).exists())
        results = json.loads((self.root / "out/tests/results.json").read_text())
        self.assertEqual(len(results), 1)
        self.assertNotEqual(results[0]["exit_code"], 0)
        with zipfile.ZipFile(evidence / "failure.zip") as archive:
            self.assertEqual(archive.read("work/fixture/config/settings.ini"), b"preserve failure configuration")

    def test_powershell_contracts(self):
        shells = [p for name in ["powershell", "pwsh"] if (p := shutil.which(name))]
        if not shells:
            self.skipTest("Windows PowerShell unavailable")
        for number, shell in enumerate(shells):
            with self.subTest(shell=shell):
                result = subprocess.run(
                    [shell, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File",
                     str(ROOT / "tests/powershell/WorkspaceTests.ps1"),
                     "-FixtureRoot", str(self.root / str(number))] +
                    (["-RequireJunction"] if os.environ.get("KEYJAYA_REQUIRE_JUNCTION_TEST") == "1" else []),
                    capture_output=True, text=True, timeout=120,
                    env={k: v for k, v in os.environ.items() if k.lower() != "psmodulepath"},
                )
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn("PASS", result.stdout)


if __name__ == "__main__":
    unittest.main()
