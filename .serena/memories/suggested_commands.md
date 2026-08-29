# Windows commands

- Restore: `dotnet restore AIUsageMonitor.sln`
- Build without re-restore: `dotnet build AIUsageMonitor.sln --no-restore`
- Full tests without re-restore: `dotnet test AIUsageMonitor.sln --no-restore`
- Git tree expression must be quoted in PowerShell: `git rev-parse HEAD^{tree}`.
- Branch/worktree baseline: `git status --short --branch`, `git branch -a -vv`, `git worktree list --porcelain`, `git ls-remote --heads origin`.
- Search tracked files with `rg --files`; search text with `rg -n`.