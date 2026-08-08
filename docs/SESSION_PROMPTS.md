# AI Usage Monitor - Direct Execution Session Prompts

**Version:** 1.1
**Rebaseline:** Portable Consumer Architecture Rebaseline - 08 August 2026
**Local Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**GitHub:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**BRD:** `docs/BRD v1.0.md`  
**Planner:** GPT-5.6 Sol
**Default executor:** Luna Max
**Fallback executors:** Terra / Sonnet only when explicitly assigned
**Reviewer:** Opus 5

Use each prompt in a separate executor/reviewer context. The repository, not prior chat context, is authoritative.

---

# COMMON ACTIVE SESSION INHERITANCE

Every active prompt below inherits these rules from `AGENTS.md`:

- execute only the named session or gate
- read `AGENTS.md`, `docs/BRD v1.0.md`, `.ai/CURRENT_STATE.md`, `docs/IMPLEMENTATION_PLAN.md`, and this assigned prompt before acting
- inspect `git status` before editing and preserve unrelated user changes
- active target architecture is WPF + .NET 10 + MVVM + Modular Clean Architecture
- active persistence is `System.Text.Json` with JSON for state/configuration and JSONL for append-oriented history/events
- V1 has no EF Core, SQL Server, LocalDB, ORM, SQLite, Angular, Electron, Node.js, npm, or embedded Chromium
- released artifacts must be self-contained; no separately installed .NET runtime, SDK, database, Node, or developer tooling may be required
- no provider CLI is mandatory for the whole application
- non-developer/browser-only users must be represented by safe connection or manual fallback paths
- preserve remaining-capacity semantics, dynamic quota windows, `DateTimeOffset`, provider truthfulness, privacy, and last-known-good behavior
- honor the Cross-Windows Compatibility Contract: Windows 10 1809/build 17763 baseline, Windows 10 1809+/Windows 11 goal, x86/x64/ARM64, x64 primary, graceful fallback for optional modern effects
- do not add new providers or unrelated product scope
- use focused tests and sanitized fixtures; no live authenticated provider calls in CI
- review the diff, inspect secrets/debug artifacts, update `CURRENT_STATE`, and stop before the next session

Completed implementation sessions must also follow the Git Delivery Contract: commit, push the session branch, merge into `main`, push `main`, fetch/verify `origin/main`, and leave the tree clean. Reviewer-only gates do not modify or deliver code unless explicitly instructed.

---

# HISTORICAL PROMPTS - COMPLETED AND NOT THE NEXT EXECUTION

The following records preserve the original work sequence. They are historical context and must not be executed again.

## SESSION 01 - Repository & Solution Foundation

**HISTORICAL - COMPLETE. DO NOT EXECUTE AGAIN.**

Session 01 created the repository, solution, project layout, build properties, editor configuration, README, and a minimal WinUI shell under the architecture approved at that time. Its actual validation is recorded in `.ai/CURRENT_STATE.md`.

## SESSION 02 - Domain & Application Architecture

**HISTORICAL - COMPLETE. DO NOT EXECUTE AGAIN.**

Session 02 created the provider-independent domain model and application contracts for dynamic quotas, subscriptions, usage snapshots, alerts, provider discovery/refresh, settings, and secure credentials. Those Domain/Application contracts are intentionally preserved through Session 03R unless a concrete migration issue requires a planner-recorded change.

## SESSION 02R - Domain Integrity Remediation

**HISTORICAL - COMPLETE. DO NOT EXECUTE AGAIN.**

Session 02R corrected last-known-data retention, used/remaining validation, subscription confidence, opaque credential references, and related targeted tests before persistence. Its decisions remain active unless Session 03R documents a necessary compatible adjustment.

## SESSION 03 - EF Core 10 + SQL Server LocalDB Persistence

```text
HISTORICAL - COMPLETED BUT ARCHITECTURALLY SUPERSEDED
DO NOT EXECUTE AGAIN
```

Session 03 successfully implemented the originally approved EF Core 10 + SQL Server LocalDB persistence design, including `DbContext`, explicit entity configuration, migrations, SQL repositories, LocalDB initialization, material-change duplicate suppression, and real LocalDB round-trip/integration tests. It was built, tested, committed, pushed, merged, and recorded in `CURRENT_STATE`.

