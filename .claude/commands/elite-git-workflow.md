---
description: Safely create or switch branches, commit selected EliteFIPServer changes, or merge a feature branch into main without pushing.
argument-hint: "[action, branch, commit message, and whether to push]"
---

Carry out the following request conservatively: $ARGUMENTS

1. Stop the running `EliteFIPServer.UI` app (process `EliteFIPServer`). Never stop unrelated processes.
2. Inspect the exact current branch, `git status --short`, and relevant diff before changing Git state.
3. Do not stage unrelated, generated, ignored, user-specific, certificate, log, `bin`, or `obj` files.
4. If the request affects a sibling repository (such as `EliteAPI`), inspect and commit that repository separately. Never combine repositories conceptually into one commit.
5. Use non-interactive Git commands. Preserve the user's requested capitalization and exact commit message.
6. Before merging, require a clean source branch, switch to lowercase `main`, and use an explicit merge commit unless the user requests another strategy.
7. Stop on conflicts; report paths and do not invent a resolution.
8. Never push, delete a branch, fetch, pull, or rebase unless explicitly requested.
9. After commit/merge, report hashes, parents for merge commits, current branches, and status of both repositories.
10. Run the relevant tests/build after a merge, then restart `EliteFIPServer.UI` on success.
