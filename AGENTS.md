# AGENTS.md - AI Usage Monitor AI Execution Contract

This is the universal execution contract for every AI model working in this repository.

**Repository:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**Local Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**Primary Requirements:** `docs/BRD v1.0.md`  
**Plan:** `docs/IMPLEMENTATION_PLAN.md`  
**Prompts:** `docs/SESSION_PROMPTS.md`  
**Live Handoff:** `.ai/CURRENT_STATE.md`

---

# 1. Roles

## Planner

GPT-5.6 Sol owns:

- BRD planning
- architecture planning
- implementation sequence
- session decomposition
- approved scope changes

## Default Executor

Luna Max is the default implementation executor from the Portable Consumer Architecture Rebaseline onward.

Terra and Sonnet are fallback executors only when explicitly assigned for a particular session. No executor may choose a different session or continue automatically.

Executor rule:

> One assigned session at a time.

Executors implement, validate, review their diff, update current state, deliver through Git, and stop.

## Reviewer

Opus 5 performs independent review gates.

Reviewer mode does not add new product scope and does not implement fixes unless explicitly asked.

---

# 2. Mandatory Read Order

Before modifying repository files:

1. Read `AGENTS.md` completely.
2. Read `docs/BRD v1.0.md`.
3. Read `.ai/CURRENT_STATE.md`.
4. Read `docs/IMPLEMENTATION_PLAN.md`.
5. Read the active root `TASK.md` completely.
6. Read the exact relevant section of `docs/SESSION_PROMPTS.md` when the task references it.
7. Inspect `git status`, then inspect only task-relevant source/config files.

Do not depend on previous chat context.

The repository is the source of truth.

---

# Active TASK.md Contract

From this remediation onward, the root `TASK.md` is the active executable task, not a historical
execution log. It may add planner-approved scope and completion requirements, but it cannot
override the BRD or this contract.

1. Root `TASK.md` contains the current executable session/task.
2. Every executor must read it during mandatory startup.
3. Execute only the task defined in `TASK.md`.
4. `TASK.md` cannot override higher-authority BRD/governance decisions.
5. `.ai/CURRENT_STATE.md` remains the authoritative factual project history/status.
6. `docs/SESSION_PROMPTS.md` remains the permanent detailed session specification library.
7. After a session completes successfully and is delivered to `main`, Luna determines the next
   approved session from the implementation plan and current state.
8. Luna opens `docs/SESSION_PROMPTS.md` and copies the full approved prompt for that next session,
   including the applicable `COMMON ACTIVE SESSION INHERITANCE` content.
9. Luna replaces the entire root `TASK.md` with that complete executable prompt. It must contain
   the actual instructions Luna will execute later, not merely a title or a reference to the
   permanent prompt library.
10. The next `TASK.md` must be self-contained enough for a fresh Luna Max chat, and its preparation
    must be committed, pushed, and verified through the Git Delivery Contract.
11. Updating `TASK.md` does not authorize executing it.
12. Luna must stop after preparing and pushing the next `TASK.md`; Luna must not execute the newly
    populated task automatically.
13. Every new task requires a new explicit user instruction such as:
    `Execute the current root TASK.md through completion.`
14. If the current task is PARTIAL or BLOCKED, do not advance `TASK.md` to the next normal
    session. Keep or create an appropriate remediation/recovery task instead.
15. Do not use `TASK.md` as a historical execution log; historical execution results belong in
    `.ai/CURRENT_STATE.md`.

## Permanent TASK.md lifecycle

The permanent approved prompt library drives the next executable task:

```text
docs/SESSION_PROMPTS.md
        |
        | contains permanent approved session prompts
        v
Luna completes current session
        |
        v
Luna identifies next approved session
        |
        v
Luna COPIES the full next-session prompt from docs/SESSION_PROMPTS.md
        |
        v
root TASK.md is replaced with it
        |
        v
commit / push / verify
        |
        v
STOP
```

`TASK.md` must always be ready for direct execution in the next fresh context. The project owner
must not be required to manually populate it, and populating it never authorizes the next session.

---

# 3. Authority Order

When instructions conflict:

1. `docs/BRD v1.0.md`
2. `AGENTS.md`
3. approved facts/decisions in `.ai/CURRENT_STATE.md`
4. `docs/IMPLEMENTATION_PLAN.md`
5. assigned session prompt
6. existing implementation
7. executor preference

Do not silently override a higher authority.

---

# 3A. Cross-Windows Compatibility Contract

This section is permanent and mandatory. Every executor and reviewer session from Session 02 onward inherits it automatically.

