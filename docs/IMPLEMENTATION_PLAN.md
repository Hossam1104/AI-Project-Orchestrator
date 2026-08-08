# AI Usage Monitor - Implementation Plan

**Version:** 1.1
**Date:** 08 August 2026
**Rebaseline:** Portable Consumer Architecture Rebaseline - 08 August 2026
**Local Project Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**GitHub Repository:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**Default Branch:** `main`  
**Primary Requirements:** `docs/BRD v1.0.md`  
**Planner:** GPT-5.6 Sol  
**Default Executor:** Luna Max
**Fallback Executors:** Terra / Sonnet only when explicitly assigned
**Reviewer:** Opus 5

---

## 1. Purpose and Authority

This file is the authoritative active implementation sequence for AI Usage Monitor. The product is a local-first Windows desktop utility for individual AI users, not a SaaS product, team platform, or developer-only tool.

Source-of-truth files, in authority order, are:

1. `docs/BRD v1.0.md` - product and business requirements.
2. `AGENTS.md` - universal AI execution contract.
3. `.ai/CURRENT_STATE.md` - single mutable factual handoff.
4. `docs/IMPLEMENTATION_PLAN.md` - sequence and gates.
5. `docs/SESSION_PROMPTS.md` - exact executor/reviewer instructions.
6. `CLAUDE.md` - Claude/Sonnet/Opus adapter behavior.

The architecture rebaseline is an approved planner decision. It does not rewrite Git history and does not claim that the source migration has already happened.

---

## 2. Historical Completed Sessions vs Active Remaining Plan

The following historical facts are preserved:

```text
01  Repository & Solution Foundation                  COMPLETE
02  Domain & Application Architecture                 COMPLETE
02R Domain Integrity Remediation                      COMPLETE
03  EF Core + SQL Server LocalDB Persistence          COMPLETE - SUPERSEDED
```

Session 03 was a valid implementation of the architecture approved at that time. It was built, tested, committed, pushed, and merged. A later product clarification changed V1 to a zero-prerequisite consumer release. The LocalDB/EF implementation is therefore historical and superseded, not a failed session.

The active plan begins with the architecture correction:

```text
03R Portable Consumer Desktop Architecture Migration NEXT
04  Provider Feasibility Investigation                NOT STARTED
Gate A                                                NOT STARTED
05  WPF Modern Design System                          NOT STARTED
06  Main Dashboard                                    NOT STARTED
07  Tray & Focus HUD                                  NOT STARTED
08  Codex                                             NOT STARTED
09  Claude                                            NOT STARTED
10  Kimi                                              NOT STARTED
11  GitHub Copilot                                    NOT STARTED
12  Antigravity                                       NOT STARTED
Gate B                                                NOT STARTED
13  Subscription Management                           NOT STARTED
14  JSONL History & Analytics                         NOT STARTED
15  Capacity Recommendation Engine                    NOT STARTED
16  Monitoring & Notifications                         NOT STARTED
17  Settings, Security & File Resilience              NOT STARTED
18  UX & Performance Polish                           NOT STARTED
Gate C                                                NOT STARTED
19  Self-Contained Packaging & Release               NOT STARTED
20  Final Stabilization                               NOT STARTED
Final Gate                                            NOT STARTED
```

Do not reuse the plain `Session 03` label for the migration. Do not start Session 04 until Session 03R is complete and the required review gate permits it.

---

## 3. Frozen V1 Target Architecture

| Area | Approved target |
|---|---|
| Language | C# |
| Runtime | .NET 10 |
| Desktop UI | WPF |
| UI pattern | MVVM |
| Architecture | Modular Clean Architecture |
| Serialization | `System.Text.Json` |
| Small local state | JSON |
| Append-oriented history/events | JSONL |
| Database | None |
| ORM | None |
| HTTP | `HttpClient` |
| Resilience | `Microsoft.Extensions.Http.Resilience` where materially justified |
| Dependency injection | `Microsoft.Extensions.DependencyInjection` |
| Logging | Serilog |
| Secrets | Windows Credential Manager or planner-approved Windows secure storage |
| Tests | Focused xUnit tests |
| Release | Self-contained Windows artifacts |
| Source control/CI | Git, GitHub, GitHub Actions |

