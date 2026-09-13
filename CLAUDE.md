# EliteFIPServer Guidelines

## Architecture

- `EliteFIPServer.UI` is the supported WinUI 3 application. Its output assembly is `EliteFIPServer.exe`; build and run that only.
- The legacy `EliteFIPServer` WPF app is retired and must not be built or launched unless explicitly requested for archival work.
- `EliteFIPServer.Core` owns EliteAPI ingestion, MATRIC integration, the ASP.NET Core panel server, and the served browser assets under `EliteFIPServer.Core/wwwroot/**`. See [EliteFIPServer.Core/CLAUDE.md](EliteFIPServer.Core/CLAUDE.md) and [EliteFIPServer.Core/wwwroot/CLAUDE.md](EliteFIPServer.Core/wwwroot/CLAUDE.md) for scoped guidance.
- The WinUI project hosts Core directly and copies the Core-owned web assets into its output.
- Browser contracts are normalized DTOs owned by the server. Do not expose EliteAPI event objects directly.
- `EliteFIPProtocol` is the sibling repository at `..\EliteFIPProtocol`; it may be changed when a shared contract belongs there.

## Working Practice

- Before editing, stop every running `EliteFIPServer.UI` process. Never stop unrelated processes.
- Preserve user changes in a dirty worktree. Do not revert or stage unrelated files.
- Start from the named file, failing behavior, test, or owning abstraction. Avoid broad repository mapping.
- Delegate cross-project ownership and EliteAPI inventory research to the read-only `elite-architecture-reviewer` subagent (Agent tool); keep only its concise result in the main context.
- Delegate multi-viewport dashboard checks to the `elite-dashboard-verifier` subagent after implementation. Reuse one browser page and avoid opening duplicate validation tabs.
- Delegate the stop/test/build/restart lifecycle to the `elite-build-runner` subagent (Agent tool); retain only command, totals, failures, PID, and URL in the main conversation.
- Consult [elite-api-data-inventory.md](docs/elite-api-data-inventory.md) only when EliteAPI inventory is relevant; current dashboard implementation is in `EliteFIPServer.Core/wwwroot/Dashboard.html`, `CockpitDashboard.js`, and `Dashboard.css`.

## Validation

- After the first edit, run the narrowest relevant test or diagnostic before widening scope.
- Run all tests with `dotnet test .\EliteFIPServer.Tests\EliteFIPServer.Tests.csproj --no-restore --nologo` when shared Core behavior changes.
- Build the current app with `dotnet build .\EliteFIPServer.UI\EliteFIPServer.UI.csproj --no-restore --nologo -v:minimal`.
- If `EliteFIPProtocol` changed, build it in Release before testing the server so the HintPath DLL is refreshed.
- A Debug UI build runs the repository signing target; do not add a separate signing step unless the build reports a signing failure.
- Every build also runs a pre-build version bump that rewrites `EliteFIPServer.UI/BuildInfo.cs`, `EliteFIPServer.UI/Package.appxmanifest`, and `EliteFIPServer.Version.props`. After a successful validation build, discard that churn (`git checkout -- <file>`) for whichever of those three files had no uncommitted changes before the build started, so validation never leaves the tree dirty. Never discard one that already had pre-existing uncommitted changes — that's real work, not version-bump noise.
- After successful validation, restart `EliteFIPServer.UI` and report its PID. The panel dashboard normally listens at `http://127.0.0.1:4545/`.
- Use the `/validate-elite-app` slash command for the complete stop, validate, build, restart loop.

## Git

- Do not commit, merge, create/delete branches, or push unless explicitly requested.
- Before Git state changes, inspect the current branch and exact porcelain status.
- Use the `/elite-git-workflow` slash command for branch, commit, and merge requests.

## Continuity

- Keep durable repository facts in repository memory or an existing handoff document rather than rediscovering them in later sessions.
- At each completed milestone, record only durable facts, summarize the result, and recommend `/compact` before the next phase. Do not carry completed browser/build logs forward.
