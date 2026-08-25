# AI Project Orchestrator (APO) - Current State

**Last Updated:** 25 August 2026
**Product:** AI Project Orchestrator (APO)
**Previous Product Identity:** AI Usage Monitor
**Repository:** `https://github.com/Hossam1104/AI-Project-Orchestrator`
**Local Root:** `D:\AI Tools\Active Projects\AI-Project-Orchestrator` (canonical active checkout; historical Hossam path absent)
**APO-18:** COMPLETE / ACCEPTED
**APO-19:** COMPLETE / SOL ACCEPTED
**APO-20:** COMPLETE — repository and physical local-root rename complete
**APO-22:** COMPLETE — Sol-accepted PR #1 merged into main
**APO-23:** COMPLETE — PR #2 merged to `main` at `40f62f9`
**APO-31:** COMPLETE / MERGED / DONE
**APO-34:** COMPLETE / MERGED / DONE
**APO-34 Merge main SHA:** `4b393b3e3cf732dd1f0e861a734e3c311327e2af`
**APO-27:** COMPLETE / SOL ACCEPTED (COMMENT 11793) / MERGED
**APO-27 Merge main SHA:** `d0efaf01b07b31effa7a432c225e7c913a86258a`
**APO-35:** COMPLETE / SOL ACCEPTED (COMMENT 11847) / MERGED / DONE
**APO-35 Merge main SHA:** `beab8072551a84aad60df7744135c74c75e51acb`
**APO-36:** COMPLETE / SOL ACCEPTED / MERGED / DONE
**APO-36 Merge main SHA:** `beab8072551a84aad60df7744135c74c75e51acb`
**APO-37:** COMPLETE / MERGED / DONE
**APO-37 Merge main SHA:** `0c76c691bd1bfb51b0d7a2799b8e5770a0c1cd9d`
**Strategic Rebaseline:** PROMPT 1/5 FINAL JIRA DAG REPAIR COMPLETE / AWAITING SOL ACCEPTANCE
**APO-5:** IN PROGRESS
**APO-4:** IN PROGRESS
**APO-6:** IN PROGRESS
**Repository/local-folder rename:** COMPLETE; repository and physical local-root rename complete
**Jira Project:** `APO`
**Default Branch:** `main`
**Current Story:** Prompt 1/5 final Jira DAG repair - Jira graph and roadmap documentation synchronization.
**Current Epic:** APO-1 governance boundary with roadmap mapping across APO-3 through APO-16.
**Status:** APO-37 finalization is complete at the accepted main baseline. Prompt 1/5 final Jira DAG repair is complete: Jira, the hard-dependency documentation, the active local-root truth, and the Sol handoff are synchronized. Draft PR #8 remains OPEN / DRAFT / UNMERGED pending Sol acceptance.
**Next implementation:** GPT-5.6 Sol must accept this final strategic rebaseline and select one bounded Story; APO-38 is the recommended first implementation boundary. No strategic runtime feature has been implemented in this continuation.
**Release state:** APO-37 provides explicit, manual, read-only local repository verification. Smart Continue, Mission Control, routing runtime, bounded execution, tracker automation, remote SCM evidence, controlled delivery, review/acceptance engines, background automation, remote approval, and full release qualification remain planned or incomplete. No GitHub CI evidence is claimed.

## Strategic Rebaseline - Prompt 1/5 Continuation (25 August 2026)

**Exact merged baseline:** `origin/main` and the strategic branch start at
`0c76c691bd1bfb51b0d7a2799b8e5770a0c1cd9d`.

**APO-37 finalization:**

- Accepted head: `e35762478ae87c406939d11662e00fef1727c04a`.
- PR #7: merged as a squash commit; accepted head tree and merge tree are identical.
- Jira APO-37: transitioned to Done; final completion comment `11862`.
- Jira APO-6: intentionally remains In Progress.
- Sol Prompt 5/5 adjudication comment `11861`: APO-37 accepted for merge finalization with
  OPUS-01, OPUS-02, OPUS-03, OPUS-04, OPUS-05, OPUS-07, and OPUS-09 retained as P3 hardening;
  OPUS-06 and OPUS-08 rejected and not opened as defects.

**Strategic branch:** `docs/apo-strategic-orchestrator-rebaseline`; continuation started from
`c392994927e21e23a612a80b29219d5d1feadef9`; draft PR #8 remains OPEN / DRAFT / UNMERGED against
`main`. The continuation documentation synchronization commit is
`e238d713c0cfb26c16c6e81b459b37c418cbe91c`; the final branch SHA after the metadata handoff is
reported at executor completion.

**Approved strategic direction incorporated:** Mission Control; Smart Continue; canonical
project context and checkpoint recovery; dependency-aware work graphs; isolated workspaces;
evidence-based QA gates; Review Inbox; composable skills/workflows; explainable project health;
quality-first quota-aware routing; bounded background automation and housekeeping; optional remote
approval design; AI Decision Ledger; context budgets; automatic role handoffs; truthful runtime
evidence; and progressive project onboarding. The active WPF/.NET/local-first architecture and
human/evidence authority are unchanged.

**Jira reconciliation:**

- Existing APO-1 through APO-17 Epics were reused; no new Epic was created.
- Existing APO-33 CI Story was reused rather than duplicated.
- New bounded roadmap Stories APO-38 through APO-63 were created under the existing Epics with
  explicit P0/P1/P2/P3 bands, scope, dependencies, acceptance evidence, and out-of-scope clauses.
- APO-43 explicitly owns Smart Continue, canonical context, and recovery checkpoints.
- APO-62 owns provider-independent read-only GitHub/Azure Repos remote SCM/CI evidence under APO-6.
- APO-63 owns controlled remote source-control delivery under APO-6 and remains behind evidence and
  owner-approval gates.
- APO-59 through APO-61 retain the accepted APO-37 P3 hardening backlog.
- APO-64 through APO-67 are Done VOID connector-correction artifacts with zero product scope and
  are excluded from roadmap totals and dependency sequencing.
- Critical predecessor links were created with Jira Blocks relationships; APO-37 to APO-59..61
  is recorded as related work.

**Required next Sol gate:** inspect BRD 1.2, Implementation Plan 1.2, this state, TASK.md, and
Jira APO-38..63; accept or revise the dependency order; then prepare one self-contained bounded
contract. Recommended first Story: APO-38, provider-independent agent/model registry and
connection truth. Do not implement APO-38 in this rebaseline session.

**Validation and runtime evidence for this rebaseline:** `dotnet restore AIUsageMonitor.sln`
succeeded; `dotnet build AIUsageMonitor.sln --no-restore` succeeded with 0 warnings and 0 errors;
the full solution test run passed 326/326 with 0 failures and 0 skips; `git diff --check` passed;
the changed-line secret scan found no credential-shaped values. The self-contained `win-x64`
publish profile completed successfully. No APO process was running during pre-publish detection;
the current published executable was started at
`D:\AI Tools\Active Projects\AI-Project-Orchestrator\src\AIUsageMonitor.Desktop\bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\AIUsageMonitor.Desktop.exe`
with PID 26240, window title `AI Project Orchestrator`, `Responding=True`, and `HasExited=False`.
UI Automation exposed `CAPACITY READY`, the Projects navigation control, the AI capacity
workspace, and the normal shell. No duplicate process was created. `LEFT RUNNING = YES`.

## Prompt 1/5 Final Jira DAG Repair (25 August 2026)

**Work item:** Final Jira dependency-graph repair and strategic documentation synchronization.

**SOL-SR-01: CLOSED** — the live Jira graph was backed up outside the repository at
`C:\Users\Win11\AppData\Local\Temp\APO-Jira-Link-Repair-20260824-234658`. The pre-delete
invariant was exactly 25 strategic `Blocks` links, IDs `10479` through `10503`, plus the three
accepted `Relates` links `10504`/`10505`/`10506`.

The old strategic `Blocks` graph was removed through official ACLI. The canonical 18-link hard DAG
was rebuilt and verified live with generated IDs `10525` through `10542`:

```text
APO-38 -> APO-39                 APO-38 -> APO-44
APO-40 -> APO-41                 APO-40 -> APO-42
APO-40 -> APO-43                 APO-40 -> APO-45
APO-39 -> APO-43
APO-41 -> APO-45                 APO-42 -> APO-45
APO-43 -> APO-45                 APO-44 -> APO-45
APO-46 -> APO-45
APO-45 -> APO-48
APO-48 -> APO-63                 APO-49 -> APO-63
APO-62 -> APO-63
APO-45 -> APO-57                 APO-49 -> APO-58
```

Live verification reported 18 Blocks, no old IDs, no duplicate pair, no reverse pair, no self-link,
and no graph cycle. `10504`/`10505`/`10506` remain `Relates` from APO-37 to APO-59/60/61. The
ACLI executable is `C:\Users\Win11\AppData\Local\AIProjectOrchestrator\tools\acli\acli.exe`,
version `1.3.29-stable`, authenticated by OAuth to `hossamsqa.atlassian.net`.

During verification, ACLI's `--out A --in B` success text was compared with Jira's live REST
`fields.issuelinks` semantics. The flags produced the inverse live direction, so the 18 initially
created links were removed and recreated with reversed flags; the final graph above is based on the
live Jira semantics, not the CLI success text alone.

**SOL-SR-02: CLOSED** — `docs/BRD.md`, `docs/IMPLEMENTATION_PLAN.md`, and
`docs/STRATEGIC_ROADMAP.md` now distinguish the canonical 18-link hard DAG from recommended
planner ordering, explicitly place APO-46 before APO-45, and preserve APO-43/62/63 ownership.

**SOL-SR-03: CLOSED** — the active canonical checkout is
`D:\AI Tools\Active Projects\AI-Project-Orchestrator`; it has the expected origin, branch, and
starting SHA `860a79e69a87c4696dc4595b70ef260d6382532f`. The historical
`D:\AI Tools\Hossam\AI-Project-Orchestrator` path does not exist.

**SOL-SR-04: CLOSED** — corrected the Section 17 compact capability map in
`docs/IMPLEMENTATION_PLAN.md` so the visual summary retains the authoritative ordering
`APO-48 → APO-49 → APO-63 → APO-50`. The BRD, implementation plan, and strategic roadmap remain
consistent; no source, test, Jira, Story scope, ownership, link, priority, or status changes were
made.

Final-session validation succeeded: restore was up to date; build completed with 0 warnings and 0
errors; the full solution test run passed 326/326 (Domain 28, Connection 10, Provider 46, Desktop
70, Infrastructure 172); `git diff --check` passed; the changed-line secret scan found no
credential-shaped matches; and product-source/test-source diffs were both zero.

The self-contained published executable remains running at
`D:\AI Tools\Active Projects\AI-Project-Orchestrator\src\AIUsageMonitor.Desktop\bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\AIUsageMonitor.Desktop.exe`
with PID `19588`, window title `AI Project Orchestrator`, `Responding=True`, and `HasExited=False`.
A fresh UI Automation state exposed `CAPACITY READY`, the normal AI Capacity workspace, provider
cards, refresh controls, and Projects/AI capacity navigation. The helper could not activate the
captured window for a navigation click, so no navigation action is claimed; the process and normal
shell state were left running. `LEFT RUNNING = YES`.

No product source or test source was changed. No APO-38 implementation, Prompt 2/5 work, CI claim,
Sonnet review, or Opus review was performed. The Jira/DAG repair documentation commit is
`b6f82854cc36950e567c3bfdf63fe4a27f8ae994`; the final metadata handoff commit and branch head are
reported at executor completion.

## -5. APO-37 SOL-37-01..05 Bounded Correctness Remediation (Prompt 4/5)

**Sol review comment:** `11851`

**Pre-remediation feature HEAD:** `7119145b633e9486b255786e66d78fde10ae47c0`

**Required main base (unchanged):** `8a81017b25fe0cfd8efcd4febafd66a1bee6c41e`