The V1 architecture must not introduce WinUI 3, Windows App SDK, EF Core, SQL Server LocalDB, SQLite, Angular, Electron, Node.js, npm, or embedded Chromium. The historical source still contains the old stack until Session 03R removes it; that is documented state, not an active target requirement.

---

## 4. Zero-Prerequisite Consumer Deployment

The released application shall require no separately installed .NET runtime, SDK, database engine, Node.js runtime, or developer tooling.

The release strategy must use self-contained publishing. Evaluate:

```text
SelfContained=true
PublishSingleFile=true
```

for WPF, enabling single-file only when stable. Do not blindly enable trimming; WPF and reflection-heavy libraries must be treated cautiously. Reliability is more important than binary size.

Release artifacts are architecture-specific where necessary:

- `win-x64`
- `win-x86`
- `win-arm64`

The user experience is `download -> run`. The application must not require SQL/LocalDB, SQLite installation, Node/npm, Angular, Visual Studio, a .NET runtime installer, or a mandatory provider CLI. Development machines may use the .NET SDK; end-user machines must not need it.

---

## 5. Cross-Windows Compatibility

The permanent baseline from `AGENTS.md` Section 3A remains in force:

- minimum baseline: Windows 10 version 1809 / build 17763
- supported OS goal: Windows 10 1809+ and Windows 11
- architectures: x86, x64, ARM64
- x64 primary validation, x86 then ARM64 secondary
- optional modern effects require capability detection and a compatible fallback
- no Windows 11-only core dependency or modern hardware requirement
- verify exact .NET 10/WPF support against current official Microsoft documentation before release claims

Domain, Application, provider engine, persistence, analytics, recommendation, monitoring, and alert evaluation must remain independent of newer Windows-only features. WPF is the baseline UI and must remain functional with ordinary rendering.

---

## 6. Repository Responsibilities

The broad project layout remains:

```text
src/AIUsageMonitor.Desktop       WPF presentation/composition root
src/AIUsageMonitor.Domain        provider-independent domain rules
src/AIUsageMonitor.Application   use cases and provider-independent contracts
src/AIUsageMonitor.Infrastructure JSON/JSONL, secure storage, logging, OS integration
src/AIUsageMonitor.Providers     provider detection, collection, parsing, normalization
tests/*                          focused domain, provider, infrastructure/file tests
```

Dependency direction:

```text
Desktop/WPF -> Application -> Domain
Infrastructure -> Application/Domain contracts
Providers -> Application/Domain contracts
```

Domain must not depend on WPF, serializers, filesystem APIs, HTTP, provider libraries, EF, SQL, or Windows UI APIs. Desktop must not parse provider data or manage secrets. Infrastructure owns file persistence and secure-storage adapters. Providers collect; core normalizes; UI displays.

---

## 7. Local File Persistence Design

V1 is single-user local software. Use ordinary per-user files rooted at:

```text
Environment.SpecialFolder.LocalApplicationData
approximately %LOCALAPPDATA%\AIUsageMonitor\
```

The exact filenames may be refined in Session 03R, but the conceptual layout is:

```text
AIUsageMonitor/
  settings.json
  providers.json
  subscriptions.json
  current-state.json
  history/
    YYYY-MM.jsonl
  alerts/
    YYYY-MM.jsonl
  logs/
```

Use JSON for small configuration/state documents and JSONL for append-oriented usage snapshots, sync history, and alert events. Long-lived formats require explicit schema versions. JSONL records require record/schema metadata for future evolution.