## Baseline

- Minimum OS baseline: Windows 10 version 1809 / build 17763
- Supported OS goal: Windows 10 1809+ and Windows 11
- Supported architectures: x86, x64, ARM64
- Primary validation architecture: x64
- Secondary validation architectures: x86, then ARM64

Design principle:

> Modern where available. Compatible everywhere supported.

The exact Windows version support of the selected .NET 10/WPF release must be verified against current official Microsoft documentation before release claims are made. No executor may silently raise the minimum supported OS.

## Mandatory Rules

1. **API guarding** - No Windows 11-only API or post-build-17763 OS feature may be used for core functionality without runtime capability/version detection. An unavailable modern feature must never crash the app.
2. **Core independence** - Domain, Application, provider engine, quota calculation, subscription handling, history, analytics, recommendation, monitoring, persistence, and alert evaluation must not depend on Windows 11-only functionality.
3. **UI graceful degradation** - Optional modern backdrops, window effects, or animations must fall back to a compatible WPF surface with identical functionality. Visual effects never gate functionality.
4. **No modern hardware prerequisites** - AVX/AVX2, a dedicated/recent GPU, TPM, NPU, AI accelerator, or recent-generation CPU must never be mandatory.
5. **Lightweight background operation** - Use low idle CPU, reasonable memory, asynchronous I/O, and restrained timers. Do not noticeably degrade the IDE or other applications.
6. **Functional parity** - provider monitoring, local file persistence, quota display/calculation, subscription information, history, alerts, notifications, tray, Focus HUD, analytics, and recommendations must work without modern GPU functionality.

## Dependency Rule

Before adopting a package, framework dependency, Windows API, or native component beginning with Session 02, check it against the build-17763 minimum and the self-contained consumer deployment goal. If it would raise the minimum supported Windows version or add an avoidable end-user prerequisite, identify the incompatibility, look for a compatible alternative, record the issue in `.ai/CURRENT_STATE.md`, and escalate to the planner. The executor does not have authority to silently raise the minimum.

Reviewers must treat an unguarded modern dependency, hard hardware-acceleration requirement, or unsupported-OS crash as a classified finding under Section 23.

---

# 4. Frozen V1 Stack

The approved active V1 stack is:

- C#
- .NET 10
- WPF
- MVVM
- Modular Clean Architecture
- `System.Text.Json`
- JSON for small state/configuration documents
- JSON Lines (JSONL) for append-oriented history and event streams
- `HttpClient`
- `Microsoft.Extensions.Http.Resilience` where materially justified
- `Microsoft.Extensions.DependencyInjection`
- Serilog
- Windows Credential Manager or another planner-approved Windows secure-storage mechanism
- Git and GitHub
- GitHub Actions
- focused xUnit tests
- self-contained Windows release artifacts

V1 has no database engine and no ORM:

- no EF Core
- no SQL Server
- no SQL Server LocalDB
- no SQLite unless the planner explicitly changes V1 architecture

Do not introduce Angular, Electron, Tauri, Node.js, embedded Chromium, a cloud backend, or Jira unless the planner explicitly changes V1 architecture.

The previous WinUI/Windows App SDK/EF/LocalDB stack is historical implementation context only and is superseded by the Portable Consumer Architecture decision recorded in `.ai/CURRENT_STATE.md`.

---

# 5. Current State Contract

`.ai/CURRENT_STATE.md` is the single mutable handoff/status source.

Every executor session must update it before stopping. It must distinguish the approved target architecture from the architecture currently present in source code.

Do not keep duplicate volatile status inside:

- `AGENTS.md`
- `CLAUDE.md`
- the BRD
- `docs/IMPLEMENTATION_PLAN.md`
- random handoff files

Historical facts, decisions, and validation evidence may remain in the handoff when clearly labeled.

---

# 6. One Session Only

Execute only the session explicitly assigned.

Do not:

- combine sessions
- continue to the next session automatically
- implement future provider work early
- re-plan the project unless a blocker requires planner involvement

At session end:

1. build or perform the validation appropriate to the assigned scope
2. run relevant tests/checks
3. review the Git diff
4. check for secrets and debug artifacts
5. update `CURRENT_STATE`
6. commit, push, merge, and verify per Section 6A
7. report status
8. stop

---

# 6A. Git Delivery Contract

This section is permanent and mandatory for every completed executor implementation/remediation session from Session 02 onward. Reviewer-only Opus gates are exempt unless explicitly asked to modify repository files.

Requirement:

> Every completed executor session must commit, push its branch, merge into `main`, push `main`, verify `origin/main`, and leave the working tree clean.

Workflow:

```text
main
  -> session feature/fix branch
  -> implement
  -> validate
  -> update .ai/CURRENT_STATE.md
  -> review diff
  -> commit
  -> push branch
  -> merge into main
  -> push main
  -> verify origin/main
  -> clean working tree
```

Mandatory rules:

1. Use an appropriately named branch such as `feature/session-XX-description`, `refactor/session-03r-description`, or `fix/...`, unless a documented technical reason prevents it.
2. Commit every completed implementation/remediation session.
3. Push the session branch.
4. Integrate the session into `main`.
5. Push `main` to `origin`.
6. Fetch and verify `origin/main` after the push.
7. Leave the final working tree clean.
8. Never leave validated work only uncommitted, only local, or only on an unintegrated feature branch.
9. Never ask the project owner whether completed work should be committed or pushed.
10. Do not automatically proceed into the next implementation session after merging.
11. Never use `git push --force`, `git reset --hard`, or `git clean -fd` unless explicitly instructed for that specific action.
12. Preserve unrelated user changes.
13. If branch protection prevents direct merging, use the repository-supported PR/merge workflow and document the restriction in `CURRENT_STATE`.
14. A session is not `COMPLETE` until remote `main` contains the validated work, unless a documented external GitHub restriction makes integration impossible.
15. A single post-merge metadata-only commit directly to `main` is permitted solely to synchronize `.ai/CURRENT_STATE.md` and `TASK.md` with the already completed merge/push/verification. It must touch no source, product/configuration, or test files, must not bypass branch protection, and must not require a recursive session.

---

# 7. Provider Truthfulness

Providers:

- Codex
- Claude
- Kimi
- GitHub Copilot
- Antigravity

Their behavior may change.

Mandatory rules:

- verify current behavior
- prefer official APIs, OAuth/device auth, account usage surfaces, and documented endpoints
- use an official CLI only when it is available and useful; a CLI is never a whole-application prerequisite
- use local metadata only when verified safe
- do not guess endpoints or file schemas
- do not assume plan, billing, reset, or subscription fields are available
- do not fabricate 5-hour, weekly, monthly, or credit values
- do not label inferred data as official

If unavailable, use one of:

- Not Available
- Manual
- Partial
- Authentication Required
- Stale
- Unsupported

A truthful partial provider is acceptable.

Provider collection priority is:

1. official provider API
2. official OAuth/device/account connection
3. official authenticated account or usage endpoint
4. official CLI, if present and useful
5. safe verified local application metadata
6. manual configuration/input fallback

---

# 8. Used vs Remaining Semantics

The UI's primary convention is remaining capacity.

For each provider:

- prove source semantics
- normalize once
- keep parsing/normalization separate
- test source-to-domain transformation
- prevent double inversion

Never interpret `used_percent = 80` as `80% available`.

---

# 9. Time and Reset Rules

Use `DateTimeOffset` for provider timestamps.

Preserve the source offset where available. Normalize safely for calculations. Display reset time in the user's current Windows timezone.

Test midnight, week boundaries, daylight/timezone conversion where relevant, reset detection, and source timestamps without explicit offsets. Never guess a reset timezone.

---

# 10. Architecture Rules

## Domain

Domain must not depend on WPF, JSON file implementation details, EF Core, SQL, HTTP, provider libraries, filesystem APIs, or Windows UI APIs.

## Application

Application owns provider-independent contracts and use cases. It may define persistence abstractions, but must not know JSON filenames, LocalAppData paths, serializer configuration, or provider payload formats.

## Desktop

Desktop/WPF must not parse provider payloads, parse CLI output, inspect provider files directly, manage tokens directly, or contain storage-format rules. It consumes application services and view models through the composition root.

## Providers

Providers own detection, collection, parsing, normalization, provider-specific errors, and capability truth. They return normalized domain/application results.

## Infrastructure

Infrastructure owns JSON/JSONL persistence, safe file operations, storage-path resolution, secure credential adapters, Windows notifications, logging infrastructure, and appropriate OS integrations. Provider failure must be isolated.

The intended direction remains:

```text
Desktop/WPF -> Application -> Domain
Infrastructure -> Application/Domain contracts
Providers -> Application/Domain contracts
```

---

# 11. Dynamic Quota Rule

Never model the system around fixed quota columns.

Support arbitrary rolling windows, session limits, daily/weekly/monthly limits, billing cycles, AI credits, token allowances, model-specific quotas, and custom provider quotas. The UI adapts to the data the provider actually exposes.