**Branch:** `feat/APO-37-local-git-verification` (Draft PR #7, reused, still OPEN/DRAFT/UNMERGED)

**Functional remediation SHA:** `1a659bb7003d89af2f517dc44e477d08c126bbd6`

This remediation fixes five bounded correctness defects identified by Sol review comment 11851
against the APO-37 read-only local Git inspector, without expanding scope, adding GitHub API/Git
write capability, or touching accepted APO-37 UI/workflow behavior:

- **SOL-37-01 (rename token order + status flags):** `GitLocalRepositoryInspector`'s porcelain `-z`
  parser previously reversed Git's real rename record order. Git emits `new\0old\0`; the parser now
  reads the token immediately following the change record as the *original* path, publishing the
  new name as `RelativePath` and the old name as `OriginalRelativePath`. Verified against a real
  `git mv` in a disposable temp repository (`RealGitRepositoryIntegrationTests`), not only against
  scripted fixtures. Status-flag interpretation was also corrected so a conflicted/unmerged entry
  (`UU`, `AA`, `DD`, `AU`, `UA`, `DU`, `UD`) is classified as `Conflicted` only and never also as an
  ordinary staged/modified/deleted change.
- **SOL-37-02 (remote URL sanitizer hardening):** `RepositoryUrlSanitizer` now strips query-string-
  and fragment-like suffixes from SCP-style remotes (`user@host:path?token=...`,
  `user@host:path#...`) in addition to the pre-existing absolute-URI userinfo/query/fragment
  stripping, and removes CR/LF/control-character injection from any untrusted remote string before
  display so a hostile remote value can never fabricate extra UI/log lines. The 512-character bound
  is preserved.
- **SOL-37-03 (async bounded path probe):** the inspector's pre-Git-invocation path check is now the
  new Infrastructure-only `ILocalPathProbe`/`SystemLocalPathProbe` seam instead of a synchronous
  `Directory.Exists`/`File.Exists` call on the calling thread. The underlying OS filesystem attribute
  lookup is not reliably cancellable (a UNC/offline path can block indefinitely), so the probe runs
  on a background thread with a 4-second deterministic bound; `Missing` and `Unavailable` are
  distinct, truthful outcomes, and Git is never invoked once the path state already rules out
  inspection.
- **SOL-37-04 (strictly bounded process timeout):** `SystemGitCommandRunner` previously fell back to
  an unbounded `AwaitOutputAsync` after a failed `KillProcessTree`. The new `BoundedProcessWait`
  helper guarantees `RunAsync` returns within a short post-kill drain bound (default 2s) even if
  `Kill` throws, the process ignores termination, or stdout/stderr never reach EOF; no raw partial
  output ever reaches the caller. The production default remains a 10-second per-command timeout;
  `UseShellExecute=false`, `ArgumentList`, `GIT_TERMINAL_PROMPT=0`, and `GIT_OPTIONAL_LOCKS=0` are
  preserved, and `LC_ALL=C`/`LANG=C` were added for deterministic Git error-text classification.
- **SOL-37-05 (truthful Git exit-code classification):** a nonzero Git exit is no longer classified
  identically regardless of cause. `NotGitRepository` requires the documented "not a git repository"
  fatal text; detached HEAD requires the documented exit status for `symbolic-ref --quiet --short
  HEAD`; unborn HEAD requires the documented "no commits yet"-family fatal text for `rev-parse
  --verify HEAD`. Any other nonzero exit — permission failure, the Git process disappearing mid-
  command, an unexpected fatal error — becomes a bounded `Failed`/`GitUnavailable` result through a
  shared `CommonFailure`/timeout/cancelled/could-not-start check applied before command-specific
  interpretation, so a mid-command process failure can never be fabricated into a semantic repository
  state (e.g. "Git disappeared halfway" no longer reads as a detached HEAD). Raw stderr never reaches
  Application/Desktop state.

**New test coverage:** full solution now passes **320/320** (was 255/255): Domain 28, Provider 46,
Connection 10, Desktop 70, Infrastructure 166 (was 101). The added 65 Infrastructure tests cover the
porcelain rename/status regression, ordinary-vs-conflict status-flag truthfulness, the path-probe
seam (available/missing/not-a-directory/unavailable/timeout/cancellation), the bounded process-wait
helper (kill-throws, hung-streams, cancellation), the real `SystemGitCommandRunner` timeout/
cancellation path against a real OS process, the `RepositoryUrlSanitizer` hardening (HTTPS and
SCP-style userinfo/query/fragment/CR-LF/control-character/max-length cases), and the Git exit-
classification truthfulness cases (`Root_NotRepository_IsNotGitRepository`,
`Root_PermissionOrUnexpectedFailure_IsNotNotGitRepository`,
`SymbolicRef_ExpectedNonSymbolicExit_IsDetached`, `SymbolicRef_UnexpectedFailure_IsFailed`,
`Head_AbsentInValidUnbornRepository_HasNoSha`, `GitDisappearsAfterVersion_DoesNotFabricateRepositoryState`,
`UnexpectedUpstreamFailure_DoesNotCreateFakeUpstream`), plus a real-Git integration suite
(`RealGitRepositoryIntegrationTests`) against a disposable temp repository proving clean/dirty/
staged/untracked/renamed/unborn states end-to-end.

**Validation at this checkpoint:** `dotnet restore` succeeded; `dotnet build --no-restore` is 0
warnings/0 errors; `dotnet test --no-restore` is 320/320 passing; `git diff --check` is clean; a
targeted secret scan of the changed lines found no leaked credentials (all `token`/`secret` matches
are intentional sanitizer test fixtures or `CancellationToken` identifiers). The `win-x64`
self-contained single-file publish succeeded and the published executable was launched and left
running (see the executor completion report for PID/path/window-title evidence). No owner project
registry or repository was touched; all Git write commands used in validation ran only against a
disposable temp repository that was removed afterward.

**Permanent governance addition:** `AGENTS.md` section 16 ("Permanent Runtime-Left-Running
Contract") now requires every future local prompt with repository access to publish/run the current
build and leave it running (not stop it after a short smoke check), reporting
`LEFT RUNNING = YES` with executable path/PID/window title/state, unless the environment explicitly
requires otherwise.

**Preflight/postflight invariants held throughout:** local feature HEAD matched origin feature HEAD
matched the required starting SHA before work began; `origin/main` remained
`8a81017b25fe0cfd8efcd4febafd66a1bee6c41e` throughout; PR #7 remained OPEN/DRAFT with
`baseRefName=main` and `mergeable=MERGEABLE`. No rebase, merge-main, force-push, or replacement PR
was performed.

## -4. APO-37 Read-Only Local Git Repository Verification (Prompt 3/5)

**Exact starting main SHA:** `8a81017b25fe0cfd8efcd4febafd66a1bee6c41e`

**Branch:** `feat/APO-37-local-git-verification`

**Functional implementation SHA:** `3fa4791641e13383cca3ac36ef4e632045bf2704`

**Documentation / handoff SHA:** `e30581d36bd9273dfc7477287c8f16ce1bd33eb7`

**Draft PR:** [#7](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/7), OPEN / DRAFT / UNMERGED.

APO-37 adds `RepositoryVerificationStatus`, bounded changed-file/remote models,
`ILocalRepositoryInspector`, `IProjectRepositoryStateService`, and the project-aware
`ProjectRepositoryStateService` in Application. Infrastructure owns `SystemGitCommandRunner` and
`GitLocalRepositoryInspector`; the runner uses `UseShellExecute=false`, `ArgumentList`,
`GIT_TERMINAL_PROMPT=0`, `GIT_OPTIONAL_LOCKS=0`, asynchronous cancellation, and a ten-second
per-command timeout. Only local read-only Git commands are allowed, and remote URL userinfo/query
material is removed before publication. Porcelain-v1 NUL status parsing preserves staged,
modified, deleted, renamed, untracked, and conflicted evidence with a deterministic 100-entry
bound and truthful total/truncation fields.

ProjectsViewModel exposes explicit Verify repository and Refresh repository state commands. It
starts at Not inspected for every selected project, resets after a successful edit or registry
refresh, cancels on selection change, and checks both request generation and selected ProjectId
before publishing a result. WPF does not execute processes or parse Git output. The detail card
shows only known local values and never labels a remote connected, synced, healthy, or reachable.

**Project lifecycle documentation correction:** the accepted values are Active, Paused, Blocked,
Archived; filter choices are All, Active, Paused, Blocked, Archived. Draft and Completed are not
ProjectStatus values.

**Validation at this checkpoint:** restore succeeded; full solution tests 255/255 passing: Domain
28, Provider 46, Infrastructure 101, Connection 10, Desktop 70; build is 0 warnings / 0 errors;
the `win-x64` self-contained single-file publish succeeded; the published executable remained alive
through a five-second startup smoke; a temporary sanitized repository produced dirty-state and
sanitized-remote evidence; and `git diff --check` is clean. No owner project registry or repository
was modified. The branch was pushed, PR #7 is OPEN / DRAFT / UNMERGED, and Jira completion comment
`11849` was posted without a status transition. The final metadata synchronization SHA is recorded
in the executor completion report.

## -3. APO-35 First Usable Projects Workspace

**Exact starting main SHA:** `34569abee50bdb708770e134e9db7db18752a80d`

**Branch:** `feat/APO-35-projects-workspace`

**Functional implementation SHA:** `6f3cd6d` (`feat(APO-35): implement first usable Projects workspace`)

**Draft PR:** [#6](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/6), OPEN / DRAFT / UNMERGED

**Story / Epic:** APO-35 / APO-5

**Delivery state:** IMPLEMENTATION COMPLETE / AWAITING GPT-5.6 SOL ACCEPTANCE; the final
documentation/handoff commit SHA is reported in `TASK.md` and the executor completion report
after the handoff commit is pushed

APO-35 adds the first usable project registry workspace over the accepted APO-27 storage
foundation. The Application layer now exposes `IProjectRegistryService` and
`ProjectRegistryService`, which generate project identity and creation timestamps, preserve
existing identity/creation time and hidden `RepositoryMetadata`/`TrackerMetadata` on edit, normalize
governance-reference lines, and reuse the existing `IProjectRepository` contract. Time behavior
uses the accepted testable `IClock` seam; Infrastructure continues to provide `SystemClock`.

The WPF shell now enables only Projects while retaining AI Capacity as the startup workspace and
keeping Agents/Activity disabled. `ProjectsViewModel` owns asynchronous loading/saving, selection,
search over name/local path, All/Active/Paused/Blocked/Archived filtering, create/edit/cancel/save
commands, archive/restore through lifecycle status, bounded validation, read/write failure states,
and degraded no-persistence behavior. The UI provides list cards, metadata-only detail, in-workspace
editor, truthful empty/no-match/loading/error states, readable local timestamps, status text, and
accessible labels for critical controls.

Repository and tracker values are registration metadata only. No local path probing, Git, tracker
calls, credentials, repository content, routing, execution, validation, review, acceptance, activity,
or orchestration runtime was added. No destructive Delete command exists.

**Focused validation:** 38 Desktop tests pass, including service create/edit semantics, hidden
metadata preservation, search/filter/selection, lifecycle transitions, validation, read/write failure
handling, degraded mode, duplicate-save prevention, and shell navigation. Full solution validation
is 208/208 passing: Domain 28, Provider 46, Infrastructure 86, Connection 10, Desktop 38.

**Build/publish:** restore and solution build pass with 0 warnings and 0 errors. The existing
`win-x64` self-contained single-file publish profile passed. x86 and ARM64 publish evidence was not
rerun because project/package/publish configuration did not change; prior accepted compile/publish
evidence remains historical and no hardware runtime claim is made.

**Runtime smoke:** the published x64 executable launched and remained alive for five seconds with
window title `AI Project Orchestrator`; it was then stopped by the smoke harness. The bundled
Computer Use native helper was unavailable in this environment, so no new visual screenshot was
captured and no visual evidence is claimed for APO-35.

**Opus cadence:** Prompt 3/5 complete. No Claude Opus review was performed, as explicitly required
by the execution contract. Next gate: GPT-5.6 Sol acceptance.

---

## -3a. APO-35 Delta Remediation: SOL-35-01 / SOL-35-02 (Prompt 4)

**Scope authority:** Sol's review of the APO-35 draft (PR #6) confirmed one defect and one missing
evidence item, and authorized Claude Sonnet 5, as bounded executor, to fix exactly those two items
with no scope expansion.

**SOL-35-01 (edit-target integrity) — FIXED.** `ProjectsViewModel` previously resolved the save
target from `SelectedProject?.Id` at save time, so if selection changed while an edit was open, a
save could silently update the wrong project. The fix adds an immutable `_editingProjectId` field
captured when `EditSelectedProject()` runs; `SaveAsync()` uses only that captured id, fails closed
with a truthful `ErrorMessage` if it is missing, and leaves it untouched on a failed save so a retry
still targets the original project. It is cleared only on cancel or a successful save. A new
`IsRegistryInteractionEnabled` property (`!IsEditing && !IsSaving && !IsLoading`) now gates the
project `ListBox`, search `TextBox`, and status filter `ComboBox` in `MainWindow.xaml`, and the
`New project`/`Refresh` command predicates were extended with the same `!IsEditing` guard, so the
registry list cannot change under an in-progress edit. The fix is confined to
`AIUsageMonitor.Desktop.ViewModels.ProjectsViewModel`; `ProjectRegistryService` (Application layer)
was not touched, since it already takes an explicit `projectId` parameter and was never the source
of the defect.

Six new regression tests were added to `ProjectsWorkspaceTests.cs`:
`EditTarget_DoesNotFollowSelectionChange`, `EditTarget_DoesNotFollowFilterSelectionLoss`,
`FailedSave_RetryUsesOriginalEditTarget`, `NewAndRefreshCommandsDisabledWhileEditing`,
`CancelClearsEditTargetAndRestoresRegistryInteraction`,
`SuccessfulSaveClearsEditTargetAndRestoresRegistryInteraction`. Full-suite validation is 214/214
passing (Domain 28, Provider 46, Infrastructure 86, Connection 10, Desktop 44, up from 208/38).
Build is 0 warnings / 0 errors; `git diff --check` reported only benign LF/CRLF notices; a
secret-pattern scan of the diff found nothing. `win-x64` self-contained single-file publish
succeeded and the published executable stayed alive through a runtime smoke launch/stop cycle. x86
and ARM64 publish were not rerun; no configuration affecting them changed.

**SOL-35-02 (sanitized visual evidence) — BLOCKED, NOT FABRICATED.** Evidence capture used
Windows-native UI Automation (`System.Windows.Automation`) to drive the real published `win-x64`
executable and GDI (`System.Drawing.Graphics.CopyFromScreen`) to capture the screen, per the
required no-new-packages/no-browser-automation constraint. During capture, the published shell was
found to start in a fully degraded, no-persistence fallback mode on **every** launch, for both the
Projects and AI Capacity workspaces, not only Projects. Root cause, confirmed via a temporary,
reverted diagnostic in `App.xaml.cs` and removed before delivery: `AiCapacityViewModel` has two
applicable public constructors, `(IProviderRegistry, IProviderConnectionService)` and
`(IExecutableLocator)`. The .NET generic host's DI activator cannot disambiguate them when resolving
`MainWindow` from the service container, throws
`InvalidOperationException: ... constructors are ambiguous` inside `App.OnStartup` → `ShowShell`,
and the existing outer `catch` silently falls back to `new MainWindow()` with null-backed,
non-persistent view models. `git log --oneline -- src/AIUsageMonitor.Desktop/ViewModels/AiCapacityViewModel.cs`
shows this file has been touched by exactly one commit, `4b393b3` (APO-34, already merged to `main`
before the APO-35 branch was created at `34569abee50bdb708770e134e9db7db18752a80d`), so this is a
pre-existing regression unrelated to `ProjectsViewModel`/SOL-35-01 and outside the authorized
SOL-35-01/SOL-35-02 scope. Because the automation could only reach a degraded shell, no
functionally real screenshot of the Projects workspace or its editor exists to sanitize and commit.
Per the explicit no-fabrication instruction, **no image was added to `docs/evidence/`** and none is
claimed. This is reported as `APO-35 VISUAL EVIDENCE COULD NOT BE CAPTURED SAFELY` for Sol
disposition; recommended follow-up is a small, separately scoped Jira item to remove the ambiguous
`AiCapacityViewModel` constructor before SOL-35-02 evidence is re-attempted.

**Out of scope, not performed (per contract):** no rebase/force-push/reset/merge; PR #6 left
OPEN/DRAFT/UNMERGED; `main` untouched; APO-35/APO-5 not marked Done; no other Story started; no
`AiCapacityViewModel` fix applied; no APO-27 P3 deferred findings addressed; no Application-layer
change.

**Jira synchronization:** remediation comment posted to APO-35, comment `11798`. No Jira status
transition was made.

---

## -3b. APO-36 Blocking Startup Regression Fix + SOL-35-02 Evidence Completion (Prompt 4 continuation)

**Scope authority:** This is an explicit continuation of the same APO-35 Prompt-4 delta (not an
advance to Prompt 5, and Claude Opus was not invoked), authorized to fix the newly discovered
`AiCapacityViewModel` DI defect reported as blocking in section -3a, on the same branch
(`feat/APO-35-projects-workspace`) and the same draft PR #6.

**Pre-APO-36 branch head:** `9c2c678`
(`docs(APO-35): record SOL-35-01 remediation and blocked SOL-35-02 evidence`)

**APO-36 root cause (confirmed, matches section -3a's diagnosis):** `AiCapacityViewModel` has two
applicable public constructors — `(IExecutableLocator)` (degraded/manual fallback) and
`(IProviderRegistry, IProviderConnectionService)` (normal provider-backed). Production registration
used an implicit `services.AddSingleton<AiCapacityViewModel>()`; Microsoft DI selects the
constructor with the most fully-resolvable parameters, then requires every other resolvable
constructor's parameter set to be a subset of that choice. `{IExecutableLocator}` is not a subset of
`{IProviderRegistry, IProviderConnectionService}`, so the container threw
`InvalidOperationException: ... constructors are ambiguous` while resolving `MainWindow` inside
`App.OnStartup`; the existing outer `catch` silently fell back to `ShowShell(persistenceAvailable:
false)`, degrading the shell on every normal launch.

**APO-36 fix (smallest composition-root change, no `AiCapacityViewModel` redesign):**
`src/AIUsageMonitor.Desktop/DesktopServiceCollectionExtensions.cs` (new) adds
`AddDesktopWorkspaceServices(this IServiceCollection)`, registering `IProjectRegistryService` →
`ProjectRegistryService` (moved from `App.xaml.cs`, unchanged registration), `AiCapacityViewModel`
via an explicit factory that resolves `IProviderRegistry` and `IProviderConnectionService` directly
(bypassing constructor selection), and `ProjectsViewModel`/`MainWindowViewModel`/`MainWindow`
(moved, unchanged registrations). `App.OnStartup`'s `ConfigureServices` now reads
`services.AddInfrastructure(_paths.RootDirectory); services.AddProviders();
services.AddDesktopWorkspaceServices();` — the exact same three calls the new composition regression
tests exercise. The degraded/manual `AiCapacityViewModel(IExecutableLocator)` constructor,
`InitializeDegradedAsync`, `ShowShell(persistenceAvailable: false)`, and `App.OnStartup`'s outer
`catch` are all unchanged.

**New composition regression tests**
(`tests/AIUsageMonitor.Desktop.Tests/ProductionCompositionTests.cs`, 5 new tests) build the real
production `ServiceCollection` (`AddInfrastructure` against a temp root + `AddProviders` +
`AddDesktopWorkspaceServices`) and resolve through the actual Microsoft DI `ServiceProvider`:
`ProductionComposition_ResolvesAiCapacityViewModel`,
`ProductionComposition_AiCapacityIsNotDegraded` (asserts `IsDegraded == false` and `Cards.Count ==
5`), `ProductionComposition_ResolvesMainWindowViewModel`,
`MainWindowViewModel_UsesResolvedNormalAiCapacity` (asserts `MainWindowViewModel.AiCapacity` is the
same non-degraded instance), `ProjectsViewModel_IsResolvedWithRegistryService` (asserts
`IsStorageAvailable == true` and shares the same `ProjectsViewModel` instance as
`MainWindowViewModel.Projects`). Existing degraded-construction tests in `CapacityViewModelTests.cs`
(direct `new AiCapacityViewModel()` / `new AiCapacityViewModel(locator)`, not routed through DI)
were not modified and remain green, confirming the fallback path is intact. SOL-35-01's six tests in
`ProjectsWorkspaceTests.cs` were not touched and remain green.

**APO-36 functional commit:** `4525b31` (`fix: resolve APO-36 AI capacity DI startup ambiguity`).

**SOL-35-02 (sanitized visual evidence) — now COMPLETE.** After the APO-36 fix, the same
Windows-native UI Automation approach used in section -3a's blocked attempt confirmed a normal,
non-degraded launch: the Overview workspace reports persistence text `"Ready"` / `"LocalAppData is
available for the foundation."` (not degraded), the AI Capacity workspace shows all five real
provider-backed cards with `Connect / edit` actions (which only render when the connection service
is present, i.e. non-degraded), and Projects shows a real, empty registry (`"0 total"`, `"No
projects registered yet."` — not `"Project storage unavailable."`). `New project` was invoked and
the editor rendered with blank fields and `Cancel`/`Save changes` visible. Screen capture used the
Win32 `PrintWindow` API with `PW_RENDERFULLCONTENT` (`user32.dll`, no new package) instead of GDI
`CopyFromScreen`, because this machine's console session had auto-locked from idle and
`CopyFromScreen` was found to capture the OS lock screen rather than the target window;
`PrintWindow` renders the window's own content via DWM regardless of physical display state. The
project registry was genuinely empty at capture time, so the screenshot contains no real project
names, paths, repository URLs, tracker ids, or credentials; it was inspected before commit and
required no cropping or modification. Evidence path:
`docs/evidence/APO-35-projects-workspace.png`.

**Full validation after the APO-36 fix:**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused Desktop composition tests | SUCCESS; 5/5 new `ProductionCompositionTests` passing |
| `dotnet test AIUsageMonitor.sln --no-restore` | SUCCESS; 219/219 passing (Domain 28, Provider 46, Infrastructure 86, Connection 10, Desktop 49; up from 214/44) |
| `win-x64` self-contained single-file publish | SUCCESS |
| `win-x86` / `win-arm64` publish | Not rerun; no project/package/publish configuration changed |
| Published `win-x64` runtime verification | SUCCESS; real UI Automation drive-through proved the normal, non-degraded shell (see above), not just process-alive |
| `git diff --check` | Clean |
| Secret-pattern scan of the diff | Clean |

**Out of scope, not performed (per contract):** no rebase/force-push/reset/merge-main; PR #6 left
OPEN/DRAFT/UNMERGED; `main` untouched; APO-35/APO-36/APO-5 not marked Done; no other Story started;
no new provider behavior, quota changes, provider refactoring, Git/GitHub product operations,
Jira/Azure integrations, agent UI, routing, execution, orchestration, validation/review/acceptance
engines, activity UI, database, cloud backend, delete/purge, or namespace migration; no Claude Opus
review performed (Prompt 4/5 continuation, not Prompt 5).

**Jira synchronization:** one comment posted to APO-36 (root cause, fix, functional SHA, final
branch SHA, PR #6, composition tests, normal runtime proof, screenshot evidence, full tests, next
gate) and one dependency-resolution comment posted to APO-35 (APO-36 fix complete, SOL-35-02
evidence completed, screenshot path, next gate Sol delta acceptance). No Jira status transition was
made.

**Next planner boundary:** GPT-5.6 Sol final Prompt-4 delta acceptance against the exact final
pushed branch SHA. If accepted, the next checkpoint is Prompt 5/5 Claude Opus independent review.

---

## -3c. APO-35 Opus UI Findings Remediation (New Opus Cycle, Prompt 1/5)

**Lifecycle at this checkpoint:** SOL-35-01 remediated -> SOL-35-02 evidence complete (APO-36 fix
applied) -> real Claude Opus 5 independent review of the Projects UI -> Opus verdict `CHANGES
REQUIRED` against four findings (`OPUS-01`..`OPUS-04`, P2, and `OPUS-05`..`OPUS-08`, P3, deferred)
-> GPT-5.6 Sol adjudicated the review in Jira comment `11838`, accepted `OPUS-01`..`OPUS-04` as
blocking and authorized this bounded Claude Sonnet 5 remediation as Prompt 1/5 of a new Opus cadence
-> THIS remediation -> next gate is Sol delta acceptance of this section.

**Scope authority:** Sol's adjudication authorized exactly `OPUS-01`..`OPUS-04`. `OPUS-05`..`OPUS-08`
(P3) are explicitly deferred and were not implemented. Claude Opus was not invoked in this Prompt.
The reused finding codes `OPUS-01`..`OPUS-04` here are a **new, distinct cycle** scoped to the
Projects UI; they are unrelated to the earlier APO-27 storage-durability `OPUS-01`..`OPUS-08` codes
recorded in section 13.3/13.4 below, which are historical and already resolved/deferred in that
separate context.

**Pre-remediation branch head:** `bad6380` (`docs/evidence: complete APO-35 Prompt 4 validation`).
Base `main`: `34569abee50bdb708770e134e9db7db18752a80d` (unchanged). Draft PR
[#6](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/6) remains OPEN / DRAFT / UNMERGED.

**OPUS-01 (P2, BLOCKING) — FIXED.** The registry `ListBox` lived inside a `StackPanel`, which gives
its children infinite available height, so the `ListBox` never received a bounded region and could
not reliably scroll once the registry held a realistic number of projects. The enclosing
`StackPanel` in `MainWindow.xaml`'s Projects registry column was replaced with a `Grid` with four
row definitions (`Auto`/`Auto`/`Auto`/`*`) for the search box, status filter, empty/no-match
messaging, and the `ListBox`; the `ListBox` now occupies the trailing star row and gained
`ScrollViewer.VerticalScrollBarVisibility="Auto"`. Verifying this against the real published
executable also surfaced a second, previously dormant defect that only became visible once the
`ListBox` was given a real bounded area: with zero items, the `ListBox`'s default theme chrome
rendered a solid opaque white fill behind the "No projects yet" panel instead of the transparent
`Background="Transparent"` already set on the control (confirmed reproducible via `PrintWindow`
against the real self-contained `win-x64` publish; a pure in-process `RenderTargetBitmap` render of
the identical visual tree did not show it, confirming the defect was specific to the real
hardware/theme-composited render path and not visible by reading XAML alone). Because this is a
direct, in-scope consequence of the `OPUS-01` layout change (the empty `ListBox` was previously
collapsed to near-zero height inside the old `StackPanel` and never rendered a visible area), it was
fixed as part of `OPUS-01`: a new `ProjectRegistryListBoxStyle` (`MainWindow.xaml`) replaces the
`ListBox`'s default control template with an explicit `Border`/`ScrollViewer`/`ItemsPresenter`
template that guarantees a transparent background regardless of the resolved theme.

**OPUS-02 (P2, BLOCKING) — FIXED.** The Projects registry/editor column `MinWidth` values (`235` /
spacer `18` / `330`) summed with the window's chrome/padding to exceed the declared `860x580`
minimum window contract, causing clipping. `Window.MinWidth`/`MinHeight` were left unchanged at
`860`/`580`. The three registry `Grid.ColumnDefinitions` `MinWidth`s were reduced to `200` (left) /
`16` (spacer) / `295` (right), within Sol's suggested ranges, and verified against the real
published executable resized to exactly `860x580`: the editor's right edge, all field labels, and
`Cancel`/`Save changes` remain visible with no horizontal clipping (see evidence below).

**OPUS-03 (P2, BLOCKING) — FIXED.** Both Projects status `ComboBox`es (`SelectedStatusFilter` and
`EditorStatus`) previously used a manually-styled `Style` that only set brush/font setters without a
custom `ControlTemplate`, so the closed box, dropdown popup, and item chrome still fell back to
default light WPF chrome with inadequate contrast against the dark shell. A new reusable
`BrandComboBoxStyle` and `BrandComboBoxItemStyle` were added to
`src/AIUsageMonitor.Desktop/Resources/Controls.xaml`, reusing only existing brand brushes
(`BrandNavyBlueBrush`, `BrandTextBrush`, `BrandBorderBrush`, `BrandCyanBrush`, `BrandFocusBrush`,
`BrandSurfaceRaisedBrush`, `BrandMutedTextBrush`). The template covers the closed state, hover
(`BrandFocusBrush` border), open/keyboard-focus (`BrandCyanBrush` / thicker `BrandFocusBrush`
border), disabled (`Opacity 0.45`), the popup (dark background, border, corner radius, drop shadow,
scrollable item list), and item hover/selection (`BrandSurfaceRaisedBrush` background,
`BrandCyanBrush` selected foreground). `MainWindow.xaml`'s `ProjectInputComboBoxStyle` now reads
`BasedOn="{StaticResource BrandComboBoxStyle}"` instead of duplicating brush setters, so both
Projects `ComboBox`es pick up the new template.

**OPUS-04 (P2, non-blocking, fixed per Sol instruction) — FIXED.** `ApplyFilter()` rebuilds a fresh
`ProjectCardViewModel` for every visible project on every search/filter keystroke. WPF's `Selector`
requires `SelectedItem` to be reference-equal to an instance actually present in `ItemsSource`;
rebuilding always broke that reference, so the real UI selection was silently lost even when the
selected project still matched the active filter. `ApplyFilter()` now captures the selected
project's id before rebuilding `FilteredProjects`, then re-resolves `SelectedProjectCard` to the
matching new instance by id (or `null` if the project no longer matches). `ReplaceProject(saved)`
(used after a successful save) was changed the same way: it now selects the exact new instance from
`FilteredProjects` by id, or `null` if the saved project fell out of the active filter — it no longer
constructs a detached `ProjectCardViewModel` that is not actually present in the bound collection.

Six new regression tests were added to `tests/AIUsageMonitor.Desktop.Tests/ProjectsWorkspaceTests.cs`:
`SearchPreservesSelectionWhenSelectedProjectStillMatches`,
`StatusFilterPreservesSelectionWhenProjectStillMatches`,
`SearchClearsSelectionWhenProjectNoLongerMatches`,
`StatusFilterClearsSelectionWhenProjectNoLongerMatches`,
`SavedProjectSelectionUsesFilteredCollectionInstance`,
`ChangingProjectStatusSoItLeavesCurrentFilterClearsSelection`.

**Full validation:**

| Check | Result |
|---|---|
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln --no-restore` | SUCCESS; 225/225 passing (Domain 28, Provider 46, Infrastructure 86, Connection 10, Desktop 55; up from 219/49, +6 new OPUS-04 regression tests) |
| `win-x64` self-contained single-file publish | SUCCESS (rebuilt after the `OPUS-01` follow-on `ListBox` template fix) |
| `win-x86` / `win-arm64` publish | Not rerun; no project/package/publish configuration changed |
| `git diff --check` | Clean (benign LF/CRLF notices only) |
| Secret-pattern scan of the diff | Clean |

**Runtime verification (real published `win-x64` executable, both required window sizes):** Windows
UI Automation (`System.Windows.Automation`) drove the real, non-degraded published executable — the
"Projects" nav item and "New project" were invoked programmatically, matching the same pattern
established in section -3b. Screen capture used the Win32 `PrintWindow` API with
`PW_RENDERFULLCONTENT`, cropped to the window's true visible bounds via
`DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS)` to exclude the invisible DWM resize-border
margin (a plain `GetWindowRect`-sized capture bled a few pixels of unrelated desktop content into the
bottom edge of the image; the DWM-bounds crop eliminates that). `CopyFromScreen` was evaluated and
rejected for this capture: `SetForegroundWindow` from the automating process was not reliably
honored in this environment, so a `CopyFromScreen` capture at the target window's screen rectangle
was, at least once, observed to capture whichever unrelated window actually had focus instead
(discovered and immediately deleted before being written anywhere durable); `PrintWindow` reads
directly from the target window's own handle regardless of foreground/z-order state and does not
have this failure mode, so it is the capture method actually used for both evidence screenshots.
At `1180x760` and at the exact `860x580` minimum-window-contract size, the real published executable
shows: a real, empty registry (`"0 total"`, `"No projects registered yet."`, not degraded), a fully
visible New Project editor with all fields blank, `Cancel`/`Save changes` reachable, a visible
editor `ScrollViewer` scrollbar, no horizontal clipping of the editor's right edge at the `860x580`
minimum, and the corrected dark `ComboBox` chrome. No cropping/content edits were made beyond the
DWM-bounds crop; both images were inspected before commit and contain no real project names, paths,
repository URLs, tracker ids, or credentials. Evidence paths: `docs/evidence/APO-35-projects-workspace.png`
(`1180x760`) and `docs/evidence/APO-35-projects-workspace-minimum.png` (`860x580`, new).

**Populated-list scroll proof (`OPUS-01`, without writing to the owner's real registry or adding a
production demo mode):** a temporary, non-shipped WPF harness project (scratchpad-only, not part of
the solution or committed to the repository) referenced the real
`AIUsageMonitor.Desktop`/`AIUsageMonitor.Application` assemblies and constructed the real
`MainWindow`/`MainWindowViewModel`/`ProjectsViewModel`/`ProjectRegistryService` production classes,
wired to a sanitized in-memory `IProjectRepository` fake seeded with 40 fake `Sample Workspace NN`
projects (fake `C:\Sample\WorkspaceNN` paths, no real data, following the same in-memory fake-repository
pattern already used by `ProjectsWorkspaceTests.cs`). Nothing in this object graph touches the real
`IProjectRepository`/LocalAppData implementation. Rendered via in-process `RenderTargetBitmap`
(pure WPF render, no OS capture APIs needed since the harness owns the window), this proved the
`OPUS-01` fix at both `1180x760` and `860x580`: the registry list is bounded to its `Grid` star row,
shows `40 total`, and exposes a working vertical scrollbar rather than expanding without limit.
Evidence paths (supplementary, not the two required sanitized evidence files above):
`docs/evidence/APO-35-registry-scroll-proof-1180x760.png`,
`docs/evidence/APO-35-registry-scroll-proof-860x580.png`.

**Out of scope, not performed (per contract):** `OPUS-05`..`OPUS-08` (P3) not implemented; no
rebase/force-push/reset/merge-main; PR #6 left OPEN/DRAFT/UNMERGED; PR #6 not marked ready for
review; APO-35/APO-36/APO-5 not marked Done; no other Story started; no Claude Opus invocation; no
Application-layer change; no new provider/routing/execution/orchestration/database/cloud-backend
behavior.

**Jira synchronization:** one comment posted to APO-35 summarizing this remediation (functional
commit SHA, final branch SHA, `OPUS-01`..`OPUS-04` resolution, focused/full test results, build,
publish, both runtime evidence screenshot paths, populated-scroll proof paths and methodology, P3
deferrals, "Prompt 1/5", next gate). No Jira status transition was made.

**Next planner boundary:** GPT-5.6 Sol delta acceptance of this remediation against the exact final
pushed branch SHA. Do not invoke Opus, merge, or start another Story from this checkpoint.

---

## -3d. APO-35 + APO-36 Delivery Finalization & Squash Merge (New Opus Cycle, Prompt 2/5)

**Lifecycle at this checkpoint:** OPUS-01..OPUS-04 remediation complete -> Sol accepted remediation in Jira comment `11847` -> PR #6 marked ready -> PR #6 squash-merged into `main` at `beab8072551a84aad60df7744135c74c75e51acb` -> local `main` synchronized -> APO-35 and APO-36 transitioned to Done in Jira -> parent Epics APO-5 and APO-4 remain In Progress -> next action is GPT-5.6 Sol planning handoff.

**Key SHAs & PR Artifacts:**
- **Pre-Merge main SHA:** `34569abee50bdb708770e134e9db7db18752a80d`
- **Functional Remediation SHA:** `c06b5163d69daad62402dd7820b3ce7a40498319`
- **Pre-Merge Feature Head:** `bb719abe0fff9a87e73359437269ec403de42841`
- **PR #6:** MERGED (squash merge into `main`)
- **Squash Merge SHA:** `beab8072551a84aad60df7744135c74c75e51acb`

**Validation Baseline:**
- 225 / 225 passing tests (Domain: 28, Provider: 46, Infrastructure: 86, Connection: 10, Desktop: 55).
- Build: 0 warnings, 0 errors.
- `win-x64` self-contained single-file publish verified.
- `git diff --check` and secret scan clean.

**Opus Cadence:**
- Previous Claude Opus 5/5 checkpoint: COMPLETE (initial CHANGES REQUIRED verdict).
- Sol Adjudication (comment `11838`): accepted `OPUS-01`..`OPUS-04` (closed), deferred `OPUS-05`..`OPUS-08` (P3).
- New Opus Cycle: Prompt 1/5 remediation complete; Prompt 2/5 delivery finalization complete.
- Next Opus review checkpoint: around Prompt 5/5 (not immediately).

**Next Action:** GPT-5.6 Sol planning handoff from merged `main` baseline to select and define the next bounded Story. Parent Epics APO-5 and APO-4 remain In Progress.

---

> SINGLE MUTABLE HANDOFF FILE.
> This file is the factual live state and historical validation handoff.
> APO-22 adds the concrete secure-credential-store adapter and is now merged at main SHA
> `40f62f98787df80368eeeca454b223edf8dbd5d9`. APO-23 establishes product identity and a
> branded WPF foundation surface. APO-31 adds only the verified provider adapter surfaces
> documented in `docs/APO-31_PROVIDER_EVIDENCE.md`; technical identifiers, persistence identity,
> and orchestration runtime remain outside this Story.

---

## -2. APO-23 Branding & WPF Product Identity (Current Delivery)

**Starting main SHA:** `f6f04dca89313c9964add3c403a151a7b8b6919e` (APO-22 squash merge)

**Branch:** `feat/APO-23-branding`, rebased from the owner-approved asset commit lineage
`74fc81eb3b11e7a6c1acf7641acbd83ffc952e23`; rebased asset commit `4b7a060`.

**Approved assets and preservation:**

- `assets/Logo.png` — authoritative logo; original bytes preserved.
- `assets/Colors.png` — authoritative palette/usage reference; original bytes preserved.
- `assets/runtime/apo-icon.ico` — derived compact symbol for Windows application/window surfaces.
- `assets/readme/apo-flow.svg` — repository-controlled, lightweight target-flow animation.
- `assets/readme/apo-shell.png` — clean screenshot from the self-contained x64 WPF shell.

**WPF theme/resource structure:**

- `src/AIUsageMonitor.Desktop/Resources/Colors.xaml` — approved palette tokens.
- `src/AIUsageMonitor.Desktop/Resources/Brushes.xaml` — surfaces, accents, states, and gradients.
- `src/AIUsageMonitor.Desktop/Resources/Typography.xaml` — Windows-native display/body typography.
- `src/AIUsageMonitor.Desktop/Resources/Controls.xaml` — cards, buttons, navigation, chips, and focus states.

**Visible product identity changes:**

- Main window title and taskbar/window icon now identify **AI Project Orchestrator**.
- Shell header, welcome surface, README hero, footer, and product metadata use **AI Project Orchestrator (APO)**.
- Startup/shutdown log messages use the APO product identity; the existing log filename remains
  compatibility-sensitive under the preserved `AIUsageMonitor` storage identity.
- `AIUsageMonitor.sln`, project/directories, namespaces, assembly names, test names, and
  `%LOCALAPPDATA%\AIUsageMonitor` remain deliberately unchanged.

**README redesign:**

- Branded hero with approved local logo and factual .NET/WPF/Windows/C# badges.
- Target orchestration flow animation and Mermaid target-flow/architecture diagrams.
- Current status, architecture, operating model, security, compatibility, build/test, roadmap,
  governance, repository tree, documentation links, identity mapping, and screenshot preview.
- No fake CI/build badges, provider claims, routing claims, or completion percentages.

**Validation evidence:**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln` | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 85/85 passing (28 Domain, 7 Provider, 50 Infrastructure) |
| `dotnet publish` `win-x64` | SUCCESS; self-contained single-file publish |
| `dotnet publish` `win-x86` | SUCCESS; self-contained single-file publish; compile/publish evidence only |
| `dotnet publish` `win-arm64` | SUCCESS; self-contained single-file publish; compile/publish evidence only |
| Self-contained WPF launch smoke | SUCCESS on this Windows x64 development machine; title, icon, logo, resources, and shell rendered |
| README local-link check | SUCCESS; 10 local paths checked, none missing |
| README Mermaid fence check | SUCCESS; 2 Mermaid blocks present |
| Approved asset hash check | SUCCESS; `Logo.png` and `Colors.png` match the approved asset commit |
| `git diff --check` | SUCCESS; only expected LF/CRLF normalization notices |

**Scope and limitations:** No provider adapters, routing engine, autonomous runtime, project
registry backend, tracker runtime, full APO-15 dashboard, LocalAppData migration, technical
namespace rename, updater/installer, cloud backend, or CI implementation was started. x86 and
ARM64 were publish-validated from the x64 development machine; no corresponding hardware runtime
execution is claimed. No independent Opus review is required for this Story unless Sol identifies
a new critical issue.

---

## -1. APO-22 Remediation (Opus Review 1 -> Sol Decisions -> Bounded Fix)

Claude Opus 5 performed independent Review 1 against implementation head `de2290a0fb070dd59f8a4c76248a140e019fd35d`.

**Opus Review 1 verdict:** CHANGES REQUIRED (architecture sound; native interop verified correct;
secret handling substantially sound). 1 MAJOR + 7 MINOR findings.

**Sol decisions (final for APO-22):**

- `credentialReference` is a **case-insensitive** identifier. Canonical Windows target casing =
  `credentialReference.ToUpperInvariant()` (invariant, not current-culture; no trimming beyond the
  approved case normalization).
- Permanent APO V1 Windows Credential Manager namespace = `AIProjectOrchestrator:Credential:`
  (replacing the legacy `AIUsageMonitor:Credential:` prefix). No production/provider credentials
  exist yet and APO-31 has not started, so **no migration, dual-read, or old-prefix handling was
  implemented** — none was required.

**Remediation commit:** applied on `feat/APO-22-windows-credential-manager` (see Section 9 Delivery
Record for the exact SHA once pushed).

**Findings addressed:**

| Finding | Resolution |
|---|---|
| MAJOR-1 (case identity undefined) | `WindowsCredentialManagerStore.BuildTargetName` now canonicalizes with `ToUpperInvariant()`; new namespace `AIProjectOrchestrator:Credential:`; case-identity tests added (store/retrieve/remove across differing casings; exact-target-name assertion). |
| MINOR (fake store used ordinal, not case-insensitive, comparer) | `FakeCredentialManagerNativeStore` dictionary now uses `StringComparer.OrdinalIgnoreCase`, matching real Windows Generic Credential TargetName identity. |
| MINOR (unsupported claim that `CRED_PERSIST_ENTERPRISE` has a lower blob-size limit) | Comment in `WindowsCredentialManagerNativeStore` corrected to state only supported, documented behavior (durable across logons, per-user, per-machine, non-roaming); the false blob-size claim was removed. |
| MINOR (native read blob not zeroed before `CredFree`) | `TryRead` now zeroes the native `CredentialBlob` bytes in a `finally` block, immediately before `CredFree`, with `CredFree` still called exactly once and no use-after-free/double-free/leak. |
| MINOR (no deterministic secret-size validation) | `StoreAsync` now validates the UTF-8-encoded secret against the 2560-byte Generic Credential blob limit (`MaxCredentialBlobSizeBytes`) **before** calling the native store, throwing `ArgumentException` (param `secret`, no secret content) without an unmanaged allocation for oversized input; the managed `secretBytes` are still zeroed in `finally`. |
| MINOR (non-asserting file test) | `StoreAsync_DoesNotWriteTheSecretToAnyFileUnderApplicationStorage` now materializes the enumerated file list with `.ToArray()` and asserts it is empty, instead of a `foreach` that could execute zero assertions. |
| MINOR (thin high-value contract coverage) | Added case-identity, exact-target-name, native read/delete failure, retrieve/remove validation, retrieve/remove cancellation, empty-secret, Unicode-secret, and exact/over the 2560-byte boundary tests. |
| Documentation (opaque/case-insensitive contract) | `ISecureCredentialStore` and `ProviderConnection.CredentialReference` XML docs now state the reference is opaque, case-insensitive, and never contains the secret. Method signatures and the storage location of `CredentialReference` were not changed. |

**Corrected blob-size documentation:** the Windows Generic Credential blob limit is
`CRED_MAX_CREDENTIAL_BLOB_SIZE = 5 * 512 = 2560` bytes. This is a property of the Generic Credential
type itself, not of `CRED_PERSIST_LOCAL_MACHINE` specifically — the prior note incorrectly implied a
persistence-mode-specific limit. APO now pre-validates this size at the `WindowsCredentialManagerStore`
boundary before any native call.

**Validation evidence (remediation):**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln` (Debug) | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; **85/85** passing (28 Domain, 7 Provider, 50 Infrastructure) — up from the 64/64 Review-1 baseline (29 prior Infrastructure security/other tests + 21 new case-identity/validation/cancellation/unicode/oversize tests) |
| `git diff --check` | Clean (only the same benign LF/CRLF normalization notice seen in the Review-1 baseline) |
| `dotnet build` Desktop, `-p:Platform=x64 -r win-x64` (Release, self-contained) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=x86 -r win-x86` (Release, self-contained) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=ARM64 -r win-arm64` (Release, self-contained) | SUCCESS; 0 warnings, 0 errors — **compile-only**; no ARM64 hardware execution performed or claimed |
| Secret/diff review | SUCCESS; diff limited to the 6 files listed below; no secret literals outside test fixtures; no assets/README/UI touched |

**Files changed in remediation:**

- `src/AIUsageMonitor.Application/Security/ISecureCredentialStore.cs` — doc-only.
- `src/AIUsageMonitor.Domain/Providers/ProviderConnection.cs` — doc-only.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerStore.cs` — new namespace, `ToUpperInvariant()` canonicalization, deterministic oversize validation.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerNativeStore.cs` — corrected persistence comment, read-blob zeroization before `CredFree`.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/FakeCredentialManagerNativeStore.cs` — `StringComparer.OrdinalIgnoreCase`.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/WindowsCredentialManagerStoreTests.cs` — fixed 2 tests, added 21 new test cases.

**Explicitly out of scope / not touched:** APO-31 provider adapters, `assets/` branding, `README.md`,
WPF UI, LocalAppData migration, solution/project/namespace rename, old-prefix migration/dual-read/
enumeration/cleanup (none required — no production credentials exist under the legacy prefix).

**Review boundary:** Claude Opus 5 must perform independent Review 2 against the remediated head.
GPT-5.6 Sol must accept the Story before merge or before APO-31 may begin.

---

## 0. APO-22 Current State & Delivery Summary

APO-22 implemented `WindowsCredentialManagerStore`, the first concrete adapter behind the existing
`ISecureCredentialStore` Application contract (`src/AIUsageMonitor.Application/Security/ISecureCredentialStore.cs`).
The contract signature was NOT changed.

| Item | Result |
|---|---|
| Starting SHA (expected, verified) | `aab184b6e8dcd1aa9f87012bec0845d4070fb410` (`origin/main` matched local `main`) |
| Branch | `feat/APO-22-windows-credential-manager` |
| Windows mechanism | Windows Credential Manager Generic Credential (`CRED_TYPE_GENERIC`), `CRED_PERSIST_LOCAL_MACHINE` persistence |
| Native APIs | `CredWriteW`, `CredReadW`, `CredDeleteW`, `CredFree` (Advapi32.dll), verified against Microsoft Learn `wincred.h` documentation before implementation |
| Secret encoding on the wire | UTF-8 bytes into `CredentialBlob`; decoded back with `Encoding.UTF8` on read |
| Target name | `AIProjectOrchestrator:Credential:{credentialReference.ToUpperInvariant()}` (namespaces APO's Generic Credentials in the shared vault; case-insensitive since APO-22 remediation — see Section -1) |
| DI registration | `ISecureCredentialStore -> WindowsCredentialManagerStore` (singleton), in `InfrastructureServiceCollectionExtensions.AddInfrastructure` |
| Non-Windows behavior | Throws `PlatformNotSupportedException` before any native call (`OperatingSystem.IsWindows()` guard, injectable for tests) |

### Files changed/added

- `src/AIUsageMonitor.Infrastructure/Security/ICredentialManagerNativeStore.cs` (new, internal) — testable seam over the native calls.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerNativeStore.cs` (new, internal) — P/Invoke implementation.
- `src/AIUsageMonitor.Infrastructure/Security/CredentialManagerNativeException.cs` (new, public) — carries operation, target name, and Win32 error code; never the secret.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerStore.cs` (new, public) — `ISecureCredentialStore` implementation.
- `src/AIUsageMonitor.Infrastructure/AIUsageMonitor.Infrastructure.csproj` (modified) — added `InternalsVisibleTo` for `AIUsageMonitor.Infrastructure.Tests` only, to keep the native seam internal per the execution contract.
- `src/AIUsageMonitor.Infrastructure/InfrastructureServiceCollectionExtensions.cs` (modified) — DI registration.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/FakeCredentialManagerNativeStore.cs` (new) — in-memory native-call fake.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/WindowsCredentialManagerStoreTests.cs` (new) — 14 focused tests.
- `TASK.md` (replaced with the APO-22 contract, now the review checkpoint below).

### Validation evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln` (Debug) | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 64/64 passing (28 Domain, 7 Provider, 29 Infrastructure) — up from the 50/50 baseline (15 prior Infrastructure tests + 14 new) |
| `git diff --check` | Clean (only a benign LF/CRLF normalization notice on `TASK.md`, not a real whitespace defect) |
| `dotnet build` Desktop, `-p:Platform=x64 -r win-x64` (Release) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=x86 -r win-x86` (Release) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=ARM64 -r win-arm64` (Release) | SUCCESS; 0 warnings, 0 errors — **compile-only**; no ARM64 hardware execution performed or claimed |
| Real native round-trip smoke check | Performed manually on this Windows x64 dev machine only (temporary test, added, run, then deleted before commit): write/read-back exact bytes/update/delete/confirm-not-found all succeeded against the actual Windows Credential Manager vault, then the credential and the scratch test file were both removed. `cmdkey /list` confirmed no `AIUsageMonitor` credential remained afterward. This is not part of the committed automated suite (the committed suite uses the fake per the execution contract), so it is not repeatable evidence for the reviewer to rerun — it only records that the native P/Invoke path was exercised once against real Windows Credential Manager during implementation. |
| Secret/diff review | SUCCESS; searched the diff for the test secret literals used in fixtures (e.g. `super-secret-token`, `codex-secret-value`) — they appear only in test files, never in generated output, logs, or persisted JSON/JSONL |

### Known limitations

- Test requirement "non-Windows guard behavior where reasonably testable" is covered by an
  injectable `Func<bool> isWindows` seam exercised in
  `NonWindowsPlatform_FailsTruthfullyWithoutCallingTheNativeStore` — this proves the guard logic
  deterministically, but does not (and cannot, on this Windows-only dev/CI environment) prove
  actual execution under a non-Windows OS.
- ARM64/x86 evidence above is compile-only, performed on an x64 development machine; no ARM64 or
  x86 hardware runtime execution was performed or is claimed.
- `CRED_MAX_CREDENTIAL_BLOB_SIZE` is 2560 bytes, a property of the Generic Credential type itself
  (not specific to `CRED_PERSIST_LOCAL_MACHINE`). Since the APO-22 remediation (Section -1),
  `WindowsCredentialManagerStore.StoreAsync` pre-validates the UTF-8-encoded secret against this
  limit and throws `ArgumentException` before any native call, instead of relying on a native
  Win32/RPC failure.
- APO-31 (provider capacity adapters) was explicitly NOT started. No provider connection UX, no
  Codex/Claude/Kimi/Copilot/Antigravity adapter code, no LocalAppData migration, and no
  solution/project/namespace rename were touched.

### Review boundary

This section records the original APO-22 implementation as reviewed in Opus Review 1. Claude Opus 5
returned CHANGES REQUIRED against that state; the bounded remediation applied on top of it is
recorded in Section -1. The Sol-accepted APO-22 head was verified unchanged, PR #1 was marked
ready, and it was squash-merged into `main` at `f6f04dca89313c9964add3c403a151a7b8b6919e`.
`main` remains unchanged after the APO-23 branch work.

---

## 1. APO-20 Current State & Identity Rename Summary

APO-20 completed the controlled repository and physical local project-root identity rename. The
active GitHub repository is `Hossam1104/AI-Project-Orchestrator`, and the local root is
`D:\AI Tools\Hossam\AI-Project-Orchestrator`.

| Item | Result |
|---|---|
| Historical old repository | `Hossam1104/AI-Usage-Monitor-Tool` |
| New repository | `Hossam1104/AI-Project-Orchestrator` |
| Historical old local root | `D:\AI Tools\Hossam\AI Usage Monitor Tool` |
| New local root | `D:\AI Tools\Hossam\AI-Project-Orchestrator` |
| Verified starting SHA | `9659bf65bda4defc91b2383cf7f195637678485f` |
| Prior APO-20 implementation merge SHA | `861dc99` |
| Finalization starting SHA | `cba84d57c88d95d93fe360f50ff2e961bdb13168` |
| Origin | `https://github.com/Hossam1104/AI-Project-Orchestrator.git`; verified with fetch |
| Physical local-root rename | COMPLETE; verified from the authoritative hyphenated root |
| Historical first rename attempt | BLOCKED by a Windows process lock; resolved before this finalization |
| Technical rename | Not performed; solution/project/namespace/assembly/test/persistence identifiers remain unchanged |

### APO-20 Validation Evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all projects up to date |
| `dotnet build AIUsageMonitor.sln` | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 50/50 passing (28 Domain, 7 Provider, 15 Infrastructure) |
| `git diff --check` | SUCCESS; clean formatting |
| Technical source scope | SUCCESS; no `src/`, `tests/`, `.csproj`, `.sln`, namespace, assembly, or persistence rename |
| Secret/artifact review | SUCCESS; no credentials or untracked generated artifacts introduced |

The older `ae712335696d827a7a1a2d2464cf667f43430c33` SHA in the APO-19 legacy-map assessment is
retained as historical evidence. The APO-19 accepted final main SHA is
`9659bf65bda4defc91b2383cf7f195637678485f`; the repository state verified for APO-20 began at
that accepted SHA.

---

## 2. APO-19 Current State & Inventory Summary

APO-19 completed the formal repository inventory, code inspection, and architectural reuse classification for the AI Project Orchestrator. All active solution files, Domain entities, Application contracts, Infrastructure persistence components, WPF desktop composition, publish profiles, and historical Git commits were analyzed against `docs/BRD.md` and the 17 approved APO Epics.

The comprehensive durable mapping artifact is delivered at `docs/LEGACY_IMPLEMENTATION_MAP.md`.

### Classification Summary Counts (17 Areas Evaluated)
- **Reuse As-Is (5)**: Dynamic Quota normalization & mathematical invariants (`QuotaWindow`), Subscriptions domain model (`Subscription`), UsageSnapshot model (`UsageSnapshot`), Capacity JSON/JSONL repositories, SystemClock abstraction (`IClock`).
- **Reuse With Extension (6)**: .NET 10 solution & platform foundation, Provider/Connection domain, Alerts & Sync domain, Schema-versioned JSON document persistence with atomic replacement & quarantine, Monthly partitioned JSONL event/history persistence with streaming and unterminated tail resilience, Self-contained multi-architecture publish profiles (`win-x64`, `win-x86`, `win-arm64`).
- **Refactor (3)**: Storage layout & `ApplicationDataPaths` (to add project/run/evidence directories and manage eventual root path migration), Desktop/WPF minimal smoke shell (to evolve into MVVM Command Center under APO-15), Providers project (to implement concrete collection adapters for Codex, Claude, Kimi, Copilot, Antigravity).
- **Superseded (3)**: Historical EF Core 10 + SQL Server LocalDB persistence (Session 03), Historical WinUI 3 / Windows App SDK shell (Session 01/02), Legacy numbered provider roadmap (Sessions 04–20 in `SESSION_PROMPTS.md`).
- **Remove (0)**: Zero dead/unsafe code in current working tree.

### Major Reusable Foundations
- Dynamic quota window normalization and strict mathematical invariants preventing double-inversion bugs and percentage/absolute value contradictions.
- Resilient local file persistence: schema-versioned JSON with atomic replacement, temporary files, in-process synchronization, and corrupt-file quarantine.
- Monthly-partitioned JSONL streaming queries with automatic unterminated-tail detection and newline recovery.
- Storage startup resilience: safe LocalAppData initialization with graceful fallback to degraded no-persistence mode.
- Self-contained single-file publish profiles targeting Windows 10 (build 17763) and Windows 11 on x64, x86, and ARM64.

### Key Refactor Areas
- `ApplicationDataPaths`: Expand directory hierarchy for registered projects, orchestration runs, validation evidence, review findings, and planning contracts.
- Desktop Shell (`MainWindow`): Refactor minimal smoke shell into rich MVVM Command Center with HUD and Tray components.
- Provider Adapters: Implement concrete, verified collection mechanisms for Codex, Claude, Kimi, GitHub Copilot, and Antigravity.

### Major Gaps for Full APO Capabilities
- Project Registry and workspace isolation (APO-5).
- Git and GitHub integration adapters (APO-6).
- Jira and Azure DevOps work-item integration adapters (APO-7).
- AI Agent / Model registry and execution connectivity (APO-8).
- Intelligent quota-aware model routing engine (APO-9).
- Bounded autonomous execution runtime (APO-11).
- Independent validation and evidence capture engine (APO-12).
- Independent review and remediation loop engine (APO-13).
- Acceptance and human approval gate enforcement (APO-14).
- Command center UI, HUD, and system tray (APO-15).
- Activity, audit, and notification event streams (APO-16).
- GitHub Actions CI/CD pipeline and multi-architecture release qualification (APO-17).

### Baseline Validation Results
| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all 8 projects restored |
| `dotnet build AIUsageMonitor.sln` | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 50/50 passing (28 Domain, 7 Provider, 15 Infrastructure) |
| `git diff --check` | SUCCESS; clean formatting |
| Active legacy dependency scan | SUCCESS; 0 active references to EF Core, SQL Server, LocalDB, WinUI, Windows App SDK, SQLite, Node, Chromium |
| Source/config scope scan | SUCCESS; no files in `src/`, `tests/`, or `.csproj` modified |
| Secret/artifact review | SUCCESS; no credentials or untracked artifacts introduced |

**NO PRODUCT SOURCE CODE WAS MODIFIED IN APO-19.**

---

## 3. Historical Planner Boundary After APO-19

Before APO-20, GPT-5.6 Sol was required to review the legacy implementation map and proceed with
Jira backlog management:

1. Review `docs/LEGACY_IMPLEMENTATION_MAP.md`.
2. Review the recommended backlog item titles and target Epics, then create and approve the real
   Jira backfill and implementation Stories with Jira-assigned keys.
3. Select the first approved implementation Story and issue its execution contract in `TASK.md`.

That historical boundary is superseded by APO-20's repository-identity work. Do not execute APO-2,
providers, orchestration, routing, Jira/GitHub adapters, UI redesign, or any other future capability
from this checkpoint.

---

## 4. Approved Active Target Architecture

```text
WPF + MVVM
      |
Application contracts/services
      |
Domain

Infrastructure -> JSON/JSONL, secure credentials, logging, OS integration
Providers/integrations -> verified collection and normalization

Release -> self-contained .NET 10 Windows artifacts
Database/ORM -> none in V1
```

The active V1 target is C#/.NET 10, WPF, MVVM, modular clean architecture, System.Text.Json, JSON/JSONL local persistence, secure external credential storage, focused xUnit tests, GitHub Actions, and self-contained Windows artifacts. Windows 10 build-17763/Windows 11 compatibility is the engineering goal with x86/x64/ARM64 consideration and x64 primary validation.

Historical EF Core, SQL Server, LocalDB, WinUI, and Windows App SDK work is not active runtime architecture. It remains historical project evidence only.

---

## 5. Jira Baseline

The approved initial Jira hierarchy is created under project `APO`:

| Key | Capability |
|---|---|
| APO-1 | APO Product Rebrand & Governance Rebaseline |
| APO-2 | Windows Platform & Application Foundation |
| APO-3 | Local Persistence, Resilience & Security Foundation |
| APO-4 | AI Usage, Subscription & Capacity Monitoring |
| APO-5 | Project Registry & Workspace Management |
| APO-6 | Git & GitHub Integration |
| APO-7 | Jira & Azure DevOps Work-Item Integration |
| APO-8 | AI Agent / Model Registry & Connectivity |
| APO-9 | Intelligent Model Routing & Quota-Aware Decisioning |
| APO-10 | Planning & Execution Contracts |
| APO-11 | Autonomous Execution Runtime |
| APO-12 | Validation & Evidence Engine |
| APO-13 | Independent Review & Remediation Engine |
| APO-14 | Acceptance & Human Approval Gates |
| APO-15 | Command Center & Project UX |
| APO-16 | Activity, Audit, History & Notifications |
| APO-17 | Packaging, Compatibility, CI & Release Quality |

Recommended backfill Stories under these Epics are detailed in `docs/LEGACY_IMPLEMENTATION_MAP.md` Section 14.

---

## 6. Current Source and Reusable Foundation

The repository currently contains:

- `AIUsageMonitor.sln` and the existing project structure;
- Domain models and invariants for providers, subscriptions, dynamic quota windows, usage snapshots, alerts, sync, and opaque credential references;
- Application contracts for provider discovery/refresh, repositories, usage aggregation, subscriptions, settings, alert evaluation, time, and secure credentials;
- WPF desktop composition and a resilient empty/no-provider shell with degraded mode;
- Infrastructure JSON state stores and monthly JSONL history/event stores under LocalAppData;
- Schema-aware JSON, atomic replacement, synchronized writes, corruption classification, optional state isolation, last-known-good behavior, and interrupted-tail handling;
- Material-change duplicate suppression and latest/range history queries;
- xUnit Domain, Provider, and Infrastructure tests (85 tests; 28 Domain, 7 Provider, 50 Infrastructure);
- x86/x64/ARM64 target configuration; and
- Self-contained publish profiles for the three Windows RIDs.

The current source uses `AIUsageMonitor` technical names while the WPF surface now presents the
AI Project Orchestrator identity through the branded foundation shell. Technical names remain
future mapping/refactor candidates; they are not user-facing product-governance claims.

---

## 7. Historical Implementation Record

### Session 01 - Repository and Solution Foundation
Completed under the earlier product identity and architecture. Created the solution, project layout, build/editor settings, Git ignore rules, initial README/governance, and a minimal desktop shell.

### Session 02 - Domain and Application Architecture
Completed 08 August 2026. Added provider-independent domain models and application contracts for dynamic quotas, used/remaining normalization, subscriptions, usage snapshots, alerts, sync, provider discovery/refresh, settings, and secure credentials.

### Session 02R - Domain Integrity Remediation
Completed 08 August 2026. Corrected last-known-data retention on provider failure, absolute and percentage quota consistency, subscription confidence, opaque credential references, and related contracts/tests.

### Session 03 - EF Core + SQL Server LocalDB Persistence (Historical / Superseded)
Completed 08 August 2026. Implemented EF Core 10, SQL Server LocalDB, migrations, repositories, duplicate-snapshot suppression, LocalDB initialization, and real LocalDB integration tests. Superseded in Session 03R by the zero-prerequisite consumer requirement.

### Session 03R - Portable Consumer Desktop Architecture Migration
Completed 08 August 2026. Converted desktop foundation to WPF, implemented JSON/JSONL file persistence, retained provider-independent contracts, and established self-contained publish profiles.

### Session 03R-F - Portable Foundation Resilience Remediation
Completed 08 August 2026. Guarded LocalAppData storage initialization, added degraded no-persistence startup behavior, optimized newest-partition latest lookup, and added unterminated JSONL tail handling.

### APO-18 - Governance Rebaseline & Consolidated BRD
Completed 21 August 2026. Consolidated `docs/BRD.md` as the single authoritative BRD, updated repository governance for Jira project `APO`, and established the six-role AI operating model.

### APO-19 - Legacy Implementation Inventory & APO Reuse Map
Completed 21 August 2026. Conducted complete code inspection, categorized all components into 5 reuse classifications, documented in `docs/LEGACY_IMPLEMENTATION_MAP.md`, and formulated title- and target-Epic-based Jira backfill recommendations. Jira keys remain for Sol/Jira to assign.

---

## 8. Historical Architecture Decisions Retained

- Portable WPF/JSON/JSONL foundation is the approved active architecture.
- Historical EF/LocalDB and WinUI implementations remain recorded as completed historical milestones.
- Dynamic quota windows and mathematical normalization invariants are preserved.
- Used/remaining contradictions are rejected at construction.
- Last-known valid provider data is retained on refresh failure and marked stale/error.
- Opaque credential references are stored in JSON; raw secrets remain in Windows Credential Manager.
- JSON/JSONL stores enforce schema metadata, atomic temporary-file replacement, in-process synchronization, duplicate suppression, streaming queries, and corruption quarantine.
- Cross-Windows compatibility (Windows 10 1809+ / Windows 11) is a permanent contract.
- APO-20 renamed the active repository and local project folder identities; technical solution,
  project, namespace, assembly, test, and persistence identifiers remain unchanged.
- APO-22 implements the first concrete `ISecureCredentialStore` adapter: Windows Credential
  Manager Generic Credentials, `CRED_PERSIST_LOCAL_MACHINE` persistence, secrets never written to
  JSON/JSONL/logs.
- APO-22 remediation (Section -1): `credentialReference` is case-insensitive; canonical Windows
  target casing is `ToUpperInvariant()`; the permanent vault namespace is
  `AIProjectOrchestrator:Credential:`.

---

## 9. Delivery Record

| Story | Branch | Implementation Commit | Merge Commit | Delivery Date | Status |
|---|---|---|---|---|---|
| APO-18 | `refactor/APO-18-product-governance-rebaseline` | `7d70dae` | `56f4ea7` | 21 Aug 2026 | COMPLETE |
| APO-19 | `docs/APO-19-legacy-implementation-map` | Pending | Pending | 21 Aug 2026 | COMPLETE |
| APO-20 | `docs/APO-20-finalize-local-root` | `138c51f` | `138c51f` (fast-forward) | 21 Aug 2026 | COMPLETE |
| APO-22 | `feat/APO-22-windows-credential-manager` | `de2290a`, `55e1b74`, `b1b97de` | `f6f04dc` (PR #1 squash) | 22 Aug 2026 | COMPLETE / SOL-ACCEPTED |
| APO-23 | `feat/APO-23-branding` | `4b7a060`, `fe95991` | `40f62f9` (PR #2 squash) | 22 Aug 2026 | COMPLETE / SOL-ACCEPTED |
| APO-31 | `feat/APO-31-provider-capacity-adapters` | `7a5bd9b` | `a585ed4` (main) | 22 Aug 2026 | COMPLETE / MERGED / DONE |

---

## 10. Next Planner Boundary

APO-20 repository and physical local-root rename work is complete. APO-22 was accepted and merged
through PR #1 at `f6f04dca89313c9964add3c403a151a7b8b6919e`. APO-23 implemented the approved
branding assets, WPF resources, product identity, shell polish, README redesign, and validation
evidence and was accepted through PR #2 at `40f62f98787df80368eeeca454b223edf8dbd5d9`.

APO-31 implementation is complete in commit `7a5bd9b7ec84866bd7e4ded603337dbb59d57299` on
`feat/APO-31-provider-capacity-adapters`. Draft PR #3 is open against `main`; its provider
evidence matrix is `docs/APO-31_PROVIDER_EVIDENCE.md`. This Story must remain unmerged pending
GPT-5.6 Sol acceptance. Do not execute another Story automatically from this checkpoint.

## 11. APO-31 Provider Capacity Adapters (Current Delivery)

**Starting main SHA:** `40f62f98787df80368eeeca454b223edf8dbd5d9` (APO-23 squash merge)

**Branch:** `feat/APO-31-provider-capacity-adapters`

**Implemented adapters and boundaries:**

- GitHub Copilot personal-user and organization billing usage endpoints are adapted as usage-only
  `Partial` results. No allowance, remaining capacity, reset, plan, or subscription is inferred.
- Anthropic organization Messages Usage Reports are adapted as token usage-only `Partial` results.
  Claude/Claude Code consumer subscription capacity remains detected/manual/unsupported.
- Kimi Code's documented local server usage endpoint is adapted only for a loopback HTTP(S) server
  address and an opaque credential reference. Documented quota rows and reset times are mapped;
  account fields are returned, while unproven plan and monetary extra-usage semantics are omitted.
- Codex and Antigravity official CLIs are safely detected. Their interactive status/usage surfaces
  are not scraped; refresh remains manual/unsupported.
- All five `ProviderCode` values are registered in deterministic enum order and discovered with
  provider isolation.

**Official evidence:** `docs/APO-31_PROVIDER_EVIDENCE.md` records source URLs, authentication
requirements, source semantics, implementation scope, and unsupported/manual boundaries.

**Validation evidence:**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused provider tests | SUCCESS; 20/20 passing |
| Full solution tests | SUCCESS; 98/98 passing (28 Domain, 20 provider, 50 Infrastructure) |
| Self-contained publish checks | SUCCESS; `win-x64`, `win-x86`, `win-arm64` |
| Secret/diff checks | SUCCESS; no secret-pattern matches; `git diff --check` clean before final metadata commit |

**Security and truthfulness:** Provider configuration stores only opaque credential references;
secure-store material is never persisted or included in error text. HTTP response bodies are not
logged or surfaced. Typed failure states and last-known-good retention are tested. Usage reports
remain usage-only, and unsupported consumer subscription surfaces remain explicit.

**Acceptance boundary:** The implementation and bounded remediation are ready for GPT-5.6 Sol
verification. Do not merge APO-31, create or execute a follow-on Story, or broaden provider scope
without Sol direction.

## 11.1 APO-31 SOL remediation / periodic review checkpoint

**Sol initial review:** `CHANGES REQUIRED` against reviewed head
`f98fa7111cdccb3a363590b44c9e79b08ce20bce`.

**Remediation starting state:**

- Branch remained `feat/APO-31-provider-capacity-adapters`; no new branch or PR was created.
- `main` and `origin/main` remained `40f62f98787df80368eeeca454b223edf8dbd5d9`.
- Draft PR #3 remained open, draft, and unmerged.
- The existing implementation architecture and provider contracts were preserved.

**Findings and bounded resolutions:**

1. **Anthropic Admin Usage pagination — DONE.** `MessagesUsageReport` now reads `has_more` and
   `next_page`; the first request preserves `starting_at`, later requests use URL-encoded cursor
   pages, and all token values are aggregated before a usage-only window is published. Blank or
   repeated cursors are malformed. Any later-page HTTP, parse, or cancellation failure prevents an
   incomplete result; a prior complete snapshot is returned stale through `ProviderAdapterBase`.
2. **Kimi credential destination safety — DONE.** Kimi validates an absolute loopback HTTP(S) URI
   using `Uri.IsLoopback` before secure-store retrieval and before setting `Authorization`. Genuine
   `127.0.0.1`, `localhost`, and `[::1]` addresses are accepted; remote IPv4 and DNS hosts fail
   with stable `invalid_configuration` without credential or handler use.
3. **Copilot identity failure typing — DONE.** All `/user` HTTP statuses now route through the
   existing billing-request classifier. Authentication, permission, unsupported, rate-limit, and
   provider-server outcomes retain their established outcome/error/stale semantics. The generic
   identity-unavailable message remains only for successful responses with no usable login.
4. **Kimi remaining evidence — DONE.** The undocumented `remaining` wire property is no longer
   mapped. Provider-supplied `used`, `limit`, and `reset_at` are mapped where present; `QuotaWindow`
   derives `UsedPercentage` and `RemainingPercentage` exactly once.
5. **Kimi subscription mapping — DONE.** First-party Kimi server documentation lists
   `userLevelName` but does not define it as the active membership subscription tier. The adapter
   therefore returns `Subscription = null` while preserving documented account fields.
6. **Kimi Extra Usage — DONE.** Monetary extra usage includes a provider currency code, but the
   current `QuotaWindow` public contract cannot preserve currency identity. APO-31 does not emit a
   misleading generic currency quota; the limitation is documented.

7. **Anthropic pagination query preservation - DONE.** Sol discovered one final query-preservation
   issue after the first remediation: subsequent pages dropped `starting_at`. The effective
   `starting_at` is now captured once and preserved across all Anthropic pages, with regression
   coverage verifying the page 1/page 2 query invariant and clock stability.

**Provider matrix after remediation:**

- Codex: official CLI detection only; consumer quota remains unsupported/manual.
- Claude: official CLI detection only for consumer subscription; Anthropic Admin API remains a
  separate organization token-usage channel with complete multi-page pagination.
- Kimi: experimental documented local API only with explicit opaque credential reference and
  loopback-only HTTP(S) destination; used/limit/reset quota and account fields; no inferred plan or
  currency window.
- GitHub Copilot: official personal-user and organization billing usage endpoints, usage-only
  partial results, with typed `/user` subject failures.
- Antigravity: official `agy` detection only; interactive usage remains unsupported/manual.

**Runtime configuration boundary:** The adapters and secure credential-reference seam exist, but the
current WPF foundation shell only registers default options through `services.AddProviders()`. It
does not yet expose Copilot, Anthropic, or Kimi connection/settings controls. Tests configure
options directly. No environment-variable or plaintext `appsettings` secret storage was added.

**Validation evidence:**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused provider tests | SUCCESS; 40/40 passing (20 existing plus 20 remediation regressions) |
| Full solution tests | SUCCESS; 118/118 passing (28 Domain, 40 provider, 50 Infrastructure); final pagination query regression included |
| `win-x64` self-contained publish | SUCCESS; existing single-file profile |
| x86/ARM64 publish | Not rerun; no project/package/publish configuration changed |
| `git diff --check` | SUCCESS; only benign LF/CRLF normalization warnings from Git |
| Secret-pattern and secret-content review | SUCCESS; no provider secret literals in source/docs; test secrets remain sanitized test-only data; unsafe Kimi tests proved zero credential/HTTP use |

**Remediation implementation commit:** `fc1bc6f` (`fix: address APO-31 provider truthfulness review`).

**Final pagination fix commit:** `da9a953153797592a4feaca19edf446241333d37`
(`fix: preserve Anthropic usage query across pages`).

**Next authority:** GPT-5.6 Sol must verify this final pagination micro-fix and decide the scheduled
periodic/critical independent review checkpoint. PR #3 remains draft/unmerged; do not merge, invoke
Opus directly, create or execute another Story, or broaden provider scope from this checkpoint.

## 11.2 APO-31 Independent-Review Remediation Final Bounded Pass

**Scheduled independent review:** Completed against the immutable feature head
`11ba9ddc085a886f27db5c9bb5632982042217ed`; verdict was `CHANGES REQUIRED`. No second Opus review
was invoked for this remediation pass.

**Findings addressed:**

- **O-01:** Anthropic Messages Usage `limit=1000` was removed after re-verifying the current
  first-party API reference. The request now omits the bucket-width-dependent optional `limit`,
  captures `starting_at` once per refresh, and preserves all non-pagination query parameters while
  URL-encoding cursor `page` values.
- **O-02:** The named Anthropic Admin API HttpClient now uses `AllowAutoRedirect = false`. 3xx
  responses are typed `provider_error` failures and are not followed; no redirect destination
  receives the `x-api-key`, and no response body, Location, or key is copied to public errors.
- **O-03:** Claude and Copilot strict numeric parsing rejects present malformed, null, negative,
  non-finite, and non-numeric values as `malformed_response`. Copilot validates both gross and net
  quantities before retaining its existing gross-over-net selection. Mixed malformed refreshes do
  not publish a smaller total; last-known-good data is retained stale.
- **O-04:** `docs/APO-31_PROVIDER_EVIDENCE.md` was corrected with the current Anthropic source,
  request/redirect semantics, and actual totals.

**Remediation commit:** `40b26ee1d8a9a6a0c4052f45d96185564d99c20d`
(`fix: remediate APO-31 independent review findings`).

**Final validation:**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused provider tests | SUCCESS; 45/45 passing |
| Full solution tests | SUCCESS; 123/123 passing (28 Domain, 45 Provider, 50 Infrastructure) |
| `git diff --check` | SUCCESS; only benign Git LF/CRLF normalization notices |
| Secret-pattern / known-secret review | SUCCESS; no real credentials or provider secret literals; sanitized test fixture only |
| `win-x64` self-contained single-file publish | SUCCESS |
| `win-x86` / `win-arm64` publish | Not rerun; no project/package/publish configuration changed; previous evidence retained |

**Final branch state:** The remediation is on `feat/APO-31-provider-capacity-adapters` with Draft
PR #3 still `OPEN`, `DRAFT`, and `UNMERGED`. `main` remains at
`40f62f98787df80368eeeca454b223edf8dbd5d9`. The final pushed remediation commit is
`40b26ee1d8a9a6a0c4052f45d96185564d99c20d`; the handoff metadata commit follows this state.

**Next planner boundary:** GPT-5.6 Sol final delta acceptance against the exact final branch SHA.
Do not merge, modify `main`, invoke another reviewer, start another Story, or authorize Provider
Settings or the Capacity Dashboard from this checkpoint.

## 12. APO-34 First Usable AI Capacity Workspace

**Starting baseline:** `a585ed40ea0e8652c50e4627ee66f7109c67d591`, verified as the exact local
`main` and `origin/main` head before implementation.

**Branch:** `feat/APO-34-ai-capacity-workspace`

**Implementation status:** COMPLETE at `5e3d9ca` on the feature branch; awaiting GPT-5.6 Sol
acceptance. Draft PR #4 is open against `main` and remains unmerged. No Jira write or independent
Opus review was performed.

**Implemented scope:**

- Added additive non-secret provider connection configuration to the existing
  `ProviderConnection` JSON record and reused the existing repository, atomic JSON store, and
  Windows Credential Manager adapter.
- Added `ProviderConnectionService` with the permanent
  `AIProjectOrchestrator:Credential:` vault boundary, opaque references, staged credential
  replacement, atomic JSON commit ordering, rollback cleanup, old-reference cleanup, and
  remove-credential ordering.
- Added Copilot personal/organization scope configuration, optional personal username,
  organization validation, Anthropic organization Admin API labelling, and Kimi loopback
  HTTP(S) server validation with the documented default `http://127.0.0.1:58627/`.
- Added an immutable copy-on-write runtime settings snapshot seam. Connection saves and startup
  loading apply settings to the already registered singleton adapters for the next refresh
  without restart; providers capture one snapshot per refresh.
- Replaced the static shell body with the bounded MVVM Overview + AI Capacity workspace. AI
  Capacity is the initial workspace; Overview remains navigable; Projects, Agents, and Activity
  remain visibly disabled/planned.
- Added five stable capacity cards in enum order, truthful configured/detected/unsupported/
  authentication/error/stale states, usage-only rendering without invented remaining values,
  remaining-percentage presentation, local reset display, isolated asynchronous refresh, and
  no-overlap Refresh All behavior.
- Added connection editor windows for Copilot, Claude/Anthropic organization API, and Kimi. The
  editor never reads a saved secret back and only sends a newly entered secret to the secure
  save service.
- Added runtime evidence at `docs/evidence/APO-34-ai-capacity-workspace.png`.

**Validation evidence:**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| Baseline Domain / Provider / Infrastructure tests | SUCCESS; **123/123** (28 / 45 / 50) |
| New desktop tests | SUCCESS; **12/12** |
| New connection transaction tests | SUCCESS; **3/3** |
| WPF solution test host | SUCCESS under x64 mapping; final test/build pass had 0 warnings or errors |
| x64 self-contained single-file publish | SUCCESS; `win-x64` profile |
| x86 self-contained single-file publish | SUCCESS; `win-x86` profile; compile/publish evidence only |
| ARM64 self-contained single-file publish | SUCCESS; `win-arm64` profile; compile/publish evidence only |
| x64 runtime smoke | SUCCESS; published executable launched and rendered the AI Capacity workspace |
| Runtime screenshot | SUCCESS; inspected `docs/evidence/APO-34-ai-capacity-workspace.png` |
| Secret/diff review | No real credential; only synthetic test fixture text, never persisted or logged |
| Live authenticated provider calls | NOT RUN; prohibited by the execution contract |

**Known limitations:**

- The computer-use native helper was unavailable in this environment. The runtime screenshot was
  captured from the launched local x64 process with a local window-capture fallback and visually
  inspected; no authenticated provider interaction was attempted.
- x86 and ARM64 were publish-validated from the x64 Windows development machine; no corresponding
  hardware runtime execution is claimed.
- Provider capacity remains subject to the official/manual boundaries documented by APO-31;
  no new provider or speculative endpoint was added.
- Full release qualification, signing, installer/updater work, and the future orchestration
  runtime remain outside APO-34.

**Acceptance boundary:** GPT-5.6 Sol must inspect the implementation and evidence, then accept or
request bounded remediation. Do not merge this branch, invoke Opus, write Jira, or execute another
Story automatically from this checkpoint.

## 12.1 APO-34 SOL acceptance remediation

**Starting implementation head:** `cf0a2241c92fbf1200ec3e5ca886672657d0813a`.

**Sol verdict:** CHANGES REQUIRED; bounded remediation was performed on the same
`feat/APO-34-ai-capacity-workspace` branch and Draft PR #4.

**Remediation implementation commit:** `02c7ee3b91f08d0903aef620cd616675ee8f859b`.

**Parent Epic correction:** APO-34 is under APO-4 - AI Usage, Subscription & Capacity Monitoring.
APO-31 is recorded above as COMPLETE / MERGED / DONE at main SHA
`a585ed40ea0e8652c50e4627ee66f7109c67d591`; its historical remediation and evidence sections are
preserved.

**R-01 result lifecycle:** every non-stale provider result replaces quota windows, including empty
results; account and subscription values clear when absent; stale results display their supplied
payload and retain the last successful timestamp.

**R-02 refresh persistence:** connection edits now produce `Updating` when a credential/configuration
edit remains configured and `NotConfigured` when removed. `RecordRefreshAsync` maps the already
completed provider result without another provider call or secure-store read, preserves credential
and configuration values, records attempt/success/error metadata, and isolates local persistence
failure from truthful provider status.

**R-03 live settings:** the Copilot regression test uses one provider instance and one runtime
settings accessor across organization A and organization B and verifies both request paths.

**R-04 degraded startup:** the five-card surface is safe when persistence is unavailable. Detection
uses only the executable locator for `codex`, `claude`, `kimi`, and `agy`; it does not read or write
credentials/persistence or call providers/network, and disables editing and refresh. Overview now
exposes an observable persistence warning state.

**Validation:** final solution validation records Domain 28, Provider 46, Infrastructure 50,
Desktop 20, and Connection 10 tests (154 total), a successful restore/build with 0 warnings and 0
errors, diff/secret review, and a successful x64 self-contained single-file publish. x86 and ARM64
publish evidence is retained because no project, package, or publish configuration changed. The
published x64 executable opened with a live `AI Project Orchestrator` window; direct Computer Use
visual inspection was unavailable after native-helper retry/reset, and no live authenticated
provider calls were run.

**Delivery state:** APO-34 is COMPLETE / MERGED / DONE on `main` at
`4b393b3e3cf732dd1f0e861a734e3c311327e2af`. Its accepted Draft PR #4 delivery is historical from
this checkpoint. No APO-34 follow-up commit was made; the next active authority is GPT-5.6 Sol for
APO-27 acceptance. No Jira write, Opus review, or downstream Story work is authorized from this
checkpoint.

## 13. APO-27 Project / Orchestration Storage Foundation (Historical Initial Delivery)

**Exact merged main base:** `4b393b3e3cf732dd1f0e861a734e3c311327e2af`.

### Corrected APO-34 local-main reconciliation

- Old local `main` before reconciliation was `a585ed40ea0e8652c50e4627ee66f7109c67d591`.
- `origin/main` was verified after `git fetch origin --prune` at
  `4b393b3e3cf732dd1f0e861a734e3c311327e2af`.
- The only working-tree change was `AIUsageMonitor.sln`, and its complete local delta was the
  four existing Desktop `Debug|Any CPU` / `Release|Any CPU` mappings from `x86` to `x64`.
- The complete `HEAD..origin/main` APO-34 delta contained those same four replacements plus the
  accepted `AIUsageMonitor.Desktop.Tests` and `AIUsageMonitor.Connection.Tests` project entries,
  configurations, and test-folder nesting.
- Every local changed line was present with the same target value in the incoming APO-34 delta.
  The redundant local solution edit was restored from old `HEAD`, the working tree became clean,
  and `git pull --ff-only origin main` fast-forwarded local `main` to the exact merged SHA.
- The synchronized solution contains both APO-34 test projects and retains the Desktop Any CPU
  `x64` mappings. No APO-34 follow-up commit was made.

### APO-27 implementation (Historical baseline)

**Branch:** `feat/APO-27-orchestration-storage`
**Implementation commit:** `d38817f96050b0decfd0a8328f8ef2cd33bc5a5e`
**Draft PR:** [#5](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/5)
**State:** OPEN / DRAFT / UNMERGED

The implementation preserves the legacy `%LOCALAPPDATA%\AIUsageMonitor\` identity and extends
the existing JSON/JSONL foundation without adding a database, ORM, cloud backend, provider code,
UI, routing engine, executor runtime, or tracker/GitHub execution.

### Storage architecture and evidence

- `ApplicationDataPaths` now exposes `projects.json`, `agents.json`, `routing-policy.json`, the
  `projects\` root, and GUID-derived project paths for `routing-policy.json`, `runs\`,
  `evidence\`, `reviews\`, and `activity\`.
- Project registry and agent/model registry use the existing versioned JSON collection store,
  atomic temporary-file replacement, per-file exclusive synchronization, and corruption/schema
  handling.
- Global routing policy and project-specific overrides use versioned JSON documents. Override
  paths are derived only from a validated project GUID.
- Execution runs, evidence metadata, review metadata, and activity/audit use the existing monthly
  JSONL event store. Records carry `schemaVersion`, `recordType`, and `projectId`; reads filter
  both the derived project directory and the record project id.
- Focused tests cover the layout, project/agent round trips, global and project policy isolation,
  four isolated JSONL streams, cross-project record rejection, chronological month-range reads,
  unsupported-schema quarantine, unterminated-tail recovery, concurrent appends, atomic-write
  cleanup, and sensitive metadata-key rejection.

### Validation evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all projects up to date |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused APO-27 storage tests | SUCCESS; 8/8 passing |
| Full solution tests | SUCCESS; 162/162 passing (28 Domain, 46 Provider, 58 Infrastructure, 10 Connection, 20 Desktop) |
| `git diff --check` | SUCCESS; no whitespace errors; only normal Git LF/CRLF notices during staging |
| Added-line targeted secret scan | SUCCESS; no high-confidence secret literals |
| `win-x64` self-contained single-file publish | SUCCESS; `win-x64` profile |
| x64 startup smoke | SUCCESS; published executable stayed running during the smoke interval and was stopped cleanly |
| x86 / ARM64 publish | Previous evidence retained; no project, package, or publish configuration changed |
| Live authenticated provider calls | NOT RUN; prohibited by the execution contract |

### Limitations and next boundary

- This is a persistence foundation. It does not implement project UI, routing decisions, task
  classification, executor/autonomous runtime, GitHub/Jira/Azure execution, validation/review
  engines, activity UI, provider changes, cloud sync, database/ORM, LocalAppData migration, or
  APO-33 CI.
- The registry and orchestration contracts intentionally persist metadata and references only;
  callers must not place secrets, prompts, source code, authenticated payloads, or raw command
  output in free-form summary/reference values.
- x86 and ARM64 remain publish evidence from the accepted baseline on the x64 development machine;
  no x86/ARM64 hardware runtime is claimed.
- GPT-5.6 Sol must inspect and accept the exact pushed feature-branch state. After Sol acceptance,
  one independent Claude Opus 5 architecture review is required by the APO-27 contract. No Jira
  writes, Opus invocation, merge, or downstream Story work has been performed.

## 13.1 APO-27 SOL Acceptance Remediation (Historical Pre-Opus Baseline)

**Sol acceptance verdict:** CHANGES REQUIRED, recorded in Jira Sol acceptance comment `11776`.

**Exact remediation target:**

- Required remediation start SHA: `5e2caa596ba8e67d14ee359302f210149eadb397`.
- Exact `origin/main` base: `4b393b3e3cf732dd1f0e861a734e3c311327e2af`.
- Branch: `feat/APO-27-orchestration-storage`.
- Functional remediation commit: `dcfa922b58c0282311ec1e027d1187bee771651b`.
- Draft PR #5 remains OPEN / DRAFT / UNMERGED and targets `main`.

The remediation stayed within the storage foundation. It did not add an orchestration execution
engine, UI, providers, routing, project actions, tracker/GitHub execution, database/ORM, or a
LocalAppData migration.

### R-01 Execution history identity and time

- `ExecutionRun` and `ExecutionRunRecord` now carry `RecordId`, `ProjectId`, `RunId`, `RecordedAt`,
  `Status`, `StartedAt`, optional `CompletedAt`, and the existing non-secret references.
- `RecordId` is generated for new application checkpoints, rejects `Guid.Empty`, and is distinct
  from the lifecycle `RunId`.
- `RecordedAt` is the append/history timestamp. Run JSONL partition selection, range filtering,
  and chronological ordering use `RecordedAt`; `StartedAt` remains lifecycle metadata.
- Recorded timestamps earlier than `StartedAt` are rejected. A regression proves one RunId can have
  an August start checkpoint and an independent September review checkpoint in separate monthly
  partitions, with the September range returning the review checkpoint.

### R-02 Finding-level review traceability

- Added bounded `ReviewFindingMetadata` and persisted `ReviewFindingMetadataRecord`.
- Each finding preserves a stable nonblank string `FindingId`, `Severity`, `AffectedReference`,
  separately persisted `Disposition`, `Blocking`, evidence IDs/references, and an optional bounded
  summary.
- `ReviewMetadata` retains its aggregate compatibility fields and now carries a normalized,
  read-only finding collection with duplicate finding IDs rejected.
- Regressions preserve two findings, affected requirements/acceptance references, dispositions,
  severity, blocking flags, and evidence references while keeping review records project-isolated.

### R-03 Activity and evidence traceability

- `ActivityAuditRecord` now carries optional `TaskReference` and normalized immutable `EvidenceIds`.
  The prior singular `EvidenceId` remains readable for compatibility and is folded into the new
  collection without duplicates; empty evidence IDs are rejected.
- `EvidenceMetadata` now carries a normalized read-only `RelatedRequirementReferences` list of
  nonblank metadata-only strings such as `FR-PROJ-003` and `FR-REV-003`.
- Activity regressions cover task linkage, multiple evidence IDs, chronological range reads, and
  malformed/unsupported-record isolation. Evidence serialization remains metadata-only and has no
  raw output or credential payload field.

### R-04 Fail-closed enum validation

Explicit `Enum.IsDefined` validation now protects `ProjectStatus`, `AgentConnectionMode`,
`AgentAvailability`, and `ExecutionRunStatus`. Invalid numeric values from persisted JSON/JSONL
throw `ArgumentException`-compatible validation failures at the application mapping boundary, so
repository/store `TryMap` paths skip them. Tests prove valid sibling project/agent records remain
readable and invalid execution checkpoints are skipped.

### R-05 DefaultBranch semantics

`Project.DefaultBranch` is now nullable for projects without repository configuration. A meaningful
repository provider, URL, ID, or metadata configuration still requires a nonblank branch; a
non-repository project accepts null/blank and round-trips it. Repository-backed `main` remains
valid.

### R-06 Final storage layout

The legacy root and routing policy location remain unchanged. Project orchestration streams now
derive from the explicit orchestration seam:

```text
%LOCALAPPDATA%\AIUsageMonitor\
    projects\
        {project-guid}\
            routing-policy.json
            orchestration\
                runs\
                    YYYY-MM.jsonl
                evidence\
                    YYYY-MM.jsonl
                reviews\
                    YYYY-MM.jsonl
                activity\
                    YYYY-MM.jsonl
```

`ApplicationDataPaths.GetProjectOrchestrationDirectory(Guid)` and
`ProjectDataPaths.OrchestrationDirectory` are the explicit path seam. All four JSONL streams
derive from it; `routing-policy.json` remains directly project-scoped outside `orchestration`.

### R-07 Regression expansion

The focused storage suite increased from 8 to 22 tests and covers the requested project registry,
agent enum, routing isolation, execution checkpoint, evidence, finding-level review, activity,
corruption, unsupported-schema, malformed JSONL, unterminated-tail, valid-partition, concurrency,
atomic-write, and metadata-secret-safety cases.

### Final validation evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all projects up to date |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused APO-27 storage tests | SUCCESS; 22 executed, 22 passed, 0 failed, 0 skipped |
| Full solution tests | SUCCESS; 176 executed, 176 passed, 0 failed, 0 skipped (28 Domain, 46 Provider, 72 Infrastructure, 10 Connection, 20 Desktop) |
| `git diff --check` | SUCCESS; no whitespace errors; only normal Git LF/CRLF normalization notices |
| Added-line secret scan | SUCCESS; 1,004 added lines scanned, 0 high-confidence credential literals |
| `win-x64` self-contained single-file publish | SUCCESS; existing publish profile, executable produced |
| `win-x86` / `win-arm64` publish | Prior accepted evidence retained; no project/package/publish configuration changed |
| x64 startup smoke | SUCCESS; published executable stayed alive for 5 seconds and closed cleanly |
| Live authenticated provider calls | NOT RUN; prohibited by the execution contract |

### Remaining limitations and next boundary

x86 and ARM64 remain compile/publish evidence from the x64 development machine; no corresponding
hardware runtime is claimed. The storage contracts persist bounded metadata and references only;
future callers remain responsible for keeping summaries and references non-secret. Projects UI,
routing, classification, execution runtime, validation/review engines, activity UI, provider
changes, tracker/GitHub execution, cloud sync, database/ORM, LocalAppData migration, installer,
signing, updater, and APO-33 CI remain deferred.

GPT-5.6 Sol's delta acceptance was recorded in Jira comment `11776`. Owner approval comment `11778`
authorized the bounded final remediation recorded in Section 13.2. The exact final functional
review target is `0ea0c65ec7dac7ec09d30a6b25156353b714298f`. The required real Claude Opus 5
independent architecture review is now pending against that exact SHA. No Jira status transition,
Opus invocation, merge, or downstream Story work has been performed.

## 13.2 APO-27 Owner-Approved Final Storage Remediation (Pre-OPUS-01 Baseline)

**Lifecycle at this checkpoint:** APO-27 initial implementation -> Sol CHANGES REQUIRED -> Luna
R-01..R-07 remediation -> Sol delta ACCEPTED FOR INDEPENDENT REVIEW -> Sol-equivalent independent
architecture review identified two approved P2 blockers -> owner approved bounded remediation
comment `11778` -> the prior final storage remediation -> real Claude Opus 5 review pending.

This section records the factual pre-OPUS-01 baseline. Its `0ea0c65ec7dac7ec09d30a6b25156353b714298f`
target and `32/32` focused-test evidence remain historical review evidence; the current lifecycle
and next gate are recorded in Section 13.3.

### Exact delivery identity

| Item | Value |
|---|---|
| Required start head | `a6ef753143996f31591d300dfc56fbfa9bbb50a4` |
| Exact merged main base | `4b393b3e3cf732dd1f0e861a734e3c311327e2af` |
| Original APO-27 implementation | `d38817f96050b0decfd0a8328f8ef2cd33bc5a5e` |
| Prior Sol remediation | `dcfa922b58c0282311ec1e027d1187bee771651b` |
| Owner-approved remediation start | `a6ef753143996f31591d300dfc56fbfa9bbb50a4` |
| New functional remediation | `0ea0c65ec7dac7ec09d30a6b25156353b714298f` |
| Final handoff / exact Opus review target | `0ea0c65ec7dac7ec09d30a6b25156353b714298f` |
| Branch | `feat/APO-27-orchestration-storage` |
| Draft PR | #5, OPEN / DRAFT / UNMERGED, base `main` |
| Jira Story | APO-27, In Progress |
| Jira parent Epic | APO-3, In Progress |
| Owner approval comment | `11778` |

### Blocker A - review aggregate truthfulness

- `ReviewMetadata.Findings` is the authoritative detailed collection.
- `FindingCount` is derived from `Findings.Count` and is no longer independently writable.
- `Blocking` is derived from finding-level blocking flags; a zero-finding review is always
  non-blocking.
- Duplicate `FindingId` values are rejected case-insensitively, blank IDs are rejected, null
  finding entries are rejected, and finding evidence references remain bounded and normalized.
- `ReviewMetadataRecord` retains `FindingCount` and aggregate `Blocking` only as derived serialized
  inspection/compatibility fields. `FromApplication` writes them from the detailed findings and
  `ToApplication` rejects any persisted contradiction before an Application model is created.
- No verdict-text workflow semantics were inferred and no review engine was added.

### Blocker B - orchestration history read truthfulness

- Added the provider-independent Application contract `HistoryReadResult<T>` with `Records`,
  `HistoryReadStatus`, and bounded `HistoryReadIssue` values.
- `Success` means requested partitions were read without storage or record-integrity issues;
  missing files and legitimately empty partitions are `Success` with empty records.
- `Partial` means valid records were preserved but one or more malformed/unsupported records or
  storage partitions may make the requested history incomplete.
- `Unavailable` means no requested partition was reliably read because of permission/I/O failure.
- Issues expose only a safe kind, partition filename, and fixed non-secret message. Exception text,
  absolute paths, raw JSONL records, prompts, source code, and credentials do not cross into
  Application.
- Added additive `JsonlEventStore.ReadRangeWithStatusAsync`; the accepted usage-history
  `ReadRangeAsync` behavior and signature remain unchanged.
- Valid records survive malformed/unsupported siblings and partial month failures. Project ID and
  derived GUID-directory isolation remain enforced before records reach consumers.
- Permission/I/O tests use a small Infrastructure-internal partition-reader seam; no ACL mutation
  or generic filesystem framework was introduced.

### Compatibility and scope

- The legacy `%LOCALAPPDATA%\AIUsageMonitor\` root, existing provider/capacity documents, and
  accepted non-APO-27 filenames/schemas remain unchanged.
- The APO-27 unmerged review record representation remains compatible with its existing derived
  aggregate fields while now failing closed on contradiction.
- No Projects UI, routing engine, execution runtime, validation/review engine, provider change,
  tracker/GitHub execution, cloud backend, database/ORM, LocalAppData migration, installer,
  signing, updater, or APO-33 CI work was performed.

### Deferred non-blocking P3 observations

- **P3-01 Metadata value boundary:** deferred; future integration adapters need explicit metadata
  contracts/allowlists and secret scanning/redaction.
- **P3-02 FilePathLocks lifetime:** deferred; the V1 expected scale does not justify a ref-counted
  keyed-lock redesign.
- **P3-03 RecordId idempotency:** deferred to the future execution runtime; storage append does not
  globally deduplicate an explicitly reused RecordId.

### Final validation evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all projects up to date |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused APO-27 storage tests | SUCCESS; 32 executed, 32 passed, 0 failed, 0 skipped |
| Full solution tests | SUCCESS; 186 executed, 186 passed, 0 failed, 0 skipped (28 Domain, 46 Provider, 82 Infrastructure, 10 Connection, 20 Desktop) |
| `git diff --check` | SUCCESS; no whitespace errors; only normal Windows LF/CRLF notices |
| Added-line secret scan | SUCCESS; 668 tracked added lines scanned, 0 high-confidence credential literals; new contract files reviewed with the same result |
| `win-x64` self-contained single-file publish | SUCCESS; executable produced from the existing profile |
| `win-x86` / `win-arm64` publish | Not rerun; prior accepted evidence retained because project/package/publish configuration did not change |
| x64 startup smoke | SUCCESS; published executable alive after 5 seconds and closed cleanly |
| Live authenticated provider calls | NOT RUN; prohibited by the execution contract |

### Markdown and tracker synchronization state

All tracked Markdown files were enumerated before editing:
`.ai/CURRENT_STATE.md`, `AGENTS.md`, `CLAUDE.md`, `README.md`, `TASK.md`,
`docs/APO-31_PROVIDER_EVIDENCE.md`, `docs/BRD.md`, `docs/IMPLEMENTATION_PLAN.md`,
`docs/LEGACY_IMPLEMENTATION_MAP.md`, and `docs/SESSION_PROMPTS.md`.

The APO-27 factual state was synchronized in `.ai/CURRENT_STATE.md`, `TASK.md`, `README.md`, and
the current-planning boundary of `docs/IMPLEMENTATION_PLAN.md`. `AGENTS.md`, `CLAUDE.md`,
`docs/BRD.md`, `docs/APO-31_PROVIDER_EVIDENCE.md`, `docs/LEGACY_IMPLEMENTATION_MAP.md`, and
`docs/SESSION_PROMPTS.md` remain intentionally unchanged because they contain permanent rules,
requirements, prior-provider evidence, historical mapping, or no stale APO-27 current-state claim.
README does not advertise autonomous orchestration, routing completion, a review engine, or a
Projects UI; it records APO-27 as a storage-foundation change pending review/merge.

At the pre-OPUS-01 checkpoint, current-state language recorded that Sol delta acceptance had
already been completed for independent review. Historical SHA references and prior Story records
remain preserved as evidence; the current post-OPUS-01 lifecycle is recorded in Section 13.3.

At that pre-OPUS-01 checkpoint, Jira remained authoritative and unchanged by that executor:
APO-27 = In Progress, APO-3 = In Progress, owner approval comment `11778`; no duplicate status
transition was made. Completion comment `11787` recorded the prior final-remediation evidence.

### Historical next exact lifecycle gate

Claude Opus 5 must independently review the actual final pushed branch state against
`0ea0c65ec7dac7ec09d30a6b25156353b714298f`, including both blockers, zero-prerequisite consumer
behavior, cross-Windows compatibility, project isolation, persistence integrity, privacy, and
the validation evidence. This was the next gate at that pre-OPUS-01 checkpoint. After the real
Opus gate, GPT-5.6 Sol remains the acceptance authority. PR #5 must remain open, draft, and
unmerged; main must remain untouched; no downstream Story may start automatically.

## 13.3 APO-27 OPUS-01 Final Durability Remediation & Final Merge

**Lifecycle:** APO-27 implementation -> Sol findings -> R-01..R-07 remediation -> Sol delta
acceptance -> owner-approved storage truthfulness remediation -> real Claude Opus 5 review -> Opus
verdict `CHANGES REQUIRED` -> Sol accepted OPUS-01 as blocking -> OPUS-01 remediation at `9350ae53edd4ed75d0158d8da78e4a8cc81ad291` -> Sol delta acceptance COMPLETE (Jira comment `11793`) -> OPUS-01 CLOSED -> PR #5 squash-merged into `main` at `d0efaf01b07b31effa7a432c225e7c913a86258a` -> Jira APO-27 Done -> APO-3 In Progress -> GPT-5.6 Sol planning handoff for APO-5.

### Exact delivery identity

| Item | Value |
|---|---|
| Exact main base before merge | `4b393b3e3cf732dd1f0e861a734e3c311327e2af` |
| Real Opus reviewed target | `0ea0c65ec7dac7ec09d30a6b25156353b714298f` |
| Real Opus verdict | `CHANGES REQUIRED` (review gate COMPLETE; satisfies APO-27 review checkpoint) |
| Accepted blocker | `OPUS-01 - P2` (CLOSED via `9350ae53edd4ed75d0158d8da78e4a8cc81ad291`) |
| Sol adjudication | Jira comment `11790` |
| Current-review state | Jira comment `11791` |
| Functional remediation SHA | `9350ae53edd4ed75d0158d8da78e4a8cc81ad291` |
| Pre-merge feature head | `813afe48ebf3ffc458126b4a5d9a63e33131bc97` |
| Sol delta acceptance | Jira comment `11793` |
| Opus re-review required | NO (cadence Prompt 2/5) |
| PR | #5, MERGED into `main` |
| Squash merge main SHA | `d0efaf01b07b31effa7a432c225e7c913a86258a` |
| Jira Story / Epic | APO-27 Done / APO-3 In Progress |

### OPUS-01 root cause

`VersionedJsonCollectionStore<TRecord>.ReadAsync` retains the accepted read-only behavior of
collapsing every non-usable `FileReadResult` to an empty collection. Both update overloads reused
that method, so `IoFailure` and `PermissionFailure` could become `[]`, invoke the update delegate,
and replace a healthy authoritative registry with only the incoming record.

### VersionedJsonCollectionStore fix

Both update overloads now call one shared private `ReadForUpdateAsync` helper:

- `UpdateAsync(string path, Func<List<TRecord>, List<TRecord>> update, ...)`;
- `UpdateAsync<TResult>(string path, Func<List<TRecord>, (List<TRecord> Items, TResult Result)> update, ...)`.

The helper uses valid items and treats missing/empty documents as safe empty collections. It
preserves the existing corrupt/unsupported-schema quarantine and recovery behavior. It throws a
bounded safe `IOException` for `IoFailure` and a bounded safe `UnauthorizedAccessException` for
`PermissionFailure` before the update delegate or `WriteCoreAsync` can run.

Transient read failures do not quarantine, delete, truncate, or replace the authoritative file.
Cancellation propagates normally. No raw exception text, file path, registry content, credentials,
or authenticated payload crosses the caller boundary. The deterministic read-failure injector is
Infrastructure-internal and unset in production; Application and Domain contracts are unchanged.

### Regression evidence

- Project registry: two valid projects were persisted; injected I/O and permission failures made
  `UpsertAsync` fail; before/after `projects.json` bytes were identical; the incoming project was
  not persisted; both originals remained readable after fault removal.
- Agent registry: two valid agents were persisted; injected I/O failure made `UpsertAsync` fail;
  before/after `agents.json` bytes were identical; the incoming agent was not persisted; both
  originals remained readable after fault removal.
- Shared result-bearing overload: injected I/O failure prevented the delegate from running and
  preserved the exact collection file bytes.
- Existing corrupt/unsupported recovery and normal same-ID replacement/distinct-ID sibling
  preservation tests remain green.

### Validation evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all projects up to date |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| Focused storage tests | SUCCESS; 36 executed, 36 passed, 0 failed, 0 skipped |
| Full solution tests | SUCCESS; 190 executed, 190 passed, 0 failed, 0 skipped (28 Domain, 46 Provider, 86 Infrastructure, 10 Connection, 20 Desktop) |
| `win-x64` self-contained single-file publish | SUCCESS; unchanged profile |
| x64 startup smoke | SUCCESS; published executable alive after 5 seconds and stopped cleanly |
| `win-x86` / `win-arm64` publish | Prior accepted evidence retained; no project/package/publish configuration changed |
| `git diff --check` | SUCCESS; no whitespace errors, only normal Windows LF/CRLF notices |
| Added-line secret scan | SUCCESS; no real secrets or sensitive payloads added |
| Live authenticated provider calls | NOT RUN; prohibited by the execution contract |

### Deferred Opus findings

- **OPUS-02:** Summary/reference length hardening - DEFER.
- **OPUS-03:** Generic metadata-value secret detection - DEFER / CALLER BOUNDARY.
- **OPUS-04:** FilePathLocks lifetime - DEFER.
- **OPUS-05:** RecordId idempotency - DEFER TO EXECUTION RUNTIME.
- **OPUS-06:** Dead JSONL `lineNumber` - DEFER.
- **OPUS-07:** Repository metadata / `DefaultBranch` semantic refinement - DEFER.

### Markdown, BRD, and governance synchronization

All tracked Markdown files were enumerated and searched. Current facts were synchronized in
`.ai/CURRENT_STATE.md`, `TASK.md`, `README.md`, and `docs/IMPLEMENTATION_PLAN.md`. `AGENTS.md`,
`CLAUDE.md`, and `docs/BRD.md` were reviewed and intentionally unchanged; BRD requirement changes
are NONE. Stable historical/provider/mapping Markdown was reviewed and did not require rewriting.

### Jira and PR state

PR #5 is MERGED into `main` at squash merge SHA `d0efaf01b07b31effa7a432c225e7c913a86258a`. APO-27
is transitioned to Done. Parent Epic APO-3 remains In Progress.

### Current next exact lifecycle gate

APO-27 is complete, merged, and Done in Jira. The completed Claude Opus review satisfied the
APO-27 independent review checkpoint (Prompt 2/5 cadence). The next action is GPT-5.6 Sol planning
and decomposition of the first usable Projects workspace Story under Epic APO-5.

## 13.4 APO-37 SOL-37-03b Global Path-Probe Correction (Prompt 4/5)

**Lifecycle:** APO-37 SOL-37-03b bounded correction -> functional implementation -> full
validation -> executor handoff to GPT-5.6 Sol. Claude Sonnet and Claude Opus were not invoked.

### Exact delivery identity

| Item | Value |
|---|---|
| Story / Epic | APO-37 / APO-6 |
| Exact main base | `8a81017b25fe0cfd8efcd4febafd66a1bee6c41e` |
| Pre-correction branch head | `accdf8e745a79327809cbf154c6e2e486726e474` |
| Functional correction SHA | `10aa9529066f94be06808223a90c95a2415ba8b9` |
| Branch | `feat/APO-37-local-git-verification` |
| Draft PR | #7, OPEN / DRAFT / UNMERGED |
| Sol comments | `11851`, `11854` |
| Jira status | APO-37 In Progress; APO-6 In Progress |
| Final branch SHA | Recorded in the final executor completion report after documentation synchronization |

### SOL-37-03b correction

`SystemLocalPathProbe` now coordinates all unresolved system path probes through one process-wide
thread-safe in-flight slot. The caller still waits only up to the configured four-second bound.
Caller timeout and cancellation return control without canceling or evicting the underlying
uncancellable filesystem operation. Same-path callers share the active task; a different path
gets bounded `Unavailable`/`TimedOut` and never receives the first path's result. Completion is
observed, exceptions are consumed safely, the slot is cleared only after the actual task finishes,
and later verification can start one fresh probe.

The maximum unresolved underlying operating-system filesystem probes is therefore **1 globally**.
No Application contract or Git inspector behavior was redesigned, and Git still runs only after
`AvailableDirectory`.

### Regression coverage

Six deterministic `LocalPathProbeTests` regressions cover repeated timeouts, concurrent callers,
cancellation, restart after completion, different-path isolation, and exception cleanup. Existing
available-directory, missing, not-a-directory, unavailable, timeout, cancellation, and
Git-suppression tests remain passing. The real Git integration tests are serialized with these
tests because the production coordinator is intentionally process-global.

### Validation evidence

| Check | Result |
|---|---|
| Focused Infrastructure tests | 172 passed, 0 failed, 0 skipped |
| Focused Desktop tests | 70 passed, 0 failed, 0 skipped |
| Full solution tests | 326 passed, 0 failed, 0 skipped (28 Domain, 46 Provider, 172 Infrastructure, 10 Connection, 70 Desktop) |
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln --no-restore` | SUCCESS; 0 warnings, 0 errors |
| `git diff --check` | SUCCESS |
| Targeted added-line secret scan | SUCCESS; no real credentials found |
| `win-x64` self-contained single-file publish | SUCCESS |

### Runtime evidence

The final published executable is running at:

- ExecutablePath: `D:\AI Tools\Hossam\AI-Project-Orchestrator\publish\win-x64\AIUsageMonitor.Desktop.exe`
- PID: `45940`
- WindowTitle: `AI Project Orchestrator`
- NormalState: non-degraded shell; accessibility inspection reported `CAPACITY READY` and exposed
  the Projects navigation control. The Windows desktop was locked during the final UI automation
  attempt, so direct Projects navigation was not performed; no degraded shell state was observed.
- LEFT RUNNING = YES

### Delivery boundary

Work item: APO-37 SOL-37-03b
Status: COMPLETE at executor boundary / awaiting GPT-5.6 Sol final Prompt-4 delta acceptance

Implemented:
- Global single-unresolved-probe coordination with path isolation, cancellation safety, completion
  eviction, exception observation, and restart-after-completion behavior.
- Six deterministic regression tests and serialized real-probe test collection coverage.

Validated:
- 326 / 326 full-suite tests passed; 0 failed; 0 skipped.
- Build succeeded with 0 warnings and 0 errors.
- Self-contained single-file win-x64 publish succeeded.
- Final executable is alive and left running.

Not validated:
- Claude Opus independent review; this is the next Prompt 5/5 gate if Sol accepts.
- Direct Projects navigation through UI automation because the Windows desktop was locked during
  the final check.

Blockers / limitations:
- No functional blocker. Runtime UI interaction was limited by the locked desktop; process title,
  responsive state, non-degraded accessibility tree, and Projects navigation control were observed.

CURRENT_STATE updated: Yes

Next planner boundary:
- GPT-5.6 Sol final Prompt-4 delta acceptance for the pushed correction.
- If accepted, Prompt 5/5 Claude Opus independent review.