Critical state writes must serialize, write a temporary file, flush where practical, and replace the destination atomically where supported. Per-store or append synchronization must protect concurrent refresh/manual/resume writes. Do not use distributed locks, write beside the executable, write under Program Files, or require administrator privileges.

History services must stream records, read only relevant monthly partitions, support time ranges, preserve ordering, and avoid loading years of history at startup. The existing material-change detector must prevent unchanged snapshots from being appended.

Storage must distinguish valid, missing, empty, unsupported-schema, corrupt, I/O-failure, and permission-failure cases. It must log/report problems, quarantine or back up safely where useful, and keep the application usable when one optional file is bad. It must never silently destroy user data.

Actual credentials are never stored in JSON, JSONL, settings, provider state, history, alerts, or logs. Files may contain only opaque credential references.

---

## 8. Provider Truth and Consumer UX

The initial provider sequence remains Codex, Claude, Kimi, GitHub Copilot, and Antigravity. Session 04 must investigate each provider before adapters are implemented.

Collection priority:

1. official provider API
2. official OAuth/device/account connection
3. official authenticated account/usage endpoint
4. official CLI where useful
5. verified safe local metadata
6. manual fallback

For each field, distinguish official, verified local, inferred, manual, unavailable, unsupported, stale, or authentication-required data. Never fabricate quotas, reset times, plan fields, billing periods, or subscription dates. A browser-only or non-developer user must have a safe connection/manual path whenever the provider allows it. A CLI is optional.

The normalized model remains dynamic: arbitrary quota windows, credits, rolling limits, model-specific allowances, used/remaining/limit values, `DateTimeOffset` timestamps, source, and confidence. The UI's primary convention is remaining capacity, with no double inversion.

---

## 9. Testing Strategy

Keep V1 tests targeted. Replace the historical database-focused emphasis with:

- JSON serialization/deserialization round trips
- schema-version compatibility and upgrade handling
- JSONL append/read behavior
- monthly partition selection and time-range queries
- chronological ordering and `DateTimeOffset` offset preservation
- material-change duplicate suppression
- atomic/safe writes
- missing, empty, corrupt, unsupported-schema, I/O, and permission behavior
- settings, provider-state, subscription, and alert persistence
- secure credential-reference/no-secret guarantees
- provider parsing and source semantics
- quota normalization, reset math, burn rate, and recommendation scoring
- critical WPF view-model behavior where useful
- self-contained publish smoke validation

No live authenticated provider calls in CI. Use sanitized fixtures. Do not carry meaningless EF migration, `DbContext`, or LocalDB installation tests into the active architecture.

---

## 10. Git Workflow

The default branch is `main`. Implementation/remediation sessions use a session branch and must follow the Git Delivery Contract in `AGENTS.md` Section 6A:

```text
inspect status -> implement -> validate -> update CURRENT_STATE
-> review diff -> commit -> push branch -> merge main
-> push main -> fetch/verify origin/main -> clean tree -> stop
```

Never force-push, hard-reset, or delete unrelated user changes. A documentation-only rebaseline may use `docs/portable-consumer-architecture-rebaseline`; Session 03R uses `refactor/session-03r-portable-consumer-architecture`.

---

# 11. Master Delivery Sequence

```text
Historical Phase 0
  01 Foundation                         COMPLETE
  02 Domain/Application                 COMPLETE
  02R Domain Integrity                   COMPLETE
  03 EF Core + SQL Server LocalDB        COMPLETE - SUPERSEDED

Active Phase 0 - Architecture Correction
  03R Portable Consumer Migration        NEXT
  04 Provider Feasibility                NOT STARTED
  Gate A                                 NOT STARTED

Active Phase 1 - Experience
  05 WPF Modern Design System             NOT STARTED
  06 Main Dashboard                       NOT STARTED
  07 Tray + Focus HUD                     NOT STARTED

Active Phase 2 - Providers
  08 Codex                                NOT STARTED
  09 Claude                               NOT STARTED
  10 Kimi                                 NOT STARTED
  11 GitHub Copilot                       NOT STARTED
  12 Antigravity                          NOT STARTED
  Gate B                                 NOT STARTED

Active Phase 3 - Product Intelligence
  13 Subscription Management              NOT STARTED
  14 JSONL History & Analytics             NOT STARTED
  15 Capacity Recommendation Engine       NOT STARTED
  16 Monitoring & Notifications            NOT STARTED

Active Phase 4 - Hardening and Release
  17 Settings, Security & File Resilience NOT STARTED
  18 UX & Performance Polish               NOT STARTED
  Gate C                                 NOT STARTED
  19 Self-Contained Packaging & Release   NOT STARTED
  20 Final Stabilization                  NOT STARTED
  Final Gate                              NOT STARTED
```

