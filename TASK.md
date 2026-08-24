# APO-37 SOL REMEDIATION DELTA ACCEPTANCE HANDOFF

## Story / scope

- Story: APO-37 — Implement Read-Only Local Git Repository Verification in Projects
- Epic: APO-6 — Git & GitHub Integration
- Exact main base (unchanged throughout this remediation): `8a81017b25fe0cfd8efcd4febafd66a1bee6c41e`
- Branch: `feat/APO-37-local-git-verification`
- Draft PR: [#7](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/7), OPEN / DRAFT / UNMERGED (reused, not replaced)
- Sol review comment: `11851`
- Pre-remediation feature HEAD: `7119145b633e9486b255786e66d78fde10ae47c0`
- Functional remediation SHA: `1a659bb7003d89af2f517dc44e477d08c126bbd6`
- Final branch SHA: recorded in the executor completion report after documentation synchronization
- Opus cadence: Prompt 4/5
- Opus review: NO OPUS REVIEW PERFORMED; Claude Opus was not invoked this prompt
- Next gate: GPT-5.6 Sol delta acceptance; if accepted, the following checkpoint is Prompt 5/5 Claude Opus independent review

## What this remediation fixes (SOL-37-01..05)

1. **SOL-37-01 — rename token order + status flags.** `GitLocalRepositoryInspector`'s porcelain
   `-z` parser previously read the rename record backwards. Git's real `-z` order is
   `new\0old\0`; the parser now publishes the new path as `RelativePath` and the old path as
   `OriginalRelativePath`, verified against a real `git mv` in a disposable temp repository.
   Conflicted/unmerged status codes (`UU`, `AA`, `DD`, `AU`, `UA`, `DU`, `UD`) are now classified
   as `Conflicted` only, never also as an ordinary staged/modified/deleted change.
2. **SOL-37-02 — remote URL sanitizer hardening.** `RepositoryUrlSanitizer` now strips
   query-string/fragment-like suffixes from SCP-style remotes as well as absolute-URI
   userinfo/query/fragment, and strips CR/LF/control-character injection from any untrusted
   remote string before display. The 512-character bound is preserved.
3. **SOL-37-03 — async bounded path probe.** The synchronous pre-Git `Directory.Exists`/
   `File.Exists` check is replaced by the new `ILocalPathProbe`/`SystemLocalPathProbe`
   Infrastructure seam: a background-thread, 4-second-bounded probe that distinguishes
   `AvailableDirectory`/`Missing`/`NotADirectory`/`Unavailable` and never blocks the WPF thread on
   a UNC/offline path.
4. **SOL-37-04 — strictly bounded process timeout.** `SystemGitCommandRunner` now uses the new
   `BoundedProcessWait` helper so `RunAsync` always returns within a short post-kill drain bound
   (default 2s) even if `Kill` fails, the process ignores termination, or stdout/stderr never
   reach EOF. `UseShellExecute=false`, `ArgumentList`, `GIT_TERMINAL_PROMPT=0`,
   `GIT_OPTIONAL_LOCKS=0` are preserved; `LC_ALL=C`/`LANG=C` were added for deterministic Git
   error-text classification. Production default remains a 10-second per-command timeout.
5. **SOL-37-05 — truthful Git exit-code classification.** A nonzero Git exit is no longer
   classified identically regardless of cause. `NotGitRepository`, detached HEAD, and unborn HEAD
   each require their documented Git outcome; every other nonzero exit (permission failure,
   process disappearance mid-command, unexpected fatal error) becomes a bounded `Failed`/
   `GitUnavailable` result through a shared timeout/cancelled/could-not-start check applied before
   command-specific interpretation. Raw stderr never reaches Application/Desktop state.

All previously accepted APO-37 behavior is preserved unchanged: manual Verify-repository flow,
project selection generation/cancellation/late-result protection, duplicate-verification
suppression, successful-edit reset, the 100-entry changed-file cap, project-relative paths, no
file-content reads or diff/patch rendering, remote comparison semantics, and normal (non-degraded)
composition.

## Application contract / model (unchanged by this remediation)

- `RepositoryVerificationStatus`: NotInspected, PathMissing, PathUnavailable, GitUnavailable,
  NotGitRepository, AvailableClean, AvailableDirty, Failed.
- `RepositoryChangedFile` with repository-relative path, old path when a rename is known, and
  staged/modified/deleted/renamed/untracked/conflicted flags.
- `RepositoryRemote` with name and sanitized URL only.
- `RepositoryRemoteComparison`: NotConfigured, NoLocalRemote, Match, Different,
  ComparisonUnavailable.
- `LocalRepositoryInspection` for Infrastructure output and `RepositoryStateSnapshot` carrying
  the selected ProjectId through publication.
- `ILocalRepositoryInspector` as the Infrastructure boundary.
- `IProjectRepositoryStateService` and `ProjectRepositoryStateService` for project-aware use-case
  semantics and registered URL comparison.

## Infrastructure Git process architecture

`GitLocalRepositoryInspector`, `SystemGitCommandRunner`, and the new `SystemLocalPathProbe` are
Infrastructure-only. Desktop/WPF does not call `Process.Start`, inspect `.git`, know executable
paths, parse porcelain, read file contents, or depend on Provider `IExecutableLocator`.

## Exact production read-only Git commands (unchanged)

1. `git --version`
2. `git -C <path> rev-parse --show-toplevel`
3. `git -C <path> symbolic-ref --quiet --short HEAD`
4. `git -C <path> rev-parse --verify HEAD`
5. `git -C <path> rev-parse --abbrev-ref --symbolic-full-name @{upstream}`
6. `git -C <path> status --porcelain=v1 -z --untracked-files=all`
7. `git -C <path> remote -v`

No fetch, pull, push, checkout, switch, reset, clean, add, commit, merge, rebase, stash, branch
mutation, tag mutation, config write, remote mutation, worktree mutation, submodule mutation,
`git ls-remote`, GitHub API, or other network operation is executed by production code.

## Tests

New regression coverage added by this remediation (Infrastructure.Tests):

- `GitLocalRepositoryInspectorTests`: rename token order, ordinary-vs-conflict status-flag
  truthfulness, detached-HEAD/unborn-HEAD exit-code truthfulness, `Root_NotRepository_*`,
  `Root_PermissionOrUnexpectedFailure_*`, `GitDisappearsAfterVersion_*`,
  `UnexpectedUpstreamFailure_*`, and path-probe-integration cases (missing/unavailable/timeout/
  cancellation skip Git entirely).
- `BoundedProcessWaitTests`: kill-throws, hung-streams, cancellation — proven deterministically
  without a real process.
- `LocalPathProbeTests`: real-filesystem available/missing/not-a-directory cases, plus injectable
  timeout/cancellation cases.
- `RepositoryUrlSanitizerTests`: HTTPS and SCP-style userinfo/query/fragment/combined cases,
  CR/LF/control-character injection, unparseable input, max-length bounding.
- `SystemGitCommandRunnerTests`: the real runner's timeout/cancellation/normal-completion paths
  against a real OS process (substituting `ping` for `git` so no Git installation or real 10-second
  wait is required).
- `RealGitRepositoryIntegrationTests`: a disposable temp repository driven by real `git init`/
  `add`/`commit`/`mv`, proving clean/dirty/staged/untracked/renamed/unborn states end-to-end
  through the real, unmodified production inspector.

Current executed totals:

- Domain: 28 passed
- Provider: 46 passed
- Infrastructure: 166 passed (was 101; +65 new tests)
- Connection: 10 passed
- Desktop: 70 passed
- Full solution: 320 passed, 0 failed, 0 skipped
- Pre-remediation baseline: 255 / 255

## Build / publish / runtime evidence

- `dotnet restore AIUsageMonitor.sln`: succeeded
- `dotnet build AIUsageMonitor.sln --no-restore`: 0 warnings, 0 errors
- `dotnet test AIUsageMonitor.sln --no-restore`: 320 passed, 0 failed, 0 skipped
- `git diff --check`: clean
- Targeted added-line secret scan: no real credentials found; all `token`/`secret` matches are
  deliberate sanitizer test fixtures or `CancellationToken` identifiers
- `win-x64` self-contained single-file publish: succeeded (`publish/win-x64/AIUsageMonitor.Desktop.exe`)
- Runtime evidence: published executable launched, verified alive with a normal (non-degraded)
  main window titled "AI Project Orchestrator", and left running per the permanent AGENTS.md
  section 16 runtime rule established by this remediation — exact PID/path recorded in the
  executor completion report; `LEFT RUNNING = YES`
- Real temp-repository validation: a disposable Git repository (real `init`/`add`/`commit`/`mv`)
  proved clean/dirty/staged/untracked/renamed/unborn states end-to-end and was removed afterward;
  the owner's registered project repository was never touched
- Visual evidence: not recaptured — this remediation changed no UI-visible XAML/WPF semantics
  (Application/Infrastructure-only correctness fixes)

## Documentation / governance / Jira synchronization

`AGENTS.md` gained section 16, "Permanent Runtime-Left-Running Contract": every future local
prompt with repository access must publish/run the current build, verify it, and leave it running
(not stop after a short smoke check) unless the environment explicitly requires otherwise, and
must report `LEFT RUNNING = YES` with executable path/PID/window title/state.

`.ai/CURRENT_STATE.md` gained a new `-5` section recording this Prompt 4/5 remediation. No BRD
change was required.

Jira status must remain:

- APO-37: In Progress
- APO-6: In Progress

One concise APO-37 completion comment records the functional remediation SHA, final branch SHA,
PR #7, all five SOL-37-01..05 resolutions, new test totals, build/publish, application PID,
`LEFT RUNNING = YES`, Prompt 4/5, and the next GPT-5.6 Sol delta acceptance gate. No duplicate
Story or status transition was made.

## Out-of-scope confirmation

This remediation added no GitHub API/Git-write capability, no new Git commands, no scope beyond
SOL-37-01..05, and touched no accepted APO-37 UI/workflow behavior. Git writes, branch creation,
checkout/switch, stage/add, commit, reset, clean, stash, merge/rebase, fetch/pull/push, GitHub
authentication/API/PR operations beyond the reused Draft PR, CI/status checks, diff/patch
rendering, Jira/Azure integration, routing, execution, agent UI, activity, validation/review/
acceptance runtime, orchestration, database/cloud backend, and provider coupling remain out of
scope.

## Delivery boundary

Work item: APO-37 (SOL-37-01..05 remediation)
Status: COMPLETE at the executor boundary / awaiting GPT-5.6 Sol delta acceptance

Implemented:
- All five SOL-37-01..05 fixes at the root cause, with regression coverage for each.
- Preservation of every previously accepted APO-37 behavior.

Validated:
- 320 / 320 full-suite tests passing (0 failed, 0 skipped).
- 0 build warnings and 0 build errors.
- `git diff --check` clean; targeted secret scan clean.
- `win-x64` self-contained single-file publish succeeded; executable verified alive and left running.
- Real disposable-temp-repository validation completed and removed.

Not validated:
- Claude Opus independent review (explicitly out of scope for this prompt; Prompt 5/5 if Sol accepts).

Blockers / limitations:
- None identified. Remote reachability/synchronization claims remain out of scope (APO-37 is local-only).

Files/areas changed:
- `src/AIUsageMonitor.Application/Projects/RepositoryUrlSanitizer.cs`
- `src/AIUsageMonitor.Infrastructure/Git/GitCommandRunner.cs`
- `src/AIUsageMonitor.Infrastructure/Git/GitLocalRepositoryInspector.cs`
- `src/AIUsageMonitor.Infrastructure/Git/LocalPathProbe.cs` (new)
- `src/AIUsageMonitor.Infrastructure/InfrastructureServiceCollectionExtensions.cs`
- `tests/AIUsageMonitor.Infrastructure.Tests/*` (five new files, one extensively extended)
- `AGENTS.md`, `.ai/CURRENT_STATE.md`, `TASK.md`, `.gitignore`

CURRENT_STATE updated: Yes

Next planner boundary:
- GPT-5.6 Sol delta acceptance of the exact final pushed remediation SHA.
- If accepted: Prompt 5/5 Claude Opus independent review.

NO Opus review performed.