---

# 12. Local File Persistence Contract

V1 is single-user local software with no database engine.

1. Store mutable application data below `Environment.SpecialFolder.LocalApplicationData`, approximately `%LOCALAPPDATA%\AIUsageMonitor\`.
2. Use JSON for settings, provider configuration/state, subscriptions, alert rules, current state, and other small documents.
3. Use monthly-partitioned JSONL for usage snapshots, sync history, and alert events unless a simpler approved partition is justified.
4. Every long-lived JSON document contains an explicit schema version such as `schemaVersion: 1`. JSONL records contain record/schema metadata sufficient for future evolution.
5. Critical JSON writes serialize to a temporary file, flush where practical, and replace the destination atomically where supported. Never rely on unsafe direct overwrite for critical state.
6. Concurrent writes are serialized with the smallest appropriate per-store or append synchronization. Do not introduce distributed locks.
7. History services stream and query only relevant JSONL files, preserve chronological ordering, and do not load all lifetime history at startup.
8. Preserve the existing material-change rule for usage snapshots; do not append unchanged snapshots.
9. Distinguish missing, empty, valid, unsupported-schema, corrupt, I/O-failure, and permission-failure states. Log/report problems, isolate or quarantine safely, and do not silently destroy user data.
10. Do not write mutable runtime data beside the executable or under Program Files. Do not require administrator privileges.
11. Do not write actual secrets to JSON, JSONL, settings, provider state, history, alerts, or logs.
12. Do not introduce a database or ORM unless the planner explicitly changes V1 architecture after evidence shows file storage is insufficient.

---

# 13. Security Rules

Never:

- store passwords or raw provider tokens
- extract browser cookies or unrelated credentials
- log tokens or sensitive environment variables
- commit credentials or raw authenticated payloads
- store provider secrets in JSON/JSONL or LocalDB
- place secrets in appsettings or other repository files
- collect prompts, conversations, source code, or task content

Use:

1. official OAuth/device/account flows
2. Windows Credential Manager or another planner-approved secure store
3. DPAPI only when technically justified
4. opaque credential references in JSON when a reference is needed
5. redacted diagnostics and sanitized fixtures

---

# 14. Privacy Rules

The monitor needs usage and subscription metadata only.

Do not collect or store prompts, conversations, provider chat history, source code, repository content, task content, browser passwords, browser cookies, unrelated tokens, app telemetry, or cloud-synced user data.

No cloud sync, app telemetry, or user tracking is enabled by default.

---

# 15. UI Rules

Visual objective:

> Modern AI command center / AI capacity cockpit.

The WPF baseline must be modern without depending on WinUI-specific effects. It is dark-first, supports light/system themes, uses strong typography, restrained gradients, rounded cards, accessible status text/icons, provider accents, clear remaining capacity, reset countdowns, polished loading/error/empty states, DPI awareness, and responsive resizing.

The product is for developers and non-developers. Do not make CLI, shell, SDK, token, or coding terminology the default unless provider setup genuinely requires it. Avoid neon overload, huge marketing areas, excessive animation, color-only meaning, and clutter. The Focus HUD must be useful beside an IDE while remaining lightweight.

---

# 16. Testing Rules

Target tests at high-risk logic rather than creating thousands of low-value tests:

- quota normalization and used/remaining conversion
- reset and timezone math
- provider parsers and source semantics
- JSON serialization/deserialization round trips
- schema compatibility/version handling
- JSONL append/read and time-range queries
- duplicate suppression
- atomic/safe writes
- missing/corrupt-file handling
- settings/subscription/provider-state persistence
- secret redaction and credential-reference safety
- burn-rate and recommendation scoring
- critical WPF view-model behavior where useful
- self-contained publish smoke checks

No live authenticated provider calls in GitHub CI. Use sanitized fixtures. Manual WPF/provider validation must be recorded honestly.

---

# 17. Validation Rules

Before stopping:

- build the full solution for implementation sessions
- run relevant targeted tests
- perform required manual/provider/publish validation
- review compile warnings caused by the session
- review `git diff`
- inspect changed files for secrets and debug artifacts
- update `.ai/CURRENT_STATE.md`

For documentation-only governance work, do not claim a build or test for an architecture that has not been implemented. Validate document consistency, paths, scope, and stale active requirements instead.

Never claim a command or test was run if it was not run. If the environment blocks validation, document exactly what was blocked and why.

---

# 18. Git Safety

Before editing, inspect `git status`.

Never destroy unrelated user changes, hard reset, blindly delete untracked files, or rewrite unrelated history. Keep session changes isolated. The repository remote is `https://github.com/Hossam1104/AI-Usage-Monitor-Tool.git` and the default branch is `main`.

