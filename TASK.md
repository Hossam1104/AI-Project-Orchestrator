# APO-35 + APO-36 PROMPT 4 FINAL SOL DELTA ACCEPTANCE HANDOFF

Story: APO-35 — Implement First Usable Projects Workspace and Project Registry Management

Bug: APO-36 — Fix AiCapacityViewModel DI constructor ambiguity causing degraded startup

Epic: APO-5 — Project Registry & Workspace Management / APO-4 — AI Usage, Subscription &
Capacity Monitoring

Exact main base: `34569abee50bdb708770e134e9db7db18752a80d`

Feature branch: `feat/APO-35-projects-workspace`

Draft PR: [#6](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/6), OPEN / DRAFT /
UNMERGED

Pre-APO-36 branch head (prior Prompt-4 SOL-35-01 remediation): `9c2c678`
(`docs(APO-35): record SOL-35-01 remediation and blocked SOL-35-02 evidence`)

APO-36 functional SHA: `4525b31` (`fix: resolve APO-36 AI capacity DI startup ambiguity`)

Final branch SHA: recorded in the executor completion report after this documentation commit is
pushed

## SOL-35-01 — edit-target integrity (ACCEPTED, PRESERVED)

Already Sol-accepted in the prior Prompt-4 delta. `ProjectsViewModel` captures an immutable
`_editingProjectId` in `EditSelectedProject()`, uses only that captured id in `SaveAsync()`, fails
closed if it is unexpectedly absent, preserves it across a failed save so a retry still targets the
original project, clears it only on cancel/success, and gates the project list/search/status filter
with `IsRegistryInteractionEnabled` while an edit is open. This fix was not touched by the APO-36
remediation below; all six regression tests in `ProjectsWorkspaceTests.cs` remain green.

## APO-36 — AiCapacityViewModel DI startup ambiguity (FIXED)

**Root cause.** `AiCapacityViewModel` has two applicable public constructors:
`(IExecutableLocator)` (the degraded/manual fallback) and `(IProviderRegistry,
IProviderConnectionService)` (the normal provider-backed path). Production registration provided
all three dependencies and registered the view model with a bare `services.AddSingleton
<AiCapacityViewModel>()`. Microsoft.Extensions.DependencyInjection selects the constructor with the
most parameters that are all resolvable, then requires every other resolvable constructor's
parameter set to be a subset of that choice; `{IExecutableLocator}` is not a subset of
`{IProviderRegistry, IProviderConnectionService}`, so the container threw `InvalidOperationException:
... constructors are ambiguous` while resolving `MainWindow` inside `App.OnStartup`. The existing
outer `try/catch` silently swallowed that exception and fell back to `ShowShell(persistenceAvailable:
false)`, so the shell degraded to the no-persistence fallback on every normal launch — this is what
blocked SOL-35-02 evidence capture in the prior Prompt-4 delta.

**Fix.** `src/AIUsageMonitor.Desktop/DesktopServiceCollectionExtensions.cs` (new) adds
`AddDesktopWorkspaceServices(this IServiceCollection)`, which registers:

- `IProjectRegistryService` → `ProjectRegistryService` (moved here from `App.xaml.cs`, unchanged
  registration)
- `AiCapacityViewModel` via an explicit factory that resolves `IProviderRegistry` and
  `IProviderConnectionService` directly, bypassing constructor selection entirely
- `ProjectsViewModel`, `MainWindowViewModel`, `MainWindow` (moved here, unchanged registrations)

`App.OnStartup` now composes the container as `services.AddInfrastructure(...);
services.AddProviders(); services.AddDesktopWorkspaceServices();` — the same three calls the new
regression tests exercise. The degraded/manual `AiCapacityViewModel(IExecutableLocator)`
constructor is untouched and still used by `InitializeDegradedAsync`/`ShowShell(persistenceAvailable:
false)` and by existing direct-construction tests; `App.OnStartup`'s outer `catch` is untouched and
still protects against genuine storage/composition failures.

### New regression tests (`tests/AIUsageMonitor.Desktop.Tests/ProductionCompositionTests.cs`)

Build the real production `ServiceCollection` (`AddInfrastructure` + `AddProviders` +
`AddDesktopWorkspaceServices`) against a temporary root directory and resolve through the actual
Microsoft DI container:

- `ProductionComposition_ResolvesAiCapacityViewModel`
- `ProductionComposition_AiCapacityIsNotDegraded`
- `ProductionComposition_ResolvesMainWindowViewModel`
- `MainWindowViewModel_UsesResolvedNormalAiCapacity`
- `ProjectsViewModel_IsResolvedWithRegistryService`

Existing degraded-construction tests in `CapacityViewModelTests.cs` (direct `new
AiCapacityViewModel()` / `new AiCapacityViewModel(locator)` calls, not routed through DI) were not
modified and remain green, confirming the fallback path is intact.

## SOL-35-02 — sanitized visual evidence

**Previous state:** BLOCKED. Prior Prompt-4 evidence capture used Windows-native UI Automation
(`System.Windows.Automation.AutomationElement`/`InvokePattern`) plus GDI
(`System.Drawing.Graphics.CopyFromScreen`) to drive the real published `win-x64` executable, but
every launch landed in the degraded no-persistence shell described above, so no real Projects
screenshot could be captured or sanitized.

**Current state:** COMPLETE. After the APO-36 fix, the same UI Automation approach confirmed a
normal, non-degraded launch: the Overview workspace reports persistence text `"Ready"` /
`"LocalAppData is available for the foundation."` (not `"Degraded mode"`), and the AI Capacity
workspace shows the five real provider-backed cards (with `Connect / edit` actions, which only
render when the connection service is present). Navigating to Projects showed a real, empty registry
(`"0 total"`, `"No projects registered yet."` — not `"Project storage unavailable."`), confirming
`ProjectsViewModel` is registry-service-backed. `New project` was then invoked; the editor rendered
with a blank `NAME`/`LOCAL PATH`/etc. and `Cancel`/`Save changes` visible.

Screen capture used `PrintWindow` with `PW_RENDERFULLCONTENT` (still `user32.dll`, no new package;
`CopyFromScreen` returned the OS lock screen because the console session had auto-locked from
idle, not the target window — `PrintWindow` renders the window's own content directly and is
unaffected by that). The registry was genuinely empty (no prior real project data existed on this
machine through this flow), so the captured screenshot contains no real project names, paths,
repository URLs, tracker ids, or credentials — it was committed unmodified, with no cropping needed.

Evidence path: `docs/evidence/APO-35-projects-workspace.png`.

Privacy verification: image inspected before commit; no real project/registry data, credentials,
prompts, source, or unrelated windows are visible. No registry data was fabricated or modified to
produce this evidence — the registry was already empty.

## Tests

Full solution: 219 executed, 219 passed, 0 failed, 0 skipped — Domain 28, Provider 46,
Infrastructure 86, Connection 10, Desktop 49 (up from 214 total / 44 Desktop; +5 new composition
tests).

## Build / publish / runtime verification

- `dotnet restore AIUsageMonitor.sln`: passed.
- `dotnet build AIUsageMonitor.sln --no-restore`: passed, 0 warnings, 0 errors.
- `dotnet test AIUsageMonitor.sln --no-restore`: 219/219 passed.
- `win-x64`: self-contained single-file publish passed
  (`dotnet publish src/AIUsageMonitor.Desktop/AIUsageMonitor.Desktop.csproj -c Release -r win-x64
  --self-contained true -p:PublishSingleFile=true`).
- `win-x86` / `win-arm64`: not rerun; no project/package/publish configuration changed.
- Published x64 executable launched and was driven through Overview → AI Capacity → Projects → New
  project via UI Automation; proved the normal, non-degraded shell (see SOL-35-02 above); process
  was stopped cleanly afterward.
- `git diff --check`: clean.
- Diff secret-pattern scan (key/secret/token/password/credential/api-key): clean.

## Markdown files changed

- `.ai/CURRENT_STATE.md` — new `-3b` section recording the APO-36 root cause/fix and completed
  SOL-35-02 evidence.
- `TASK.md` — this handoff (replaces the prior blocked-evidence handoff).
- `README.md` — updated Desktop/full-suite test counts (49 / 219).
- `docs/IMPLEMENTATION_PLAN.md` — new section recording the APO-36 remediation and completed
  evidence.
- `AGENTS.md`, `CLAUDE.md`, `docs/BRD.md`, `docs/SESSION_PROMPTS.md`,
  `docs/LEGACY_IMPLEMENTATION_MAP.md`, and `docs/APO-31_PROVIDER_EVIDENCE.md` reviewed and
  intentionally unchanged.

## Jira

- APO-36 comment: root cause, fix, functional SHA, final branch SHA, PR #6, composition tests,
  normal runtime proof, screenshot evidence, full tests, next gate.
- APO-35 comment: APO-36 fix complete, SOL-35-02 evidence completed, screenshot path, next gate Sol
  delta acceptance.
- No Jira status transition was made. APO-36, APO-35, APO-4, APO-5 remain In Progress.

## Out-of-scope confirmation

No new provider behavior, quota changes, provider refactoring, Git/GitHub product operations,
Jira/Azure integrations, agent UI, routing, execution, orchestration, validation engine, review
engine, acceptance engine, activity UI, database, cloud backend, delete/purge, or namespace
migration was performed. No deferred APO-27 finding was addressed. No Application-layer change was
made beyond the Desktop composition-root extraction described above.

Opus cadence: Prompt 4/5 complete. No Opus review performed, as explicitly required by the execution
contract.

Next gate: GPT-5.6 Sol final Prompt-4 delta acceptance. If accepted, the following checkpoint is
Prompt 5/5 Claude Opus independent review.
