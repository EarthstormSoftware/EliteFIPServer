---
description: Record a Store build as released — adopt the vX.Y.Z tag from the GitHub release (or create it), move the Release tag, record the version in the docs, draft Store "What's new" notes, and optionally push.
argument-hint: "[released version, e.g. 4.0.85.0; optional: push]"
---

Record the Microsoft Store release described here: $ARGUMENTS

Only run this after the user confirms the package is live (or submitted) in Partner Center. Confirm with the user before every Git state change in this list, unless they have already approved the plan.

## How releases are made now

`Build-Release.ps1` builds once and packages that build as both the Store `.msix` and the GitHub Releases installer, so both carry the same version. The version bump is committed and pushed *before* the installer is uploaded (`Build-Installer.ps1 -SkipBuild -Publish`), and publishing the GitHub release creates the `vX.Y.Z` tag on that commit. So by the time this runs, the bump is normally committed and `vX.Y.Z` already exists on GitHub. This command records what is in the Store, which can lag the GitHub release (certification, or a later go-live).

## Conventions

- Tags are lightweight (no `-a`). The version tag is `v` plus the first three parts: `4.0.85.0` → `v4.0.85`.
- `Release` is a lightweight tag that moves to the newest Store build. It is the source of truth for the last uploaded version: `git show Release:EliteFIPServer.Version.props`. It always points at the same commit as that build's `vX.Y.Z` tag.
- List earlier releases with `git tag -l "v*"`.

## Steps

1. Inspect `git branch --show-current` and `git status --porcelain`. Expect `main`. Stop and report if unrelated files are modified — do not stage them.
2. Read `<Version>` from `EliteFIPServer.Version.props`, both in the working tree and at `HEAD`. If the user named a version, it must match one of them; otherwise stop and report the mismatch.
3. Read the previous release version from `git show Release:EliteFIPServer.Version.props`. The new version must be strictly higher. Stop if not.
4. Find the release commit:
   - Run `git fetch origin tag vX.Y.Z --no-tags`. If the tag exists on GitHub (normal case), the release commit is the one it points at. Check with `gh release view vX.Y.Z --json isDraft,tagName` that the GitHub release is published, and that `git show vX.Y.Z:EliteFIPServer.Version.props` carries the released version; stop and report if either is not so. Never create or move `vX.Y.Z` in this case.
   - If there is no such tag (a Store-only release): if the released version exists only in the working tree, commit just the three version files (`EliteFIPServer.Version.props`, `EliteFIPServer.UI/BuildInfo.cs`, `EliteFIPServer.UI/Package.appxmanifest`) with the message `Bump version to X.Y.Z.W for Store release`. The release commit is that commit (or `HEAD` if it already carries the version). Stop and report if `vX.Y.Z` exists locally; otherwise create it on the release commit.
5. Run `git tag -f Release <release commit>`.
6. Update the recorded last Store version to the new one: the `(X.Y.Z.W as of YYYY-MM-DD)` note in the Validation section of `CLAUDE.md`, and the `Release tags` bullet in `docs/HANDOFF.md` (`X.Y.Z.W at this point`). Commit only those two files, with the message `Record X.Y.Z.W as the last Store release`. This commit comes after the release commit and carries no tag.
7. Draft Store "What's new" notes. Start from the GitHub release notes (`gh release view vX.Y.Z --json body`) when there is a release, keeping only what applies to the Store version: leave out installing, SmartScreen, and the updater files. Add anything else user-facing from `git log --no-merges --pretty=%s <previous Release commit>..<release commit>`. Leave out version bumps, Claude/agent configuration, and internal refactors. Write user-facing bullets in plain English and call the product "Matric", not MATRIC. Show the draft; do not write it to a file unless asked.
8. Push only if the user asked. Before pushing, run the `elite-push-reviewer` subagent on the outgoing commits and tags. If it returns `FAIL`, report its findings and stop. On `PASS`, run `git push origin main`, plus `git push origin vX.Y.Z` only if this command created the tag, then `git push -f origin Release` (Release always needs a force push). Mention any older release tags that are missing from `git ls-remote --tags origin`.
9. Update the "last Store upload" fact in repository memory if it records a version number.

## Report

The commit hashes (or "no commit needed"), the tags created, adopted or moved with their commit, the previous → new version, whether anything was pushed, and the draft release notes.