The implementation was valid for the earlier architecture. A later product clarification changed V1 to a zero-prerequisite consumer application usable by general AI subscribers. The LocalDB/EF implementation is therefore superseded and will be removed/replaced by Session 03R. Do not rewrite this history as a failure and do not use the old prompt to execute Session 03 again.

---

# SESSION 03R - Portable Consumer Desktop Architecture Migration

```text
EXECUTE SESSION 03R ONLY.
DEFAULT EXECUTOR: Luna Max.
BRANCH: refactor/session-03r-portable-consumer-architecture
SUGGESTED COMMIT: refactor: migrate to portable WPF and file persistence
STOP BEFORE SESSION 04.
```

## Why this session exists

The current source still contains the historical WinUI 3 / Windows App SDK / EF Core / SQL Server LocalDB implementation from Session 03. The approved V1 product is now a simple local-first consumer desktop application usable by developers and non-developers without Visual Studio, a .NET runtime installer, SQL Server, LocalDB, SQLite installation, Node/npm, Angular, migrations, environment setup, or developer command-line tools.

The approved target is WPF + .NET 10 + MVVM, JSON/JSONL local persistence, secure Windows credential storage, and self-contained Windows release artifacts. This is an implementation migration, not a history rewrite. Preserve the completed Domain/Application work and retain the historical Session 03 evidence in `CURRENT_STATE`.

## Mandatory startup

1. Read `AGENTS.md` completely.
2. Read `docs/BRD v1.0.md` completely.
3. Read `.ai/CURRENT_STATE.md` completely.
4. Read `docs/IMPLEMENTATION_PLAN.md`.
5. Read this Session 03R prompt completely.
6. Inspect `git status` and confirm the branch is `refactor/session-03r-portable-consumer-architecture` or create it from a clean/up-to-date `main`.
7. Inspect the existing solution/project files, Domain/Application contracts, Infrastructure persistence code, tests, and desktop shell relevant to the migration.
8. Do not delete or overwrite unrelated user changes.

## Scope boundary

This session performs the architecture migration only. Do not implement provider adapters, provider feasibility investigation, the dashboard, polished design system, tray/HUD features, analytics, recommendations, notifications, or any Session 04+ work. Do not add providers. Stop after validating the migrated foundation and delivering this session.

## A. Convert the desktop foundation to WPF

Convert `src/AIUsageMonitor.Desktop` from the historical WinUI 3/Windows App SDK project to a smallest valid WPF .NET 10 shell.

Required outcomes:

- preserve the `AIUsageMonitor.Desktop` project name unless a compelling technical reason is recorded
- use the appropriate WPF SDK/project properties for .NET 10
- remove active `Microsoft.WindowsAppSDK`, `UseWinUI`, WinUI XAML, WinUI bootstrap, Windows App SDK-specific properties, and WinUI-only startup code/dependencies
- create a minimal WPF `App`/`Window` shell with a composition root and a clear startup path
- keep UI code out of Domain/Application/Providers
- do not require Mica, Acrylic, Windows App SDK runtime installation, or a modern GPU
- verify the selected target framework/Windows support against official documentation where a compatibility claim is needed; do not silently raise the build-17763 baseline

The shell must be sufficient to build and launch. It is not the Session 05 design system or Session 06 dashboard.

## B. Preserve core architecture and domain behavior

Keep these projects and their dependency direction where practical:

```text
AIUsageMonitor.Domain
AIUsageMonitor.Application
AIUsageMonitor.Infrastructure
AIUsageMonitor.Providers
AIUsageMonitor.Desktop
```

Preserve the provider-independent Domain/Application concepts and contracts already created, including:

- Provider, connection, account, subscription, and billing concepts
- dynamic quota definitions/windows
- used/remaining/limit normalization and validation
- UsageSnapshot and `UsageSnapshotChangeDetector`
- provider refresh result states and last-known-good retention
- alert/sync concepts
- `DateTimeOffset` semantics
- `ISecureCredentialStore` and opaque credential references
- provider-independent repository/service contracts where they remain sound

Domain must remain free of WPF, serializer details, filesystem paths, secure-storage implementations, HTTP, provider code, EF, and SQL. Application contracts may abstract persistence but must not know JSON filenames or LocalAppData implementation details.

If an existing contract was shaped around EF and no longer makes sense, change it only as far as needed for a clean file-backed implementation, preserve behavior, add focused tests, and record the material decision in `CURRENT_STATE`.

## C. Remove the historical database runtime