Sessions execute sequentially. Do not implement Sessions 08-12 before Session 04 evidence and Gate A approval. Do not execute Session 04 before Session 03R completes.

---

# 12. Session 03R - Portable Consumer Desktop Architecture Migration

**Status:** NEXT.
**Default executor:** Luna Max.
**Branch:** `refactor/session-03r-portable-consumer-architecture`.

Objective: convert the currently implemented WinUI/Windows App SDK/EF Core/SQL Server LocalDB source to the approved WPF + JSON/JSONL + self-contained consumer architecture without discarding the completed Domain/Application work or falsifying historical Session 03.

Required outcomes:

- convert `AIUsageMonitor.Desktop` to a minimal WPF shell targeting .NET 10; remove active WinUI/Windows App SDK configuration and dependencies
- preserve project name and dependency direction where practical
- preserve Domain/Application contracts and their dynamic quota/time/security invariants
- remove EF Core, SQL Server, LocalDB, `DbContext`, migrations, database initialization, SQL repositories, and obsolete SQL-specific tests from the active runtime
- implement Infrastructure file stores with `System.Text.Json` and JSONL as appropriate
- preserve provider-independent repository/service contracts where they remain sound
- implement LocalAppData path resolution and automatic directory initialization
- add schema versioning, safe atomic JSON writes, synchronized writes, corruption classification/recovery, and monthly streaming JSONL history
- replace the EF usage snapshot repository with JSONL behavior while preserving material-change detection, latest/range queries, ordering, source/confidence, and `DateTimeOffset`
- migrate infrastructure tests to focused JSON/JSONL/file-resilience tests
- make the WPF shell open with no providers, missing state, empty history, or isolated storage/provider failures
- establish and validate self-contained publishing for win-x64, win-x86, and win-arm64 where the environment permits; evaluate single-file without blind trimming
- update `CURRENT_STATE` with actual target/current distinctions and validation; stop before Session 04

Session 03R must not implement provider adapters, the dashboard, tray/HUD polish, or Session 04 investigation.

---

# 13. Session 04 - Provider Feasibility Investigation

Evidence-only session. For each provider investigate official usage API, account usage endpoint, OAuth/device/account auth, browser-based supported workflow, optional CLI, safe local metadata, manual fallback, plan, quota windows, reset timestamp/timezone, subscription/renewal information, source semantics, and authentication failure behavior.

Answer whether a non-developer/browser-only user can connect, whether CLI is required or optional, what is official/verified/inferred/manual, and what must remain unavailable. Do not implement adapters. Save a capability/evidence matrix in `CURRENT_STATE` and preserve privacy/security.

---

# 14. Gate A - Opus 5

Gate A reviews Sessions 01-04 and must inspect:

- WPF is the active desktop foundation
- Domain/Application separation and dynamic quota correctness
- absence of WinUI/Windows App SDK/EF/SQL/LocalDB runtime dependency
- JSON/JSONL stores, schema versions, atomic writes, synchronization, corruption handling, and `DateTimeOffset`
- duplicate-history suppression and streaming/range behavior
- LocalAppData and no administrator requirement
- self-contained publish strategy and x64/x86/ARM64 artifacts
- provider feasibility evidence and non-developer UX feasibility
- credential boundaries, no secrets in files, and provider truth
- Cross-Windows compatibility and graceful UI degradation

