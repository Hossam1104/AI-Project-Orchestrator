# APO-39 Prompt 3/5 — Progressive Project Onboarding and Canonical Context Resolution

## Work item

- Story: APO-39 — Progressive Project Onboarding and Canonical Context Resolution
- Parent Epic: APO-5 — Project Context, Workspace & Lifecycle
- Executor: GPT-5.6 Luna xHigh
- Planner / acceptance authority: GPT-5.6 Sol
- Jira status: In Progress
- Jira implementation-start comment: `12036`
- Exact `main` base: `7cb94104c25fdb552c010835158e3c7e3eb813fe`
- Branch: `feat/APO-39-progressive-project-onboarding`
- Functional implementation commit: `bcbf966`
- Prior delivery metadata commit: `fe1491d1a1fa3cdcfe30accd919d7dbf59289e6e`; the final state
  synchronization commit is the current branch head reported in the completion handoff.
- Draft PR: #10 — https://github.com/Hossam1104/AI-Project-Orchestrator/pull/10
- PR state: OPEN / DRAFT / UNMERGED; base `main`; merge was not performed

## Bounded scope delivered

- Added a four-step WPF New Project flow: Project, Repository, Tracker, and AI Roles.
- Reused `Project`, `ProjectEdit`, `IProjectRepository`, `ProjectRegistryService`, and the
  existing versioned JSON project store; no new registry, database, cloud backend, or persistence
  root was introduced.
- Reused `ILocalRepositoryInspector`, `LocalRepositoryInspection`, and
  `RepositoryStateSnapshot` for safe, read-only local Git evidence.
- Added reference-only tracker setup with explicit Skipped, ConfiguredUnverified, and
  NotConfigured truth; no tracker API, OAuth, PAT, browser, CLI, or credential access was added.
- Reused the APO-38 agent catalog, registry, project override, and credential-reference contracts.
  Global defaults are bootstrapped absent-only for Sol, Luna xHigh, Sonnet 5, Opus 5, Terra HIGH,
  and Gemini 3.7; access, authentication, entitlement, and connection truth remain Unknown.
- Added GUID-scoped project context references at
  `%LOCALAPPDATA%\AIUsageMonitor\projects\<guid>\context-reference.json` with contract version 1,
  repository/tracker/model-role/current-work/policy references, and a truthful next-safe-action.
- Added explicit context persistence read states and resolver states, including project isolation,
  embedded ProjectId validation, unsupported-version handling, and incomplete/unavailable states.
- Added project-specific agent disable restrictions without mutating global defaults or granting
  capabilities.
- Existing project editing remains the advanced edit surface; New Project uses progressive
  onboarding in production composition.

## Truth and safety boundaries

- Repository onboarding accepts only existing local inspection evidence; it never probes directly,
  infers remote connectivity, invents `main`/`master`, stores file contents/diffs/raw command output,
  or persists secrets.
- Repository skip is explicit. Detached or unborn Git state requires an explicit branch or skip.
- Tracker onboarding stores references only and labels configured references unverified.
- Context references are not orchestration JSONL and do not implement Smart Continue, routing,
  tracker integration, remote SCM/CI evidence, or runtime execution. APO-43 remains unstarted and
  not authorized by this handoff.
- Project creation followed by a later persistence failure is reported as partial; the service never
  reports Ready without a durable context reference.

## Validation evidence

- `dotnet restore AIUsageMonitor.sln`: SUCCESS; all projects up to date.
- `dotnet build AIUsageMonitor.sln --no-restore`: SUCCESS; 0 warnings, 0 errors.
- `dotnet test AIUsageMonitor.sln --no-restore --verbosity minimal`: 367/367 passed, 0 failed,
  0 skipped — Domain 28, Connection 30, Provider 46, Desktop 80, Infrastructure 183.
- `git diff --check`: SUCCESS.
- Changed-line secret scan: CLEAN.
- Base-to-head scope review: completed; no provider API, tracker runtime, orchestration runtime,
  routing, database, cloud, or unrelated Story implementation found.
- GitHub CI: no checks are configured/reported; no CI success is claimed.

## Runtime and delivery evidence

- Self-contained single-file `win-x64` publish: SUCCESS.
- Executable:
  `D:\AI Tools\Active Projects\AI-Project-Orchestrator\src\AIUsageMonitor.Desktop\bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\AIUsageMonitor.Desktop.exe`
- Final live process: PID `26360`; window title `AI Project Orchestrator`; `Responding=True`;
  normal/non-degraded shell with `CAPACITY READY`; exactly one APO process at handoff.
- The UI navigation probe could not be completed because the Computer Use target could not be
  foregrounded while an unrelated RMS POS modal owned the active desktop. No interaction with that
  unrelated app was attempted. The APO process was left running after the bounded probe.
- `LEFT RUNNING = YES`
- Branch was pushed and exactly one Draft PR (#10) was created against `main`; it remains unmerged.
- Keep Jira APO-39 In Progress. APO-38 remains Done; APO-43 and APO-44 remain To Do and
  unauthorized.

## Required next planner boundary

Prompt 3/5 APO-39 implementation complete. Draft PR remains OPEN / DRAFT / UNMERGED. Next action is GPT-5.6 Sol exact-head acceptance review. APO-40, APO-43, and APO-44 remain NOT AUTHORIZED.