Remove the active runtime implementation of:

- EF Core packages and SQL provider packages
- `Microsoft.Data.SqlClient` where it is only needed by LocalDB persistence
- `DbContext` and SQL database options
- LocalDB connection strings and database initializers
- EF migrations from the active project/runtime
- SQL-specific repositories and SQL-only infrastructure paths
- SQL/LocalDB prerequisite UI or startup logic
- obsolete SQL security-workaround dependencies that are no longer needed

Do not leave dead EF migrations or a dead database initializer in the active runtime architecture. Do not delete historical Git commits or falsely remove the Session 03 validation record from `CURRENT_STATE`. Do not promise automatic LocalDB-to-file migration unless a safe, tested design is actually implemented; never destroy an existing user database as a shortcut.

## D. Implement Infrastructure file persistence

Use `System.Text.Json` and JSONL. Prefer clear store responsibilities over one giant generic file service. Candidate abstractions include equivalents of:

- `ISettingsStore`
- `IProviderStateStore`
- `ISubscriptionStore`
- `IUsageHistoryStore`
- `IAlertEventStore`

Preserve existing application repository contracts when useful rather than renaming abstractions without purpose. Infrastructure should provide concrete file-backed implementations and DI registration.

Use a safe application-data path:

```text
Environment.SpecialFolder.LocalApplicationData
approximately %LOCALAPPDATA%\AIUsageMonitor\
```

Do not write mutable runtime files beside the executable, under Program Files, or to a path requiring administrator privileges. Initialize the directory structure automatically at startup.

## E. JSON state documents and schema versioning

Use JSON for relatively small state/configuration documents such as settings, providers/current state, subscriptions, alert rules, and application metadata. Every long-lived JSON document must contain an explicit schema version such as:

```json
{
  "schemaVersion": 1
}
```

Design deserialization so unsupported versions are reported distinctly from malformed JSON and so future migrations are possible. Preserve `DateTimeOffset` values and source offsets according to the domain rules. Do not store tokens, passwords, cookies, refresh tokens, or raw authenticated payloads in any file.

## F. Safe writes and concurrency

Critical JSON writes must use a safe pattern:

```text
serialize
  -> write temporary file
  -> flush where practical
  -> replace destination atomically where supported
```

Do not use unsafe direct overwrite for critical state. Serialize concurrent writes caused by background refresh, manual refresh, resume, or foreground activation using a small per-store/append synchronization mechanism such as `SemaphoreSlim`. Do not add distributed locks.

Add focused tests for:

- first write and replacement
- missing destination
- concurrent writer serialization where practical
- no partial critical JSON after a failed write simulation where practical
- no secret values in serialized output

## G. JSONL usage history

Replace the EF `IUsageSnapshotRepository` implementation with JSONL/file-backed persistence while preserving:

- material-change detection and duplicate suppression
- `GetLatest` behavior
- chronological range queries
- monthly partitioning or a simpler explicitly justified equivalent
- streaming/incremental reads
- source and confidence values
- `DateTimeOffset` and source offset semantics
- provider/quota identity and dynamic quota fields

Prefer paths such as `history/YYYY-MM.jsonl`. Each record must contain enough schema/record metadata for future evolution. Append lines without rewriting all history. Query only relevant files for a time range, preserve ordering, and do not load all lifetime history at startup. Use temporary test directories and deterministic clocks where useful.

Apply the same `UsageSnapshotChangeDetector` material-change rule used by the historical application logic. Do not append unchanged snapshots merely because a refresh occurred.

## H. Corruption and failure resilience

Storage code must distinguish:

- valid file
- missing file
- empty file
- unsupported schema
- corrupt JSON/JSONL record
- I/O failure
- permission failure

A missing optional file should produce a safe default/empty state where appropriate. A corrupt file must be reported/logged and isolated or quarantined/backed up where safe; never silently destroy it. A corrupt history partition must not prevent settings, the WPF shell, or other history partitions from loading. A failed provider or store must not prevent application startup or other providers from loading.

Retain last-known valid provider values when refresh fails and mark them stale/error. Never replace valid data with zero.

## I. Startup and composition

The WPF shell must launch when:

- no configuration exists
- the LocalAppData directory does not yet exist
- history is empty
- no provider is connected
- a provider CLI is absent
- one provider fails
- optional state is missing/corrupt and safe recovery is possible
- the machine is offline

