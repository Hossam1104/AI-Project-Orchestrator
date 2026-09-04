# AI_Orchestrator completion checks

- For source changes: `dotnet restore AIUsageMonitor.sln`; `dotnet build AIUsageMonitor.sln --no-restore`; `dotnet test AIUsageMonitor.sln --no-restore`; inspect warnings and test skips.
- Always run `git diff --check`, review changed-line secrets/generated artifacts, search removed symbols/references, and confirm `git status` is clean at handoff.
- For persistence/runtime changes, add or preserve focused tests for schema compatibility, atomic writes, recovery, cancellation, replay/authority, and project isolation.
- Delivery requires a named branch, committed/pushed branch, one Draft PR against main, truthful CI status, updated .ai/CURRENT_STATE.md, and no automatic merge. Use `mem:conventions` for scope and acceptance boundaries.