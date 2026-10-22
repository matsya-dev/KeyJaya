# Preparing the GitHub repository and releases

The project folder is already a local Git repository on `main`. Do not create a
second repository inside it. A **commit** records a local snapshot, **publishing**
puts the repository on GitHub, and a **release** offers a downloadable app version.

## Before the first public commit

Original code uses MIT. The owner confirmed permission to distribute the included
book content on 22 October 2026; see [LICENSING.md](LICENSING.md). For future
content, check permissions before committing it. Removing files in a later commit
does not remove them from earlier public history.

Run `python tools/audit_project.py`, build, and run the automated tests. Review
the Changes list: generated `out/` files, personal profiles, credentials, ebook
inputs, and any data still awaiting permission must not be staged.

## Commit and publish with GitHub Desktop

1. Choose **File > Add local repository** and select this project folder.
2. Review the changed files and the Git author name/email in repository settings.
   Use your GitHub no-reply address if you want to keep your email private.
3. Enter a summary such as `Prepare KeyJaya source for publication`, then click
   **Commit to main**. This step stays on your computer.
4. Click **Publish repository**. Use the name `KeyJaya` and a short description.
   Clear **Keep this code private** to make it public, then publish.
5. Use **Repository > View on GitHub** to verify the files, README, and visibility.
   On later changes, commit locally and click **Push origin** to upload them.

If GitHub Desktop asks you to sign in, complete the normal browser sign-in flow.
Do not place access tokens or passwords in project files or commit messages.

## Publish a downloadable release

1. On the repository page, open **Releases > Draft a new release**.
2. Choose a tag such as `v1.9.4` at the exact tested source commit. Do not reuse a
   version tag for different app contents.
3. Describe the version, Windows requirements, installation, and any limitations.
4. Attach the tested installer and a SHA-256 checksum file. Keep installers out
   of ordinary Git history; the `out/` folder is ignored for that reason.
5. Publish only when both the source and bundled data are approved for sharing.
   Open the release afterward and check that the download and checksum are present.

If publishing source before data permissions are resolved, omit the installer.
State the required local data clearly in the README; a public source repository
and a public app download are separate deliverables.

Future development should take place in this source tree rather than copying old
test output folders into it. Use a short descriptive commit for each tested change.
Keep the installation on the developer's computer separate from source and test fixtures.

This clean repository has no automated publishing or signing credentials.