Startup should initialize storage directories, load safe state, compose services, and activate the shell. It must not require a database, provider connection, developer tooling, or administrator permission. Do not add dashboard UI in this session.

## J. Self-contained publish foundation

Configure and validate the publish strategy without claiming release validation that was not performed.

Evaluate:

```text
SelfContained=true
PublishSingleFile=true
```

for WPF. Do not blindly enable trimming. Establish `win-x64`, `win-x86`, and `win-arm64` release targets where the SDK/environment allows. It is acceptable to report an architecture as build-not-validated when hardware or toolchain support is unavailable, but record exactly what was and was not run. Prove from publish output/configuration that a released artifact carries its .NET runtime and does not require a separately installed runtime.

## K. Test migration

Replace database integration tests with focused file-persistence tests. Retain valuable Domain/provider tests. At minimum cover:

- settings JSON round trip
- subscription JSON round trip
- provider state round trip
- schema version serialization/deserialization
- JSONL append/read
- chronological ordering and time-range filtering
- source offset preservation
- duplicate suppression
- missing/empty-file behavior
- corrupt-file isolation/recovery
- unsupported schema handling
- atomic/safe update behavior where practical
- credential reference persisted without a secret
- WPF shell/view-model startup behavior where useful

Do not carry meaningless EF migration, `DbContext`, LocalDB-installation, or database-initializer tests into the active architecture.

## L. Validation and documentation truth

Run the relevant restore/build/tests and publish checks. Review compile warnings attributable to the migration. Perform a source-scope check proving that no provider/dashboard/session-04 work slipped in. Update any implementation-facing documentation needed to keep architecture/status/path references truthful, without expanding product scope or executing future sessions. Inspect the final diff and changed files for secrets, tokens, raw payloads, debug files, and generated artifacts.

Update `.ai/CURRENT_STATE.md` with:

- Session 03 remains `COMPLETE - HISTORICAL/SUPERSEDED`
- Session 03R status and actual validation
- current source now reflects WPF/file persistence only if it truly does
- target/current distinction if any migration is partial
- self-contained publish architectures and actual results
- known limitations and blockers
- Session 04 remains `NOT STARTED`

Do not claim WPF/JSON is implemented if the migration is incomplete. Do not claim a clean-machine launch, ARM64 run, or self-contained artifact test that was not actually performed.

## M. Git delivery and stop condition

Follow the Git Delivery Contract in `AGENTS.md`:

1. update `CURRENT_STATE`
2. review the diff and secrets
3. commit on `refactor/session-03r-portable-consumer-architecture`
4. push the branch
5. merge into `main`
6. push `main`
7. fetch and verify `origin/main`
8. leave the working tree clean

Do not ask whether to commit, push, or merge. Do not execute Session 04. Stop after Session 03R delivery.

---

# SESSION 04 - Provider Feasibility Investigation

```text
EXECUTE SESSION 04 ONLY.
EVIDENCE/FEASIBILITY SESSION - DO NOT IMPLEMENT PROVIDER ADAPTERS.
```

Inherit the active WPF/JSONL/zero-prerequisite/Cross-Windows/Git rules above. Confirm Session 03R and Gate A are complete before starting.

Investigate Codex, Claude, Kimi, GitHub Copilot, and Google Antigravity using official documentation, safe local inspection, and approved account workflows. For each provider determine:

1. installation/application detection
2. account and authentication-state detection
3. official usage API or account usage endpoint
4. OAuth/device/account connection options
5. browser-only workflow if officially supported
6. official CLI/status/usage command, if any
7. safe verified local metadata
8. actual quota windows/credits exposed
9. whether values mean used or remaining
10. absolute used/remaining/limit availability
11. reset timestamp and timezone
12. plan and subscription/renewal information
13. network and rate-limit requirements
14. auth expiry behavior
15. manual/partial/unsupported fallback

Answer explicitly:

- Can a non-developer connect it?
- Can a browser-only user use it?
- Is CLI required, or can it remain optional?
- What is official, verified local, inferred, manual, unavailable, or unsupported?

Do not extract browser cookies/passwords, print tokens, commit raw payloads, or assume an online document proves an account endpoint exists. Record a capability/evidence matrix and safe implementation recommendation in `CURRENT_STATE`. Do not create provider production code. Validate the existing solution only as appropriate, update state, deliver the assigned evidence session, and stop before Gate A/Session 05.

---

# REVIEW GATE A - OPUS 5

