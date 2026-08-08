
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

# PLANNER ADDENDUM — SESSION 03R

The existing Session 03R prompt remains authoritative.

Apply these two corrections during Session 03R.

## 1. Correct Session 04 / Gate A sequencing

A circular dependency currently exists in the planning documentation.

The correct sequence is:

```text
Session 03R
    ↓
Planner checkpoint
    ↓
Session 04 — Provider Feasibility
    ↓
Opus Review Gate A
    ↓
Session 05
```

Session 04 must NOT require Gate A to already be complete.

Correct `docs/SESSION_PROMPTS.md` so Session 04 requires only:

* Session 03R completed
* Session 03R merged to `main`
* `origin/main` verified
* no unresolved planner blocker preventing provider feasibility work

Gate A occurs AFTER Session 04 and reviews Sessions 01–04.

Also correct any equivalent contradictory wording in `docs/IMPLEMENTATION_PLAN.md` and `.ai/CURRENT_STATE.md`.

Do not otherwise reorder the approved sessions.

## 2. Windows compatibility claims

Do not blindly claim that .NET 10/WPF officially supports every Windows 10 edition from build 17763 onward.

During Session 03R, verify the current official Microsoft .NET 10 Windows support matrix.

Keep these architectural targets:

* x86
* x64
* ARM64
* Windows 10 compatibility where supported
* Windows 11
* zero separately installed .NET runtime for self-contained releases

But distinguish:

```text
Officially supported OS/edition
```

from:

```text
technically builds/runs or best-effort compatibility
```

Do not falsely claim execution or official support on an OS/edition that was not verified.

If the approved broad Windows compatibility goal conflicts with the official .NET 10 support matrix, record that fact in `.ai/CURRENT_STATE.md` for planner review rather than silently changing the runtime/framework.

This compatibility check must not block the WPF + JSON/JSONL migration itself unless an actual technical build/runtime incompatibility is discovered.

Do not execute Session 04.



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
