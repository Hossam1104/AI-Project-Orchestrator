# APO-35 SOL DELTA ACCEPTANCE HANDOFF

Story: APO-35 — Implement First Usable Projects Workspace and Project Registry Management

Epic: APO-5 — Project Registry & Workspace Management

Exact main base: `34569abee50bdb708770e134e9db7db18752a80d`

Feature branch: `feat/APO-35-projects-workspace`

Draft PR: [#6](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/6), OPEN / DRAFT / UNMERGED

Prior functional SHA: `6f3cd6d` (`feat(APO-35): implement first usable Projects workspace`)

Prior handoff SHA: `dbfd080` (`docs(APO-35): record Sol acceptance handoff`)

Delta remediation SHA: to be verified after this commit is pushed and recorded in the final report

## Remediation scope (Sol-authorized, no scope expansion)

This handoff covers exactly two Sol-identified items on top of the existing APO-35 delivery:
SOL-35-01 (edit-target integrity defect) and SOL-35-02 (sanitized runtime visual evidence). No
other file, feature, or unrelated deferred finding was touched.

## SOL-35-01 — edit-target integrity (FIXED)

`ProjectsViewModel.SaveAsync()` previously read `SelectedProject?.Id` at save time, so changing the
list selection (directly, or indirectly through a status-filter change that dropped the in-progress
item from view) while an edit was open could redirect the save to the wrong project.

Fix, confined to `src/AIUsageMonitor.Desktop/ViewModels/ProjectsViewModel.cs`:

- A new `_editingProjectId` field is captured once, in `EditSelectedProject()`, from the project
  being edited, and is the only source used by `SaveAsync()` to resolve the update target.
- `SaveAsync()` fails closed with `Project could not be saved because the edit target is no longer
  available.` if `_editingProjectId` is unexpectedly absent for an edit (not create).
- On a failed save (validation exception, not-found, cancellation, or unexpected error),
  `_editingProjectId` and the editor contents are left untouched, so retrying the save targets the
  same original project.
- `_editingProjectId` is cleared only in `CancelEdit()` and on a successful save.
- A new `IsRegistryInteractionEnabled` property (`!IsEditing && !IsSaving && !IsLoading`) gates the
  project list, search box, and status filter in `MainWindow.xaml`, and the `New project`/`Refresh`
  command `CanExecute` predicates were extended with the same `!IsEditing` guard, so the registry
  cannot be mutated out from under an in-progress edit.
- `ProjectRegistryService` (Application layer) was not changed; it already takes an explicit
  `projectId` parameter and was never the source of the defect.

### New regression tests (`tests/AIUsageMonitor.Desktop.Tests/ProjectsWorkspaceTests.cs`)

- `EditTarget_DoesNotFollowSelectionChange`
- `EditTarget_DoesNotFollowFilterSelectionLoss`
- `FailedSave_RetryUsesOriginalEditTarget`
- `NewAndRefreshCommandsDisabledWhileEditing`
- `CancelClearsEditTargetAndRestoresRegistryInteraction`
- `SuccessfulSaveClearsEditTargetAndRestoresRegistryInteraction`

## SOL-35-02 — sanitized visual evidence (BLOCKED, NOT FABRICATED)

Evidence capture used Windows-native UI Automation
(`System.Windows.Automation.AutomationElement`/`InvokePattern`) to drive the real published
`win-x64` self-contained executable, and GDI (`System.Drawing.Graphics.CopyFromScreen`) for the
screenshot, per the no-new-packages/no-browser-automation constraint.

The capture found that the published shell starts in a fully degraded, no-persistence fallback mode
on **every** launch, for both the Projects workspace and the AI Capacity workspace — not a
Projects-specific or automation-specific issue. Root cause, isolated with a temporary diagnostic in
`App.xaml.cs` that was reverted before this commit: `AiCapacityViewModel` has two applicable public
constructors — `(IProviderRegistry, IProviderConnectionService)` and `(IExecutableLocator)`. The
.NET generic host's DI activator cannot disambiguate them while resolving `MainWindow`, throws
`InvalidOperationException: ... constructors are ambiguous` inside `App.OnStartup` → `ShowShell`,
and the existing outer `catch` silently falls back to a null-backed `new MainWindow()`.
`git log --oneline -- src/AIUsageMonitor.Desktop/ViewModels/AiCapacityViewModel.cs` shows exactly
one commit touching that file, `4b393b3` (APO-34), already merged to `main` before the APO-35
branch existed — this is a pre-existing regression unrelated to `ProjectsViewModel`/SOL-35-01, and
fixing it is outside the SOL-35-01/SOL-35-02 authorization.

Because automation could only reach the degraded shell, no functionally real screenshot of the
working Projects workspace or its editor exists to sanitize. Per the explicit no-fabrication
instruction, **no file was added to `docs/evidence/`** and none is claimed as APO-35 evidence.

**Result: `APO-35 VISUAL EVIDENCE COULD NOT BE CAPTURED SAFELY`.** Recommended follow-up: a small,
separately scoped Jira item under Epic APO-5 to remove the ambiguous `AiCapacityViewModel`
constructor, after which SOL-35-02 evidence capture should be re-attempted.

## Tests

Full solution: 214 executed, 214 passed, 0 failed, 0 skipped — Domain 28, Provider 46,
Infrastructure 86, Connection 10, Desktop 44 (up from 208 total / 38 Desktop).

## Build / publish / runtime smoke

- `dotnet build AIUsageMonitor.sln -c Release`: passed, 0 warnings, 0 errors.
- `dotnet test AIUsageMonitor.sln -c Release --no-build`: 214/214 passed.
- `win-x64`: self-contained single-file publish passed.
- `win-x86` / `win-arm64`: not rerun; no project/package/publish configuration changed.
- Published x64 executable launched and remained alive through a launch/stop smoke cycle. It runs
  in the pre-existing degraded no-persistence mode described above; this is not a SOL-35-01
  regression.
- `git diff --check`: only benign LF/CRLF notices. Diff secret-pattern scan: clean.

## Markdown files changed

- `.ai/CURRENT_STATE.md` — new `-3a` section recording the SOL-35-01/SOL-35-02 delta remediation.
- `TASK.md` — this delta acceptance handoff (replaces the prior APO-35 Sol acceptance handoff).
- `README.md` — updated Desktop/full-suite test counts (44 / 214).
- `docs/IMPLEMENTATION_PLAN.md` — new section 11 recording the delta remediation and the blocked
  evidence item.
- `AGENTS.md`, `CLAUDE.md`, `docs/BRD.md`, `docs/SESSION_PROMPTS.md`,
  `docs/LEGACY_IMPLEMENTATION_MAP.md`, and `docs/APO-31_PROVIDER_EVIDENCE.md` reviewed and
  intentionally unchanged.

## Jira completion comment

Jira remediation comment posted: APO-35 comment `11798`. APO-35 and APO-5 remain In Progress. No
Jira status transition was made.

## Out-of-scope confirmation

No Git/GitHub integration, Jira/Azure DevOps integration, Agent UI, routing, execution runtime,
validation engine, independent review engine, acceptance engine, Activity workspace, orchestration
controls, database, cloud backend, storage redesign, LocalAppData migration, provider changes,
technical namespace rename, or `AiCapacityViewModel` fix was performed. No deferred APO-27 P3
finding was addressed. No Application-layer change was made.

Opus cadence: Prompt 4/5. No Opus review performed, as explicitly required by the execution
contract (this remediation was executed and reported by Claude Sonnet 5 only).

Next gate: GPT-5.6 Sol acceptance of the exact final pushed feature-branch SHA, including
disposition of the blocked SOL-35-02 evidence and the newly discovered `AiCapacityViewModel`
defect.