```text
REVIEW GATE A ONLY. DO NOT IMPLEMENT FEATURES.
```

Read governance, current state, the full source/test tree relevant to Sessions 01-04, and Git history/diff where useful. Review:

- WPF is the active desktop technology and WinUI/Windows App SDK are not active runtime dependencies
- Domain/Application dependency direction and dynamic quota invariants
- EF Core/SQL/LocalDB absence from the active runtime
- JSON/JSONL stores, schema versioning, atomic writes, synchronized writes, corruption handling, and `DateTimeOffset`
- file-backed duplicate suppression, latest/range queries, streaming, and LocalAppData behavior
- zero-prerequisite/self-contained publish strategy for x64/x86/ARM64
- secure credential boundary and no secrets in files/logs
- provider feasibility evidence and non-developer/browser-only connection feasibility
- Cross-Windows capability/fallback behavior

Classify BLOCKER/HIGH/MEDIUM/LOW. Opus must reject the gate if the active architecture still requires LocalDB/SQL/EF, if a mandatory external runtime/database is hidden, or if provider feasibility is unsupported by evidence. Output architecture, security, file-persistence, provider-feasibility, and compatibility verdicts plus required remediation. Verdict exactly:

```text
APPROVED TO CONTINUE
```

or:

```text
REJECTED - BLOCKERS LISTED
```

---

# SESSION 05 - WPF Modern Design System

```text
EXECUTE SESSION 05 ONLY after Gate A approval or assigned remediation.
```

Build a reusable WPF design system for a modern AI capacity cockpit:

- premium dark-first shell
- light/system theme option
- resource/design tokens
- typography hierarchy
- spacing/layout scale
- rounded cards and buttons
- modern icons and provider accents
- status badges and progress bars/rings
- readable remaining-capacity emphasis
- loading, empty, stale, partial, and error states
- responsive resizing and high-DPI behavior
- keyboard navigation, focus, accessible labels, and contrast
- restrained, lightweight motion

Do not rely on Mica/Acrylic/WinUI-specific effects. Optional OS effects may enhance but never determine usability. Do not implement providers or dashboard data collection. Use clearly identified design-time data only. Build/manual-validate what is available, update `CURRENT_STATE`, deliver, and stop.

---

# SESSION 06 - Main Dashboard

```text
EXECUTE SESSION 06 ONLY.
```

Implement the WPF dashboard for general AI users. It must answer quickly:

- how much available/remaining capacity exists
- when relevant windows reset
- which provider has the best available capacity

Include header/last-updated state, overall capacity summary, best-capacity provider, next reset, responsive provider cards, dynamic quota rows, credit-only layouts, connection status, stale/partial/error states, manual Refresh All, refresh progress, and a capacity insight placeholder/binding.

Use language such as Available capacity, Remaining, Used, Resets in, Plan, Credits, Last updated, Connection required, Manual data, and Provider unavailable. Do not require the user to understand CLI/shell/SDK/API-key terminology unless setup genuinely requires it. Do not parse provider payloads in WPF or fabricate missing quota windows. Validate compact/normal/large windows and high DPI where possible, update state, deliver, and stop.

---

# SESSION 07 - System Tray and Focus HUD

```text
EXECUTE SESSION 07 ONLY.
```

Implement lightweight WPF desktop behavior:

- tray icon and menu
- Open Dashboard
- Show/Hide Focus HUD
- Refresh All
- Pause/Resume Monitoring hook
- Settings
- explicit Exit
- predictable minimize/close-to-tray behavior
- no orphan process

Implement the optional compact Focus HUD with provider rows, key remaining quotas, best available provider, stale/error indicators, compact/expanded mode, always-on-top option, remembered position/size/mode, optional opacity if stable, manual refresh, and dashboard/provider navigation. Keep it readable beside an IDE and useful to non-developers. Do not introduce Chromium/browser background processes or WinUI-only effects. Use JSON settings where persistence is needed. Validate lifecycle/DPI/resizing where possible, update state, deliver, and stop.

---

# SESSION 08 - Codex Provider

```text
EXECUTE SESSION 08 ONLY after Session 04 evidence and Gate A approval.
```

