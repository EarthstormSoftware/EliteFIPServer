---
description: Mark a Store build as released — commit the version bump, tag vX.Y.Z, move the Release tag, draft Store "What's new" notes, and optionally push tags.
argument-hint: "[released version, e.g. 4.0.52.0; optional: push]"
---

Record the Microsoft Store release described here: $ARGUMENTS

Only run this after the user confirms the package is live (or submitted) in Partner Center. Confirm with the user before every Git state change in this list, unless they have already approved the plan.

## Conventions

- Tags are lightweight (no `-a`). The version tag is `v` plus the first three parts: `4.0.51.0` → `v4.0.51`.
- `Release` is a lightweight tag that moves to the newest Store build. It is the source of truth for the last uploaded version: `git show Release:EliteFIPServer.Version.props`.
- List earlier releases with `git tag -l "v*"`.

## Steps

1. Inspect `git branch --show-current` and `git status --porcelain`. Expect `main`. Stop and report if unrelated files are modified — do not stage them.
2. Read `<Version>` from `EliteFIPServer.Version.props`, both in the working tree and at `HEAD`. If the user named a version, it must match one of them; otherwise stop and report the mismatch.
3. Read the previous release version from `git show Release:EliteFIPServer.Version.props`. The new version must be strictly higher. Stop if not.
4. Update the recorded last Store version to the new one: the `(X.Y.Z.W as of YYYY-MM-DD)` note in the Validation section of `CLAUDE.md`, and the `Release tags` bullet in `docs/HANDOFF.md` (`X.Y.Z.W at this point`).
5. If the released version exists only in the working tree, commit just the three version files (`EliteFIPServer.Version.props`, `EliteFIPServer.UI/BuildInfo.cs`, `EliteFIPServer.UI/Package.appxmanifest`) plus `CLAUDE.md` and `docs/HANDOFF.md` with the message `Bump version to X.Y.Z.W for Store release`. If `HEAD` already carries the version, commit only the two doc updates, with the message `Record X.Y.Z.W as the last Store release`.
6. Stop and report if the tag `vX.Y.Z` already exists. Otherwise create it on the release commit, then run `git tag -f Release` on the same commit.
7. Draft Store "What's new" notes from `git log --no-merges --pretty=%s <previous Release commit>..<new commit>`. Leave out version bumps, Claude/agent configuration, and internal refactors. Write user-facing bullets in plain English and call the product "Matric", not MATRIC. Show the draft; do not write it to a file unless asked.
8. Push only if the user asked: `git push origin main vX.Y.Z` then `git push -f origin Release` (Release always needs a force push). Mention any older release tags that are missing from `git ls-remote --tags origin`.
9. Update the "last Store upload" fact in repository memory if it records a version number.

## Report

The commit hash (or "no commit needed"), the tags created or moved with their commit, the previous → new version, whether anything was pushed, and the draft release notes.