Opus must reject Gate A if the active architecture still requires LocalDB/SQL Server/EF or if zero-prerequisite deployment is unproven at the architecture level.

---

# 15. Session 05 - WPF Modern Design System

Build a modern, premium, dark-first WPF design system with light/system option, resource tokens, typography, spacing, cards, buttons, icons, status badges, progress components, provider accents, responsive layout, loading/error/empty states, keyboard accessibility, high-DPI behavior, and restrained animation. Do not depend on Mica/Acrylic or any unavailable modern effect. Do not implement providers.

---

# 16. Session 06 - Main Dashboard

Build the general-user dashboard around available/remaining capacity, reset countdowns, plans, credits, connection status, last-updated time, stale/partial/error states, best available capacity, and dynamic provider card/quota rows. A provider may expose zero, one, or many windows or only credits. Do not use developer jargon by default, parse provider payloads in WPF, or fabricate missing windows.

---

# 17. Session 07 - Tray and Focus HUD

Implement lightweight WPF tray behavior and an optional compact Focus HUD: open dashboard, show/hide HUD, refresh, pause/resume hook, settings, explicit exit, remembered position/size/mode, always-on-top, readable provider rows, stale/error indicators, and no heavy browser background process. Preserve simple startup and shutdown behavior.

---

# 18. Sessions 08-12 - Providers

Keep the sequence: 08 Codex, 09 Claude, 10 Kimi, 11 GitHub Copilot, 12 Antigravity. Each adapter must implement only the capability path verified in Session 04. CLI is optional, never assumed. Every adapter requires detector, collector, parser, normalizer, structured failures, stale/auth handling, safe persistence, sanitized fixtures, focused tests, and a truthful manual/partial/unsupported fallback where automatic usage is unavailable. No fabricated endpoints, quotas, reset times, subscription data, or credential access.

---

# 19. Session 13 - Subscription Management

Persist and display plan, original start, billing period, renewal, cancellation/paid-through semantics, auto-renew, price, currency, cadence, source, confidence, and last verified time through JSON stores. Missing values are normal. Support manual entry and clearly distinguish provider, verified-local, inferred, and manual data. No payment functionality.

---

# 20. Session 14 - JSONL History and Analytics

Use partitioned JSONL history with streaming/incremental reads, time ranges, chronological ordering, remaining-capacity trends, consumption delta, burn rate, reset detection, estimated exhaustion, estimated remaining-at-reset, and explicit stale gaps. Do not load all lifetime history unnecessarily or fabricate interpolation.

---

# 21. Session 15 - Capacity Recommendation Engine

Implement deterministic capacity scoring only. Use remaining capacity, relevant windows, credits, reset proximity, burn rate, provider availability, stale confidence, and exhaustion. Handle missing/credit-only data explicitly. Output a score, ranking, reason, and best available capacity recommendation. Do not rank intelligence or invoke an AI model.

---

# 22. Session 16 - Monitoring and Notifications

Implement lightweight background refresh (approximately 60 seconds by default), startup/resume/foreground/manual triggers, cancellation, timeout/backoff, provider isolation, no overlapping refresh, and notification deduplication. Persist alert events through JSONL/file stores without high-frequency disk writes. Support warning/critical remaining thresholds, exhaustion, reset/restored, authentication failure, stale provider, and renewal notifications.

---

# 23. Session 17 - Settings, Security and File Resilience

Implement settings for providers, refresh, startup, themes, HUD, thresholds, and manual subscriptions using JSON. Harden atomic writes, backups/quarantine where useful, schema upgrades, missing/corrupt/unsupported files, permissions/path failures, storage availability, credential manager integration, secure disconnect, secret redaction, and provider auth recovery. Remove database-resilience assumptions.