Use only current, verified Codex mechanisms. Re-verify evidence if behavior changed. Implement the safest verified detector, authentication/account state, usage collector, parser, normalizer, actual quota windows, reset timestamps, plan/account/subscription fields, structured errors, stale/last-known state, and manual fallback for unavailable fields. Do not hardcode Codex as 5-hour + weekly unless the actual verified account exposes those windows. CLI is optional. No browser cookies, raw tokens, undocumented endpoint guesses, or unsanitized fixtures. Persist only normalized state/history through JSON/JSONL stores. Add focused sanitized parser/normalization/timezone tests, compare against the provider's own usage surface where safe, integrate generic dashboard/HUD contracts, update state, deliver, and stop.

---

# SESSION 09 - Claude Provider

```text
EXECUTE SESSION 09 ONLY after Session 04 evidence and Gate A approval.
```

Use verified Claude mechanisms. Where actually exposed, implement detection, connection state, plan, five-hour/session quota/reset, weekly quota/reset, model-specific quota, credits/extra usage, and subscription metadata. Normalize to generic dynamic quota objects and remaining-capacity semantics. Handle auth expiration, partial/stale state, malformed source, timeouts, and offline behavior. CLI remains optional and browser credential scraping is prohibited. Use JSON/JSONL persistence, sanitized fixtures, focused tests, dashboard/HUD integration, and manual comparison with Claude's visible usage surface where safe. Update state, deliver, and stop.

---

# SESSION 10 - Kimi Provider

```text
EXECUTE SESSION 10 ONLY after Session 04 evidence and Gate A approval.
```

Use verified Kimi/Kimi Code mechanisms. Where exposed, implement detection, account/membership plan, rolling five-hour quota, weekly quota, monthly credits, Extra Usage, reset timestamps, subscription metadata, partial/stale/auth-required outcomes, and manual fallback. Prefer official API/account/CLI mechanisms verified in Session 04; CLI is optional. Normalize dynamic windows and used/remaining semantics, persist through JSON/JSONL, add sanitized parser tests, integrate UI, compare values with the provider's own usage surface where safe, update state, deliver, and stop. Never extract browser cookies or fabricate unavailable fields.

---

# SESSION 11 - GitHub Copilot Provider

```text
EXECUTE SESSION 11 ONLY after Session 04 evidence and Gate A approval.
```

Use GitHub-supported authentication only. Implement verified GitHub identity, Copilot plan, AI Credits used/remaining, monthly reset/billing context, subscription/renewal metadata, and manual fallback where safely available. Do not invent 5-hour or weekly Copilot windows. If an endpoint does not expose a field, mark it unavailable or manual rather than scraping browser pages/cookies. Store only an opaque credential reference in JSON; actual credentials belong in secure storage. Add focused tests, normalized JSON/JSONL persistence, dashboard/HUD integration, compare to GitHub's supported usage view, update state, deliver, and stop.

---

# SESSION 12 - Antigravity Provider

```text
EXECUTE SESSION 12 ONLY after Session 04 evidence and Gate A approval.
```

Use only verified safe Antigravity/Google mechanisms. Where exposed, implement installation detection, connection status, Google AI plan, actual quota/model pool, five-hour/weekly windows, reset timestamps, AI-credit/overage state, and subscription metadata. If programmatic access is unreliable, provide safe partial detection/status and manual fallback. Do not reverse-engineer unsafe credentials or scrape cookies. Normalize dynamic quotas, persist through JSON/JSONL, add sanitized tests, integrate the dashboard, manually compare where safe, update state, deliver, and stop.

---

# REVIEW GATE B - OPUS 5

```text
REVIEW GATE B ONLY. DO NOT IMPLEMENT FEATURES.
```

Review Sessions 08-12 against Session 04 evidence. Audit invented endpoints, undocumented scraping, browser cookies, token leakage, unsafe persistence, source semantics, used/remaining inversion, dynamic quota labels, reset timestamps/timezones, fabricated plans/subscriptions, stale data shown as current, last-known values replaced by zero, provider isolation, excessive traffic, duplicate history writes, and provider logic leaking into WPF. Confirm CLI optionality, non-developer/manual fallback truth, JSON/JSONL persistence, and Cross-Windows assumptions. Classify BLOCKER/HIGH/MEDIUM/LOW, give provider-by-provider/security/data-correctness verdicts, required remediation, and exactly `APPROVED TO CONTINUE` or `REJECTED - BLOCKERS LISTED`.

---

# SESSION 13 - Subscription Management

```text
EXECUTE SESSION 13 ONLY.
```

