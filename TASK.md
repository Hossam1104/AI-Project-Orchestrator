# APO-35 SOL ACCEPTANCE HANDOFF

Story: APO-35 — Implement First Usable Projects Workspace and Project Registry Management

Epic: APO-5 — Project Registry & Workspace Management

Exact main base: `34569abee50bdb708770e134e9db7db18752a80d`

Feature branch: `feat/APO-35-projects-workspace`

Draft PR: [#6](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/6), OPEN / DRAFT / UNMERGED

Functional SHA: `6f3cd6d` (`feat(APO-35): implement first usable Projects workspace`)

Final branch SHA: to be verified after the documentation/handoff commit and recorded in the final report

## Architecture summary

The vertical slice reuses the accepted APO-27 `Project` model and `IProjectRepository` contract.
The dependency direction remains WPF/Desktop -> Application -> Domain, with Infrastructure owning
JSON persistence and file paths. Desktop does not parse `projects.json`, know LocalAppData paths, or
instantiate `JsonProjectRepository`.

## Application use-case/service design

`IProjectRegistryService` and `ProjectRegistryService` provide `GetProjectsAsync`,
`CreateProjectAsync`, and `UpdateProjectAsync`. Create generates a non-empty Guid and assigns one
clock value to `CreatedAt` and `UpdatedAt`. Update loads the existing project first, preserves its
Id and CreatedAt, assigns the current clock value to UpdatedAt, and carries forward hidden
`RepositoryMetadata` and `TrackerMetadata`. Governance references are trimmed and blank lines are
ignored. The existing `Project` constructor remains final validation authority.

## Time handling

The service uses the accepted Application `IClock` seam; Infrastructure registers `SystemClock`.
Tests use a fixed clock. Internal timestamps remain `DateTimeOffset`; UI display uses local Windows
time formatting.

## ProjectsViewModel

`ProjectsViewModel` owns asynchronous registry loading/saving, project and filtered collections,
selection, editor state, search text, lifecycle filter, loading/saving flags, validation, bounded
errors, and commands. It uses in-memory search over Name and LocalPath and never scans the file
system.

## Navigation integration

Main window now exposes `Projects`, `IsProjectsSelected`, and `ShowProjectsCommand`. Selecting
Overview, Projects, or AI Capacity resets the other selected flags. AI Capacity remains the default
startup workspace. Agents and Activity remain disabled/planned.

## Workspace behavior

- Header/actions: Projects title, Refresh, New project, search, and lifecycle status filter.
- Project list: cards show name, lifecycle text, local path, repository/tracker summaries, and local
  UpdatedAt. Unconfigured values say `Not configured`; no connectivity is inferred.
- Detail view: name, status, local path, repository metadata fields, tracker metadata fields,
  governance references, routing/safety references, and CreatedAt/UpdatedAt.
- New project: clean in-workspace editor with all approved visible registration fields.
- Edit project: loads visible fields from the selected project and preserves hidden metadata.
- Search: case-insensitive Name/LocalPath in-memory search.
- Status filter: All, Active, Paused, Blocked, Archived; archived projects are discoverable by
  default through All and explicitly through Archived.

## Lifecycle

Active, Paused, Blocked, and Archived are editable status values. Archive and restore are ordinary
status edits. There is no Delete, Remove permanently, or Purge command.

## Validation

- Blank Name: `Project name is required.`
- Blank LocalPath: `Local path is required.`
- Repository metadata without DefaultBranch: `Default branch is required when repository
  information is configured.`
- Application `Project` construction remains the final invariant authority.

## Persistence and degraded behavior

Loading is asynchronous and duplicate refresh/save invocation is guarded. Empty registry and
no-match states are distinct. Read failures show `Projects could not be loaded.` with Retry. Write
failures show `Project could not be saved.`, retain the editor and entered values, and do not mutate
the visible collection before persistence succeeds. Raw exceptions, stack traces, JSON, internal
storage paths, tokens, and credentials are not rendered. If LocalAppData startup is unavailable,
Projects shows `Project storage unavailable.` and does not create an alternate in-memory registry.

## Project isolation and privacy

Selection and editing use one project record at a time. No project content, Git state, tracker
payload, credentials, prompts, conversations, or source code is read or persisted. Repository and
tracker fields are metadata-only registration values; no GitHub/Jira/Azure DevOps calls occur.

## Tests

Focused Desktop suite: 38 executed, 38 passed, 0 failed, 0 skipped. Coverage includes Application
service create/edit semantics, identity/time behavior, default-branch invariant, hidden metadata
preservation, loading/empty/no-match states, search, status filters, selection isolation, clean new
editor, create/edit success, archive/restore, validation, read/write failures, editor preservation,
degraded storage, duplicate-save prevention, and MainWindow navigation.

Full solution: 208 executed, 208 passed, 0 failed, 0 skipped — Domain 28, Provider 46,
Infrastructure 86, Connection 10, Desktop 38.

## Build / publish / runtime smoke

- `dotnet restore AIUsageMonitor.sln`: passed.
- `dotnet build AIUsageMonitor.sln --no-restore`: passed, 0 warnings, 0 errors.
- `win-x64`: existing self-contained single-file publish passed.
- `win-x86` / `win-arm64`: not rerun because project/package/publish configuration was unchanged;
  prior accepted compile/publish evidence remains historical and no hardware runtime claim is made.
- Published x64 executable launched and remained alive for five seconds with window title `AI Project
  Orchestrator`, then was stopped by the smoke harness.
- Bundled Computer Use native helper was unavailable, so no APO-35 visual screenshot was captured.

## Markdown files changed

- `.ai/CURRENT_STATE.md` — current APO-35 implementation and acceptance state.
- `TASK.md` — this Sol acceptance handoff.
- `README.md` — factual feature-state and capability wording.
- `docs/IMPLEMENTATION_PLAN.md` — APO-35 delivery boundary and Sol acceptance pending.
- `AGENTS.md`, `CLAUDE.md`, `docs/BRD.md`, `docs/SESSION_PROMPTS.md`,
  `docs/LEGACY_IMPLEMENTATION_MAP.md`, and `docs/APO-31_PROVIDER_EVIDENCE.md` reviewed and
  intentionally unchanged.

## Jira completion comment

Jira completion comment pending Sol synchronization. APO-35 and APO-5 remain In Progress. No Jira
status transition was made.

## Out-of-scope confirmation

No Git/GitHub integration, Jira/Azure DevOps integration, Agent UI, routing, execution runtime,
validation engine, independent review engine, acceptance engine, Activity workspace, orchestration
controls, database, cloud backend, storage redesign, LocalAppData migration, provider changes, or
technical namespace rename was performed.

Opus cadence: Prompt 3/5. No Opus review performed, as explicitly required by the execution
contract.

Next gate: GPT-5.6 Sol acceptance of the exact final pushed feature-branch SHA.