---

# 24. Session 18 - UX and Performance Polish

Perform no-new-scope stabilization. Validate startup time, idle CPU, memory, JSONL large-history behavior, UI responsiveness, WPF rendering, high DPI, x86/x64/ARM64 builds where feasible, older supported hardware, reduced-effect fallback, restrained animations, cancellation, and absence of unnecessary disk/provider work.

---

# 25. Gate B - Opus 5

Review all five provider implementations against Session 04 evidence. Audit invented endpoints, brittle scraping, cookie/token exposure, source semantics, used/remaining inversion, reset/timezone errors, fabricated subscription values, stale-state handling, duplicate snapshots, aggressive polling, provider isolation, and provider-specific logic leaking into WPF. Classify findings and approve or reject Phase 3.

---

# 26. Gate C - Opus 5

Perform a pre-release audit of BRD coverage, WPF/MVVM boundaries, JSON/JSONL integrity, security, provider correctness, UX/accessibility, performance, resilience, monitoring, alerts, analytics, recommendation logic, compatibility, and zero-prerequisite release evidence. Resolve BLOCKER/HIGH findings before Session 19.

---

# 27. Session 19 - Self-Contained Packaging and Release

Create repeatable release artifacts and CI for `win-x64`, `win-x86`, and `win-arm64` where feasible. Validate `SelfContained=true`, evaluate stable single-file publishing, avoid blind trimming, keep mutable data outside the install directory, and test a clean compatible Windows machine/VM with no separately installed .NET runtime, SDK, SQL, Node, or developer tooling. Do not add database installers, Angular, Node, WebView, or hidden runtime prerequisites.

---

# 28. Session 20 - Final Stabilization

No new features. Validate download -> launch -> use, first-run directory creation, missing/empty/corrupt state recovery, existing history, provider failure isolation, offline/stale behavior, credential security, dashboard/HUD/tray, analytics, alerts, recommendations, self-contained artifacts, and the Cross-Windows contract. Resolve BLOCKER/HIGH issues, update `CURRENT_STATE` to RELEASE CANDIDATE when justified, and stop before Final Review.

---

# 29. Final Opus 5 Release Review

For every BRD requirement classify PASS, PARTIAL, FAIL, or NOT APPLICABLE. Explicitly confirm:

- WPF is the active UI technology
- WinUI and Windows App SDK are absent from the active runtime
- EF Core, SQL Server, and LocalDB are absent from the active runtime
- SQLite was not introduced without approval
- JSON/JSONL persistence and schema versioning are active
- safe writes, corruption handling, streaming history, and duplicate suppression work
- secrets remain external to files
- self-contained release works without a .NET runtime installation or developer tooling
- provider CLI is optional and browser/non-developer use is represented truthfully
- provider truth, remaining-capacity semantics, timestamps, and reset behavior are correct
- terminology serves general AI users
- WPF UI remains modern, readable, accessible, and performant
- x64/x86/ARM64 artifacts and supported-OS claims are evidence-based

The final verdict must be exactly `READY FOR PERSONAL PRODUCTION USE` or `NOT READY FOR PERSONAL PRODUCTION USE`.

---

# 30. Definition of Done

Every executor session is done only when:

1. assigned scope is implemented and no future scope was added
2. appropriate build/validation and focused tests actually ran
3. no session-created warnings/errors remain unexplained
4. no secrets/debug artifacts are present
5. final Git diff was reviewed
6. `.ai/CURRENT_STATE.md` was updated factually
7. changes were committed
8. session branch was pushed
9. branch was merged into `main`
10. `main` was pushed
11. `origin/main` was fetched and verified
12. working tree is clean
13. limitations/blockers and next session are recorded
14. executor stops

V1 is complete only when Sessions 01-20, all Opus gates, truthful provider paths/fallbacks, dynamic capacity display, JSONL history, HUD, alerts, security, CI, self-contained release, and the final Opus verdict are complete.