Implement subscription management using JSON stores, not a database. Support plan, original start, billing-period start/end, renewal, cancellation/paid-through semantics, auto-renew, price, currency, cadence, source, confidence, last verified, and manual fallback. Populate only verified provider data; missing values are valid; inferred dates must not appear as official facts. Clearly distinguish provider, verified-local, inferred, and manual values. Add service, persistence, WPF view/detail UI, focused tests, and no payment functionality. Update state, deliver, and stop.

---

# SESSION 14 - JSONL History and Analytics

```text
EXECUTE SESSION 14 ONLY.
```

Implement JSONL history retrieval and analytics for 24h, 7d, 30d, and billing-cycle views where available. Require incremental/streaming reads, relevant monthly partition selection, chronological ordering, remaining-capacity series, consumption delta, burn rate, reset event identification, trends, estimated exhaustion only with sufficient evidence, and estimated remaining-at-reset. Never fabricate interpolation across missing data; show stale/gap periods clearly; do not load all lifetime history unnecessarily. Add high-value math/file tests and responsive WPF charts, update state, deliver, and stop.

---

# SESSION 15 - Capacity Recommendation Engine

```text
EXECUTE SESSION 15 ONLY.
```

Implement deterministic capacity scoring only. Inputs may include current remaining capacity, shortest relevant window, weekly/monthly/credit capacity when actually exposed, reset proximity, burn rate, provider availability, stale confidence, and exhaustion. Outputs include score, ranking, explanation, recommended provider for extended work, and constrained-provider warning. Handle missing data, credit-only providers, stale data, unavailable providers, reset boundaries, and ties explicitly. Do not invoke an AI model or claim intelligence/model-quality ranking. Add focused edge-case tests and integrate dashboard/HUD insight, update state, deliver, and stop.

---

# SESSION 16 - Monitoring and Notifications

```text
EXECUTE SESSION 16 ONLY.
```

Implement low-overhead monitoring with approximately 60-second default refresh, startup/manual/resume/foreground triggers, cancellation, timeout, retry/backoff, rate-limit awareness, no overlapping refresh, provider isolation, and no meaningful quota consumption by monitoring itself. Persist monitoring/alert events through JSONL/file stores without high-frequency disk writes. Support configurable warning 30% remaining, critical 15%, exhausted, reset/restored, auth expired, stale provider, and upcoming renewal alerts. Deduplicate/cool down repeated notifications and use Windows notifications through a compatible adapter. Validate idle CPU/memory and last-known-good behavior, update state, deliver, and stop.

---

# SESSION 17 - Settings, Security and File Resilience

```text
EXECUTE SESSION 17 ONLY.
```

Harden JSON settings and local storage for enabled providers, refresh interval, startup behavior, theme, Focus HUD, notification thresholds, provider setup, and manual subscriptions. Implement schema upgrades, atomic writes, file synchronization, missing/empty/corrupt/unsupported files, quarantine/backups where useful, permissions/path/storage failures, partial history, and safe first-run defaults. Complete Windows Credential Manager/approved secure-storage integration, secure disconnect/remove behavior, opaque references, log redaction, sanitized diagnostics, provider auth recovery, and no-secret tests. Remove any database resilience assumptions. Update state, deliver, and stop.

---

# SESSION 18 - UX and Performance Polish

```text
EXECUTE SESSION 18 ONLY. NO NEW PRODUCT SCOPE.
```

Review remaining-capacity wording, reset readability, provider cards, typography, keyboard/accessibility, loading/empty/stale/partial/error states, themes, high DPI, resizing, Focus HUD, tray lifecycle, WPF rendering, and restrained animation. Profile startup, idle CPU, memory, JSONL large-history behavior, UI responsiveness, file/provider I/O, cancellation, disposal, and event leaks. Validate x64 primary plus x86/ARM64 builds where feasible, older supported hardware/Windows behavior, and no-modern-effects fallback. Do not redesign the architecture or add scope. Update state, deliver, and stop.

---

# REVIEW GATE C - OPUS 5

```text
REVIEW GATE C ONLY. DO NOT IMPLEMENT FEATURES.
```

Perform a complete pre-release review through Session 18. Inspect BRD coverage, WPF/MVVM boundaries, JSON/JSONL schema/versioning/safe writes/history, provider correctness, security, UX/accessibility, performance, resilience, monitoring, alerts, analytics, recommendation logic, self-contained deployment, Cross-Windows compatibility, and graceful degradation. Do not propose speculative features. Classify findings and issue release blockers, security, architecture, UX, provider, persistence, and compatibility verdicts. Verdict exactly `APPROVED TO CONTINUE` or `REJECTED - BLOCKERS LISTED`.

