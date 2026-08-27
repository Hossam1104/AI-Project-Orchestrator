# APO-46 - Isolated Worktrees and Safe Repository Workspace Evidence

## Active execution contract

- Prompt: `5/5` - GPT-5.6 Sol planner/acceptance authority; GPT-5.6 Luna xHigh executor
- Jira: `APO-46` (In Progress); parent `APO-6`
- Authorized starting main SHA: `a37af4d7fd77eaacdfc3a716cf7c73987483f63d`
- Authorized starting main tree: `aa5dd49d566ca81745cbf0a191b8dc2e7b37c578`
- Feature branch: `feat/APO-46-isolated-worktrees-safe-evidence`
- Functional commit: `5aada3813741a22406b0ec4eecdd5b3fa7a37601`
- Functional tree: `6e2bf39b591cdb6f8081584d529eaf1737593a2c`
- Draft PR #17: `OPEN / DRAFT / UNMERGED`; base `main`; head is this feature branch
  ([GitHub PR #17](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/17))
- APO-44: `COMPLETE / MERGED / DONE` at the authorized baseline; merge SHA and tree are the starting main SHA/tree
- APO-45: `TO DO / NOT AUTHORIZED`

## Delivered scope

APO-46 delivers bounded, project-isolated workspace preparation authority:

1. Read-only Git discovery captures exact repository root/common directory, source HEAD and
   branch, cleanliness, bounded local branches, and porcelain worktree evidence.
2. `WorkspacePreparationPlan` is immutable, create-once, project-scoped, context/contract-bound,
   includes the requested branch and exact base SHA, and carries lower-case SHA-256
   content-integrity evidence.
3. Plans derive destinations only from GUIDs beneath the managed application-data workspace root;
   existing path components are checked for reparse points before mutation.
4. Preparation requires an explicit `WorkspacePreparationApproval` whose exact project-bound plan
   reference and hash match the immutable plan. Critical repository facts are revalidated after a
   repository-identity lock and immediately before the sole permitted Git mutation.
5. The Git adapter uses `UseShellExecute=false`, `ArgumentList`, bounded output capture, bounded
   process waits, no remote discovery, and only `git worktree add -b <branch> <managedPath>
   <exactBaseSha>` as a mutating command.
6. `WorkspacePreparationReceipt` is immutable and create-once. Receipt failure never removes or
   rewinds the Git worktree; recovery reports `PreparedWithoutReceipt` and supports receipt-only
   finalization. Successful receipts can become `RecoveryEvidenceKind.Repository` references; no
   checkpoint or execution runtime is created.
7. Cross-process repository serialization uses an application-data file lock keyed by normalized
   repository identity. No cleanup, fetch, push, reset, stash, merge, rebase, branch deletion,
   or worktree removal is implemented.

## Validation evidence

- `dotnet restore AIUsageMonitor.sln`: SUCCESS.
- `dotnet build AIUsageMonitor.sln --no-restore`: SUCCESS; 0 warnings, 0 errors.
- `dotnet test AIUsageMonitor.sln --no-restore`: 668/668 passed; 0 failed; 0 skipped.
- Suite totals: Domain 28; Connection 193; Provider 46; Desktop 82; Infrastructure 319.
- Focused APO-46 tests: 9/9 passed in `WorkspacePreparationTests`.
- `git diff --check`: clean before metadata-only changes.
- Changed-line credential-shaped review: no real credentials; only bounded sanitized fixture values.
- GitHub CI: `NONE / NOT CLAIMED`; no checks are reported on Draft PR #17.

## Governance and handoff boundary

This is the APO-46 implementation handoff, not acceptance. APO-46 remains `In Progress`; the PR
must remain `OPEN / DRAFT / UNMERGED` until GPT-5.6 Sol exact-head acceptance. Do not merge, mark
the PR Ready, transition APO-46 to Done, begin APO-45, invoke any model, add execution lifecycle,
synchronize trackers, implement validation/approval engines or Mission Control, or add automatic
worktree cleanup.

`OPUS-05-03..11 = DEFERRED / NON-BLOCKING`

`JsonFileStore.CurrentSchemaVersion = UNCHANGED` (remains `1`).

`OPUS-05-05 MUST BE CLOSED BEFORE JsonFileStore.CurrentSchemaVersion IS INCREMENTED`.

Runtime was explicitly not launched per APO-46 Prompt 5/5:

`APO PROCESS COUNT = 0`

`APPLICATION LEFT RUNNING = NO`

## Next planner boundary

> APO-46 Prompt 5/5 implementation complete and awaiting GPT-5.6 Sol exact-head acceptance. PR remains OPEN / DRAFT / UNMERGED. This completes the five-prompt implementation cycle; if Sol acceptance passes, the next gate is Claude Opus 5 independent critical-checkpoint review before merge. Do not begin APO-45, model invocation, execution runtime, tracker synchronization, validation/approval engines, Mission Control UI, or autonomous cleanup.
