---
name: "Validate Elite App"
description: "Stop EliteFIPServer.UI, run focused or full tests, build the current WinUI app, restart it, and verify the panel server."
argument-hint: "Optional: affected area or focused test names"
agent: "agent"
tools: [read, search, execute]
---
# Validate EliteFIPServer

Perform the repository validation lifecycle for the affected changes.

1. Inspect the current branch and working-tree status. Do not alter Git state.
2. Stop all `EliteFIPServer.UI` processes.
3. If the sibling `EliteFIPProtocol` has relevant changes, build `..\EliteFIPProtocol\EliteFIPProtocol\EliteFIPProtocol.csproj -c Release --nologo -v:minimal` first.
4. Run the narrowest relevant tests requested by `${input:affectedArea}`. If no scope is supplied or shared Core behavior changed, run:
   `dotnet test .\EliteFIPServer.Tests\EliteFIPServer.Tests.csproj --no-restore --nologo`
5. Build:
   `dotnet build .\EliteFIPServer.UI\EliteFIPServer.UI.csproj --no-restore --nologo -v:minimal`
6. On success, start the newest Debug `win-x64` `EliteFIPServer.exe` under `EliteFIPServer.UI\bin` with the `--start-panel-server` argument.
7. Verify the process remains running, port 4545 is listening, and the relevant dashboard URL responds.
8. Report test totals, build warning/error counts, PID, URL, and any validation not performed.

Do not edit source files, commit, merge, push, or poll with sleeps.