---

# SESSION 19 - Self-Contained Packaging and Release

```text
EXECUTE SESSION 19 ONLY after Gate C approval/remediation.
```

Prepare repeatable self-contained release artifacts and CI. Require:

- `SelfContained=true`
- `PublishSingleFile=true` where validated stable for WPF
- no blind trimming
- release artifacts for win-x64, win-x86, and win-arm64 where feasible
- no separately installed .NET runtime or SDK
- no SQL/LocalDB/database installer
- no Node/npm/Angular/Electron/embedded browser runtime
- user data outside the application install directory
- clean first-run directory initialization
- no live provider credentials in CI
- concise install/update documentation

Use VM/clean-machine testing where available. The acceptance test is a clean compatible Windows machine with no separately installed runtime, SDK, SQL, Node, or developer tooling launching the released application successfully. Do not claim architectures/OS runs not performed. Update state, deliver, and stop.

---

# SESSION 20 - Final Stabilization

```text
EXECUTE SESSION 20 ONLY. FINAL IMPLEMENTATION SESSION. NO NEW FEATURES.
```

Compare every V1 requirement against actual code and evidence. Validate `download -> launch -> use` with clean first launch, existing JSON/JSONL state, empty/missing/corrupt optional data, provider failure isolation, offline/stale behavior, credentials, history integrity, alerts, recommendations, WPF dashboard/HUD/tray, self-contained artifacts, x64/x86/ARM64 release considerations, high DPI, and Cross-Windows fallback behavior. Resolve BLOCKER/HIGH defects only. Inspect dead code, debug artifacts, accidental sample data, and secrets. Update `CURRENT_STATE` to RELEASE CANDIDATE only when justified, identify Final Review as next, deliver, and stop.

---

# FINAL RELEASE REVIEW - OPUS 5

```text
FINAL INDEPENDENT RELEASE REVIEW. DO NOT IMPLEMENT FEATURES.
```

Read `AGENTS.md`, the full BRD, plan, current state, complete source/tests, provider implementations, build configuration, CI, packaging, and relevant history. Classify every requirement PASS, PARTIAL, FAIL, or NOT APPLICABLE.

Explicitly confirm the release requirement of zero-prerequisite consumer deployment:

- WPF is the active UI technology
- WinUI and Windows App SDK are removed from the active runtime
- EF Core, SQL Server, and LocalDB are removed from the active runtime
- SQLite is not introduced without explicit approval
- JSON/JSONL are the active persistence formats with schema versioning
- safe writes, file synchronization, corruption handling, streaming history, and duplicate suppression work
- actual secrets remain external to local files and logs
- self-contained release does not require a .NET runtime installation, SDK, or developer tooling
- provider CLI is optional and browser/non-developer use is truthful
- provider data, source semantics, remaining percentages, reset timestamps, subscription data, and stale states are trustworthy
- general AI-user terminology is used
- WPF UI remains modern/premium, accessible, responsive, and compatible
- release artifacts and OS/architecture claims are backed by actual evidence

Audit especially fabricated provider data, used/remaining inversion, timezone errors, cookie/token handling, provider isolation, file-history math, notification spam, performance, packaging, and release usability. Output release verdict, blockers, severity findings, requirement compliance, security, architecture, UX, provider, persistence, and deployment verdicts.

Final verdict must be exactly:

```text
READY FOR PERSONAL PRODUCTION USE
```

or:

```text
NOT READY FOR PERSONAL PRODUCTION USE
```

Do not approve conditionally while BLOCKER/HIGH release issues remain.

---

# OPTIONAL - OPUS REMEDIATION HANDOFF PROMPT

Use only when a review gate rejects the project and the planner assigns remediation.

Act as the explicitly assigned implementation executor. Read the governance files, latest review findings, current state, Git status, and affected code. Resolve only the BLOCKER/HIGH findings and directly required supporting changes. Do not add product scope or proceed to the next planned session. Confirm each defect against evidence, implement the smallest production-quality correction, add focused validation, review the diff/secrets, update `CURRENT_STATE`, and follow the Git Delivery Contract. Return the project to the same review gate.