---

# 19. Dependency Discipline

Before adding a package:

- prove it is needed
- prefer .NET/Microsoft/Windows capabilities compatible with the minimum OS
- avoid duplicate packages
- prefer maintained packages
- consider self-contained publishing and end-user prerequisites
- record significant dependency decisions in `CURRENT_STATE`

Do not replace approved technologies because another framework is personally preferred.

---

# 20. Error Handling

When refresh or storage operations fail:

- retain the last-known valid value
- mark stale/error
- show the last successful update
- expose concise diagnostic detail
- do not replace values with zero
- do not crash other providers, history, settings, or startup

The application must open with no connected providers and must isolate an unavailable or corrupt optional local file where safe recovery is possible.

---

# 21. Scope Control

Do not add:

- AI chat
- prompt runner
- model auto-switching
- AI-based recommendation calls
- cloud sync
- mobile app
- Angular dashboard
- Electron/embedded browser runtime
- organization/team billing
- payment workflows
- unrelated productivity features

Do not add new providers during the Portable Consumer Architecture Rebaseline. Retain the approved five-provider sequence.

---

# 22. Executor Completion Report

End every executor session with:

```text
Session:
Status: COMPLETE / PARTIAL / BLOCKED

Implemented:
- ...

Validated:
- ...

Not validated:
- ...

Blockers / limitations:
- ...

Files/areas changed:
- ...

CURRENT_STATE updated: Yes

Next planned session:
- ...
```

---

# 23. Reviewer Rules

Opus 5 review severity:

- BLOCKER - unsafe, fundamentally wrong, or prevents release/progression
- HIGH - major correctness/security/reliability issue
- MEDIUM - important but safely deferrable
- LOW - minor maintainability/polish

Reviewers must inspect actual code/evidence, not trust executor summaries. A polished UI does not compensate for unreliable quota data or a release that requires a hidden runtime/database/developer prerequisite.

Gate A, Gate B, Gate C, and Final Review must explicitly inspect the zero-prerequisite consumer contract, cross-Windows compatibility, provider truthfulness, file persistence integrity, credential security, and self-contained release evidence where applicable.

---

# 24. Core Product Standard

The application must be trustworthy enough that an individual can glance at it while working and decide which paid AI subscription or service has sufficient remaining capacity.

Accuracy and explicit uncertainty always outrank visual symmetry.

---

# 25. Zero-Prerequisite Consumer Deployment Contract

This is a permanent V1 release requirement.

1. The released application must not require a separately installed .NET runtime.
2. The released application must not require the .NET SDK, Visual Studio, or other developer tooling.
3. The released application must not require SQL Server, LocalDB, SQLite installation, or any other database engine.
4. The released application must not require Node.js, npm, Angular, Electron, or an embedded browser runtime.
5. No provider CLI may be mandatory for the whole application. A user who uses an AI service in a browser must have a truthful connection or manual fallback path where supported.
6. The application must open with no connected providers, empty history, missing optional configuration, or an isolated provider failure.
7. The application must open with missing/corrupt optional local data whenever safe recovery or quarantine is possible; user data must not be silently destroyed.
8. Mutable data must use per-user application-data locations and never require administrator privileges.
9. Actual credentials must remain outside JSON/JSONL and behind the secure credential abstraction.
10. Release artifacts must be self-contained. `PublishSingleFile=true` and `SelfContained=true` must be evaluated for WPF; trimming must not be enabled blindly.
11. Release consideration must cover `win-x64`, `win-x86`, and `win-arm64`, with claims limited to artifacts and environments actually validated.

---

# 26. Portable Consumer Architecture Rebaseline

Approved planner decision: 08 August 2026.

The approved target is a local-first Windows desktop application for general individual AI users, not a developer-only utility:

```text
WPF + MVVM
       |
Application contracts/services
       |
Domain

Infrastructure -> JSON/JSONL, secure credentials, OS integration
Providers      -> verified collection and normalization

Release -> self-contained .NET 10 Windows artifacts
Database/ORM -> none in V1
```

Session 03 completed the originally approved EF Core + SQL Server LocalDB persistence design successfully. That implementation is a completed historical implementation, not a failure. The later product clarification changed the V1 deployment requirement to zero external prerequisites and general-consumer usability. The LocalDB/EF implementation is therefore superseded; Session 03R removed it from the active runtime before provider development begins.

---
