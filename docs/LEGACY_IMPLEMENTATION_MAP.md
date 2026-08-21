# Legacy Implementation Inventory and APO Reuse Classification

**Document Version:** 1.0
**Status:** COMPLETE / DURABLE EVIDENCE ARTIFACT
**Date:** 21 August 2026
**Jira Story:** APO-19 — Inventory and classify legacy implementation for APO reuse
**Parent Epic:** APO-1 — APO Product Rebrand & Governance Rebaseline
**Planner / Acceptance Authority:** GPT-5.6 Sol
**Assigned Executor:** Gemini 3.7 (Auxiliary cost/quota-balancing executor)
**Authoritative BRD:** `docs/BRD.md` (v1.1)
**Operating Contract:** `AGENTS.md`

> Historical baseline note: this durable APO-19 evidence artifact records the repository and local
> root as they existed before APO-20. The old URL/path values below are intentionally retained as
> audit evidence and are not active repository or local-root references.

---

## 1. Purpose

This document provides the formal legacy implementation inventory and architectural reuse classification for the AI Project Orchestrator (APO) repository (`https://github.com/Hossam1104/AI-Project-Orchestrator`).

APO succeeds the previous "AI Usage Monitor" product identity. Rather than rewriting the codebase from scratch, APO preserves and evolves validated engineering assets. This inventory inspects all active solution files, Domain models, Application contracts, Infrastructure persistence components, WPF desktop composition, publish profiles, tests, and historical Git commits against the 17 approved APO Epics and the consolidated Business Requirements Document (`docs/BRD.md`).

Every meaningful implementation area is classified to guide GPT-5.6 Sol in Jira backlog backfill, architectural refactoring, and execution sequencing.

---

## 2. Assessment Baseline

| Property | Value / Evidence |
|---|---|
| **Active Repository** | `https://github.com/Hossam1104/AI-Project-Orchestrator` |
| **Active Local Project Root** | `D:\AI Tools\Hossam\AI Project Orchestrator` |
| **Historical pre-APO-20 Repository** | `https://github.com/Hossam1104/AI-Usage-Monitor-Tool` |
| **Historical pre-APO-20 Local Project Root** | `D:\AI Tools\Hossam\AI Usage Monitor Tool` |
| **Inspected Starting SHA** | `ae712335696d827a7a1a2d2464cf667f43430c33` |
| **APO-19 Final Main SHA** | `9659bf65bda4defc91b2383cf7f195637678485f` |
| **APO-20 Identity Rename Status** | Complete; repository and local-root identity renamed |
| **Session Branch** | `docs/APO-19-legacy-implementation-map` |
| **Authoritative Requirements** | `docs/BRD.md` (v1.1, approved 21 August 2026) |
| **Active Target Architecture** | C# / .NET 10, WPF, MVVM, modular clean architecture, `System.Text.Json`, JSON/JSONL local file persistence, Serilog, Windows Credential Manager, self-contained Windows artifacts |
| **Prohibited Runtime Tech** | EF Core, SQL Server, LocalDB, SQLite, WinUI, Windows App SDK, Node.js/npm, Electron, Tauri, Chromium |

---

## 3. Classification Definitions

Every evaluated capability is categorized into exactly one of the five approved classifications:

1. **Reuse As-Is**: The implementation already satisfies APO BRD requirements, adheres to clean architecture boundaries, has passing automated tests, and requires no immediate functional or design changes.
2. **Reuse With Extension**: The implementation represents a sound, verified technical foundation that satisfies core requirements but requires additional models, contracts, or capabilities to support full APO orchestration workflows.
3. **Refactor**: The implementation contains valuable logic or architecture, but its structure, boundaries, naming, or UX requires controlled refactoring to align with APO standards.
4. **Superseded**: The implementation was completed and valid under earlier architecture decisions but has been deliberately replaced (e.g., EF Core/LocalDB replaced by JSON/JSONL, WinUI replaced by WPF). Retained in Git history for traceability; prohibited in active runtime.
5. **Remove**: Active code or configuration that is dead, unsafe, incompatible, or contrary to the BRD. (None identified in current working tree).

---

## 4. Executive Mapping

| Area | Current Evidence | APO Epic | BRD Requirements | Classification | Validation | Gap / Future Work |
|---|---|---|---|---|---|---|
| **Solution & Target Framework** | `AIUsageMonitor.sln`, `Directory.Build.props`, 8 projects targeting .NET 10 | APO-2 / APO-17 | §1, §12, §13 | **Reuse With Extension** | Build passes (0w/0e) | Solution/project technical naming retains `AIUsageMonitor`; add CI workflows |
| **Dynamic Quota Domain** | `QuotaWindow.cs`, `QuotaDefinition.cs`, `QuotaType.cs`, `QuotaUnit.cs` | APO-4 | FR-CAP-002, FR-CAP-003, FR-CAP-004, FR-CAP-005 | **Reuse As-Is** | 28 Domain tests pass | Integrate into routing cost/capacity scoring engine (APO-9) |
| **Subscription Domain** | `Subscription.cs`, `BillingCadence.cs` | APO-4 | FR-CAP-001, §8.3, §10 | **Reuse As-Is** | Domain tests pass | Connect to provider collection adapters (APO-4) |
| **Provider Identity & Connections** | `Provider.cs`, `ProviderAccount.cs`, `ProviderConnection.cs`, `ProviderCode.cs` | APO-4 / APO-8 | FR-AGENT-001, FR-AGENT-003, FR-CAP-006, §10 | **Reuse With Extension** | ProviderConnection tests pass | Extend for Agent/Model capabilities, execution mechanisms, CLI/API routes (APO-8) |
| **Alerts & Sync Domain** | `AlertRule.cs`, `AlertEvent.cs`, `SyncEvent.cs` | APO-4 / APO-16 | FR-AUD-001, FR-NOTIFY-001, §11 | **Reuse With Extension** | AlertRule tests pass | Extend for orchestration run alerts, gate approvals, validation events (APO-16) |
| **Usage Snapshot Domain** | `UsageSnapshot.cs` | APO-4 | FR-CAP-003, FR-CAP-008, §11 | **Reuse As-Is** | Domain & Infra tests pass | Consumed by capacity history and burn rate analytics |
| **Application Capacity Contracts** | `IAiUsageProvider.cs`, `IRefreshOrchestrator.cs`, `IUsageSnapshotRepository.cs`, `ProviderRefreshResult.cs` | APO-4 | FR-CAP-006, FR-CAP-007, FR-CAP-008 | **Reuse With Extension** | 7 Provider tests pass | Implement multi-provider concurrency & background refresh loop |
| **Security & Time Contracts** | `ISecureCredentialStore.cs`, `IClock.cs`, `SystemClock.cs`, `ISettingsService.cs` | APO-2 / APO-3 | FR-SET-001, §12.1, §12.2 | **Reuse With Extension** | Used across Infra tests | Implement Windows Credential Manager adapter; typed settings |
| **Storage Layout & Resilience** | `ApplicationDataPaths.cs`, `StorageStartup.cs` | APO-3 | §8, §12.1, §13 | **Refactor** | 3 StorageStartup tests pass | Refactor paths for APO projects/runs/evidence; rename root dir under planner approval |
| **JSON Document Persistence** | `JsonFileStore.cs`, `VersionedJsonCollectionStore.cs`, `FileRecords.cs` | APO-3 | §8, §12.1, §12.2 | **Reuse With Extension** | Document tests pass | Add stores for `projects.json`, `agents.json`, `routing-policy.json` |
| **JSONL Monthly Event Persistence** | `JsonlEventStore.cs`, unterminated tail handling, stream queries | APO-3 / APO-16 | FR-AUD-001, FR-AUD-002, §8, §12.1 | **Reuse With Extension** | JSONL tests pass | Add partitions for audit logs, execution run traces, review findings |
| **JSON/JSONL Repositories** | 9 repository implementations in `Infrastructure/Persistence/Repositories` | APO-3 / APO-4 | §8, §12.1 | **Reuse As-Is** | 15 Infra tests pass | Complete for capacity monitoring; template for future APO repos |
| **WPF Desktop Shell** | `App.xaml.cs`, `MainWindow.xaml`, DI host composition, Serilog | APO-2 / APO-15 | FR-UI-001, FR-UI-002, §5, §13 | **Refactor** | Published shell runs | Smoke UI only; refactor into full MVVM Command Center with HUD/Tray |
| **Provider Implementations** | `AIUsageMonitor.Providers` project with `AssemblyMarker.cs` | APO-4 / APO-8 | §10, FR-CAP-001..009, FR-AGENT-001..005 | **Refactor** | Assembly loads | All 5 provider adapters (Codex, Claude, Kimi, Copilot, Antigravity) are new work |
| **Publish Profiles & Targets** | `win-x64.pubxml`, `win-x86.pubxml`, `win-arm64.pubxml`, `app.manifest` | APO-17 | §13, §14 | **Reuse With Extension** | Publish profiles verified | Add CI packaging workflows and clean-machine matrix validation |
| **EF Core 10 / SQL LocalDB** | Historical Session 03 (commits `41db8b4`, `774e268`) | Historical | §1, §7, §12 | **Superseded** | Replaced in Session 03R | Historical evidence only; prohibited in active runtime |
| **WinUI / Windows App SDK** | Historical Session 01/02 desktop foundation | Historical | §1, §7, §12 | **Superseded** | Replaced in Session 03R | Historical evidence only; WPF is active platform |
| **Numbered Session Roadmap** | Historical Sessions 04–20 in `SESSION_PROMPTS.md` | Historical | §1, §6, §15, §16 | **Superseded** | Marked non-executable | Replaced by APO-1 through APO-17 Epics |

---

## 5. Windows Platform and Solution Foundation

### 5.1 Current Architecture & Evidence
- **Solution File**: `AIUsageMonitor.sln` containing 8 projects (5 `src`, 3 `tests`).
- **Global Properties**: `Directory.Build.props` enforces C# `latest`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<Deterministic>true</Deterministic>`, `<Features>strict</Features>`.
- **Target Framework**: `net10.0` for Domain, Application, Infrastructure, Providers, and Tests; `net10.0-windows10.0.17763.0` with `TargetPlatformMinVersion=10.0.17763.0` for Desktop.
- **Application Manifest**: `app.manifest` configures Windows 10/11 compatibility GUIDs, `PerMonitorV2` DPI awareness, `longPathAware`, and `asInvoker` security level.
- **Host & Dependency Injection**: Uses `Microsoft.Extensions.Hosting` (10.0.0) and `Microsoft.Extensions.DependencyInjection` (10.0.0).
- **Logging**: Configured via `Serilog` (4.3.0) with `Serilog.Sinks.File` (7.0.0) rolling daily (14-day retention) and `Serilog.Sinks.Debug` (3.0.0).

### 5.2 Reuse Classification: Reuse With Extension
- **Reusable Elements**: Clean project dependency layering (`Desktop -> Application -> Domain`, `Infrastructure -> Application/Domain`, `Providers -> Application/Domain`), platform manifest, Serilog logging architecture, and .NET 10 compilation configuration.
- **Extension Needs**: Composition root in `App.xaml.cs` must be expanded to register future APO application services (Project Registry, Git Service, Tracker Adapters, Agent Executors, Routing Engine, Validation Engine, Review Engine, Acceptance Service).
- **Rebranding Note**: Solution file and project filenames currently use `AIUsageMonitor.*`. These may remain during initial development and be migrated under a dedicated planner-controlled refactoring task.

---

## 6. Domain and Application Architecture

### 6.1 Current Domain Implementation
The `AIUsageMonitor.Domain` project contains strict, self-validating entity models with zero external infrastructure dependencies:

1. **Quotas (`AIUsageMonitor.Domain.Quotas`)**:
   - `QuotaWindow`: Encapsulates dynamic capacity windows. Normalizes used vs remaining percentages ensuring exact 100% mathematical integrity (0.05% tolerance). Rejects contradictory explicit percentages vs absolute values. Preserves `DateTimeOffset` timezone offsets.
   - `QuotaDefinition`: Unique identifier, provider binding, window name, type, and unit.
   - `QuotaType`: Enumerates `RollingFiveHour`, `Daily`, `Weekly`, `RollingSevenDay`, `Monthly`, `BillingCycle`, `Credits`, `RequestAllowance`, `Tokens`, `Custom`.
   - `QuotaUnit`: Enumerates `Requests`, `Tokens`, `Credits`, `Currency`, `Percentage`, `Count`.
2. **Subscriptions (`AIUsageMonitor.Domain.Subscriptions`)**:
   - `Subscription`: Stores plan name, billing start/end, renewal/cancellation dates, auto-renew flag, price, currency, cadence, confidence, and verification timestamps. Ensures price is non-negative, currency is specified when price exists, and end date does not precede start date.
   - `BillingCadence`: `Monthly`, `Annual`, `Weekly`, `UsageBased`, `OneTime`, `Unknown`.
3. **Providers (`AIUsageMonitor.Domain.Providers`)**:
   - `Provider`: Root provider entity.
   - `ProviderCode`: Strongly typed provider codes (`Codex`, `Claude`, `Kimi`, `Copilot`, `Antigravity`).
   - `ProviderConnection`: Holds status, connection type, endpoint, and opaque `CredentialReference`.
   - `ProviderAccount`: Holds account name, email, organization, tier, and last-detected timestamps.
4. **Alerts & Sync (`AIUsageMonitor.Domain.Alerts`, `AIUsageMonitor.Domain.Sync`)**:
   - `AlertRule`: Threshold-based alerting rules with cooldown and enabled state.
   - `AlertEvent`: Fired alert instances with severity (`Info`, `Warning`, `Critical`) and type (`LowCapacity`, `Exhausted`, `Restored`, `Stale`, `AuthFailure`, `Renewal`, `Expiry`).
   - `SyncEvent`: Captures refresh attempt duration, success flag, change detection flag, and error codes.
5. **Usage (`AIUsageMonitor.Domain.Usage`)**:
   - `UsageSnapshot`: Immutable point-in-time capture of a `QuotaWindow` tied to a provider and quota definition.

### 6.2 Current Application Contracts
The `AIUsageMonitor.Application` project defines provider-independent interfaces:
- `IAiUsageProvider`, `IProviderRegistry`, `IProviderDiscoveryService`, `IRefreshOrchestrator`.
- `ProviderRefreshResult`, `ProviderRefreshOutcome` (`Success`, `Stale`, `AuthenticationRequired`, `Unsupported`, `RateLimited`, `ProviderError`, `NetworkError`).
- `IUsageSnapshotRepository`, `IUsageAggregationService`, `UsageSnapshotChangeDetector`.
- `ISubscriptionService`, `IQuotaDefinitionRepository`, `IProviderConnectionRepository`, `IProviderRepository`.
- `IAlertEvaluator`, `IAlertRuleRepository`, `IAlertEventRepository`, `ISyncEventRepository`.
- `ISecureCredentialStore`, `ISettingsService`, `IClock`, `SystemClock`.

### 6.3 Reuse Classification
- **Domain Quotas, Subscriptions, Usage Snapshots**: **Reuse As-Is**.
- **Domain Providers, Connections, Alerts, Sync**: **Reuse With Extension** (expand for Agent Registry, Model Routing, Orchestration Alerts).
- **Application Contracts**: **Reuse With Extension** (expand for project registry, Git/tracker integrations, execution contracts, validation, review, and acceptance use cases).

---

## 7. Persistence, Resilience, and Security Foundation

### 7.1 Current Persistence Implementation
The `AIUsageMonitor.Infrastructure` project implements a local-first, zero-prerequisite file persistence architecture:

1. **Storage Layout (`ApplicationDataPaths.cs`)**:
   - Stores per-user data under `%LOCALAPPDATA%\AIUsageMonitor\`.
   - Subdirectories: `history/`, `alerts/`, `sync/`, `logs/`.
   - Document paths: `settings.json`, `providers.json`, `connections.json`, `subscriptions.json`, `quota-definitions.json`, `alert-rules.json`.
2. **Startup Resilience (`StorageStartup.cs`)**:
   - `StorageStartup.TryInitialize()` wraps directory creation and environment resolution in safe try/catch blocks.
   - Returns a typed `StorageStartupResult` containing available status, resolved paths, or caught exception.
   - If LocalAppData is read-only or unavailable, the application starts in degraded mode with debug logging rather than crashing.
3. **Atomic JSON Document Storage (`JsonFileStore.cs`, `VersionedJsonCollectionStore.cs`)**:
   - Schema version metadata (`schemaVersion: 1`).
   - Writes serialize to a temporary file (`.tmp`), flush to disk (`FileStream.Flush(true)`), and atomically replace destination (`File.Move(..., overwrite: true)` with fallback).
   - In-memory path synchronization using `ConcurrentDictionary<string, SemaphoreSlim>` to serialize concurrent writes to the same store.
   - Corrupt files are quarantined to `<filename>.corrupt.<yyyyMMddHHmmss>` rather than overwritten or silently dropped.
   - Unsupported schema versions and empty files are detected and reported safely.
4. **Partitioned JSONL History Storage (`JsonlEventStore.cs`)**:
   - Monthly partition files (`yyyy-MM.jsonl`).
   - Streaming asynchronous reads via `IAsyncEnumerable<TRecord>`.
   - Range-bounded queries (`ReadRangeAsync`) reading only necessary partition files.
   - Optimized latest lookup (`ReadLatestAsync`) scanning newest partitions first.
   - Unterminated tail handling: inspects last byte before append and injects a newline if the previous session terminated abruptly, preventing corruption of existing lines.
5. **Material-Change Duplicate Suppression (`JsonUsageSnapshotRepository.cs`)**:
   - Uses `UsageSnapshotChangeDetector.HasMaterialChange()` to compare incoming quota windows with the latest snapshot.
   - Redundant snapshots with identical used/remaining values are skipped, keeping JSONL files compact.
6. **Credential Privacy (`ISecureCredentialStore.cs`, `ProviderConnection.cs`)**:
   - `ProviderConnection` stores only an opaque `CredentialReference` string in JSON.
   - Raw tokens, passwords, and secrets are strictly forbidden from JSON/JSONL files and logs.

### 7.2 Reuse Classification
- **Core Persistence Engines (`JsonFileStore`, `JsonlEventStore`, `VersionedJsonCollectionStore`)**: **Reuse With Extension** (can directly persist future APO documents like `projects.json`, `agents.json`, `routing-policy.json`, and JSONL audit traces).
- **Storage Layout (`ApplicationDataPaths`)**: **Refactor** (needs additional paths for `projects/`, `runs/`, `reviews/`, `evidence/`, and eventual root directory rename).
- **Secure Credentials**: **Reuse With Extension** (requires Windows Credential Manager concrete implementation).

---

## 8. AI Usage and Capacity Domain

### 8.1 Evaluated Components
- **Mathematical Invariants**: `QuotaWindow.NormalizePercentages()` prevents double-inversion bugs, enforces `used + remaining == 100`, and ensures percentages match absolute values (`used / limit * 100`).
- **Timezone Safety**: All timestamps use `DateTimeOffset`. Reset times preserve original offsets and convert safely to the user's local timezone.
- **Truthful Outcomes**: `ProviderRefreshResult` enforces truthful states:
  - `Failed()` with last-known data preserves old quota windows and transitions outcome to `Stale`.
  - `Failed()` without data returns `ProviderError` with error codes and zero fabricated values.
  - `AuthenticationRequired()` and `Unsupported()` return empty data.
- **Quota Flexibility**: Supports arbitrary rolling windows, credits, requests, and tokens without fixed schema columns.

### 8.2 Reuse Classification: Reuse As-Is
- The dynamic quota normalization, subscription facts, and refresh outcome invariants directly implement BRD Section 8.3 (FR-CAP-001 through FR-CAP-009) and Section 10.
- They are ready to serve as the capacity evaluation layer for APO's intelligent model routing engine (APO-9).

---

## 9. Desktop and WPF Foundation

### 9.1 Current State & Composition
- `App.xaml` / `App.xaml.cs`: Application entry point initializing storage via `StorageStartup`, configuring Serilog file/debug logging, building the Generic Host, and displaying `MainWindow`.
- Degraded Mode: If storage initialization fails, `App.xaml.cs` displays `MainWindow` with a warning status message and switches to debug-only logging.
- `MainWindow.xaml` / `MainWindow.xaml.cs`: Minimal dark-themed shell (`#FF10131A`) displaying title "AI Usage Monitor" and a status text block.

### 9.2 Reuse Classification: Refactor
- **Reusable Foundation**: Generic Host lifecycle, Serilog integration, storage initialization guard, and degraded startup handling.
- **Refactor Requirements**:
  1. The UI is currently a minimal smoke shell; it must be refactored into the full MVVM Command Center (APO-15) with ViewModels, navigation, and state bindings.
  2. User-facing window title and labels currently display "AI Usage Monitor" and must be rebranded to "AI Project Orchestrator (APO)".
  3. Support for the Focus HUD and System Tray components must be added under APO-15.

---

## 10. Packaging, Compatibility, and Release Quality

### 10.1 Evaluated Publish Profiles
Located in `src/AIUsageMonitor.Desktop/Properties/PublishProfiles/`:
- `win-x64.pubxml`: Target RID `win-x64`, `SelfContained=true`, `PublishSingleFile=true`, `PublishTrimmed=false`, `IncludeNativeLibrariesForSelfExtract=true`.
- `win-x86.pubxml`: Target RID `win-x86`, `SelfContained=true`, `PublishSingleFile=true`, `PublishTrimmed=false`, `IncludeNativeLibrariesForSelfExtract=true`.
- `win-arm64.pubxml`: Target RID `win-arm64`, `SelfContained=true`, `PublishSingleFile=true`, `PublishTrimmed=false`, `IncludeNativeLibrariesForSelfExtract=true`.

### 10.2 Compatibility Contract
- Minimum OS: Windows 10 version 1809 (build 17763.0) and Windows 11.
- Architectures: x64 (primary validation), x86, ARM64.
- Zero Prerequisites: Self-contained packaging ensures users do not require .NET runtime, SDK, SQL Server, Node.js, or external runtimes installed.

### 10.3 Reuse Classification: Reuse With Extension
- Profiles are correctly configured and functional.
- Extension work under APO-17: Establish GitHub Actions CI pipeline to automate multi-RID builds, run automated tests in CI, and perform clean-machine installation validation.

---

## 11. Tests and Quality Verification

### 11.1 Current Test Inventory (50 Tests Passing)

1. **`AIUsageMonitor.Domain.Tests` (28 Tests)**:
   - `QuotaWindowTests` (16 tests): Factory validation, percentage normalization, sum-to-100 enforcement, contradiction rejection, clamp behavior, non-negative limits, null-handling.
   - `DynamicQuotaCollectionTests` (3 tests): Multiple dynamic windows per provider, distinct window types.
   - `SubscriptionTests` (5 tests): Invariants, price/currency validation, billing period order validation.
   - `ProviderConnectionTests` (2 tests): Status transitions, credential reference opacity.
   - `AlertRuleTests` (1 test): Threshold, severity, cooldown invariants.
   - `FoundationSmokeTests` (1 test): Domain assembly loading.
2. **`AIUsageMonitor.Provider.Tests` (7 Tests)**:
   - `ProviderRefreshResultTests` (6 tests): Stale retention on failure, error code preservation, distinction between plain stale and failed-recovered, AuthenticationRequired/Unsupported empty data.
   - `FoundationSmokeTests` (1 test): Providers assembly loading.
3. **`AIUsageMonitor.Infrastructure.Tests` (15 Tests)**:
   - `JsonDocumentPersistenceTests` (6 tests): Read/write round-trip, atomic write replacement, concurrent write synchronization, corruption quarantine, unsupported schema handling, missing file handling.
   - `JsonlHistoryPersistenceTests` (5 tests): Append/read round-trip, monthly partition routing, date-range filtering, latest record lookup optimization, unterminated tail newline injection.
   - `StorageStartupTests` (3 tests): Usable per-user storage initialization, degraded result on directory failure, degraded result on path failure.
   - `TemporaryStore` (1 fixture): Safe isolated test directory creation and recursive cleanup.

### 11.2 APO Test Gaps (Future Work)
- Project Registry persistence & isolation tests (APO-5).
- Git & GitHub integration tests using sanitized fixtures (APO-6).
- Jira / Azure DevOps adapter mapping tests (APO-7).
- Model routing policy and score evaluation tests (APO-9).
- Bounded autonomous execution lifecycle tests (APO-11).
- Validation engine command execution and secret redaction tests (APO-12).
- Independent review finding parsing and remediation cycle tests (APO-13).
- Human approval gate enforcement tests (APO-14).
- WPF ViewModel command and state tests (APO-15).

---

## 12. Historical Superseded Architecture

The following implementations are preserved in Git history and current-state documentation as completed historical milestones but are strictly superseded:

### 12.1 EF Core 10 and SQL Server LocalDB (Session 03)
- **Commits**: Implementation `41db8b42c430c5af64b633cba4e8f129ab6e6a8e`, Merge `774e268` (08 August 2026).
- **Scope**: `AIUsageMonitorDbContext`, migrations (`20260808164735_InitialCreate`), EF entity configurations, LocalDB initializer, and 10 integration tests against real LocalDB.
- **Supersession Reason**: Superseded in Session 03R (`a708e49`) by the zero-prerequisite consumer requirement. LocalDB required external SQL Server installation and caused packaging bloat. Replaced by portable JSON/JSONL file persistence.
- **Status**: **SUPERSEDED — DO NOT REVIVE IN ACTIVE RUNTIME**.

### 12.2 WinUI / Windows App SDK Desktop Shell (Sessions 01–02)
- **Scope**: Windows App SDK 1.5/1.6 and WinUI 3 desktop shell.
- **Supersession Reason**: Superseded in Session 03R (`a708e49`) due to external MSIX/Windows App Runtime dependencies and compatibility limitations on older Windows 10 builds. Replaced by standard WPF targeting .NET 10.
- **Status**: **SUPERSEDED — DO NOT REVIVE IN ACTIVE RUNTIME**.

### 12.3 Legacy Numbered Provider Roadmap (Sessions 04–20)
- **Scope**: Historical session prompt sequence in `docs/SESSION_PROMPTS.md`.
- **Supersession Reason**: Superseded by APO-18 rebaseline and the 17-Epic Jira hierarchy in `docs/BRD.md`.
- **Status**: **SUPERSEDED — DO NOT EXECUTE**.

---

## 13. Rebranding and Identifier Impact Analysis

The product identity has formally transitioned to **AI Project Orchestrator (APO)**. To ensure smooth, safe refactoring, identifiers are classified as follows:

| Category | Identifier / Location | Current Value | Target Value | Action Plan |
|---|---|---|---|---|
| **User-Facing Branding** | `MainWindow.xaml` | `Title="AI Usage Monitor"` | `Title="AI Project Orchestrator"` | Update during APO-15 / shell refactor |
| **User-Facing Branding** | `MainWindow.xaml` | `Text="AI Usage Monitor"` | `Text="AI Project Orchestrator"` | Update during APO-15 / shell refactor |
| **User-Facing Branding** | `Directory.Build.props` | `<Product>AI Usage Monitor</Product>` | `<Product>AI Project Orchestrator</Product>` | Update in APO-2 platform refactor |
| **Logging Identity** | `App.xaml.cs` | `"AI Usage Monitor started..."` | `"AI Project Orchestrator started..."` | Update in APO-2 platform refactor |
| **File Storage Root** | `ApplicationDataPaths.cs` | `ApplicationDirectoryName = "AIUsageMonitor"` | `"AIProjectOrchestrator"` or `"APO"` | Migrate in APO-3 persistence task with backward-compatible migration |
| **Technical Namespaces** | All `.cs` files | `namespace AIUsageMonitor.*` | `namespace AIProjectOrchestrator.*` | Incremental refactor under Sol planner approval; preserve during initial tasks |
| **Project & Solution Names** | `AIUsageMonitor.sln`, `src/*.csproj` | `AIUsageMonitor.*` | `AIProjectOrchestrator.*` | Incremental refactor under Sol planner approval |

---

## 14. Jira Backfill Recommendations

The following backlog items are recommendations for GPT-5.6 Sol to review and create in Jira
project `APO`. The titles and target Epics below are planning input only; Jira keys must be assigned
by Jira after Sol creates the real Stories/Tasks. No provisional `APO-20` through `APO-32` numbering
is authoritative.
*(Note: Recommended initial status `Done` is only proposed where complete implementation and passing validation evidence exist in the current repository).*

### 14.1 Epic: APO-2 — Windows Platform & Application Foundation

1. **Title: Preserve and Rebaseline .NET 10 / WPF Platform & Host Composition**
   - **Target Epic**: APO-2 - Windows Platform & Application Foundation
   - **Type**: Story (Historical Backfill & Foundation)
   - **BRD Requirements**: §1, §12, §13, §14
   - **Implementation Evidence**: `AIUsageMonitor.sln`, `Directory.Build.props`, `app.manifest`, `App.xaml.cs`, `InfrastructureServiceCollectionExtensions.cs`.
   - **Acceptance Criteria**: .NET 10 WPF project compiles with 0 warnings/errors for `net10.0-windows10.0.17763.0`, generic host and Serilog logging configure safely, degraded mode handles storage initialization failure.
   - **Validation Required**: `dotnet build AIUsageMonitor.sln`, execution smoke check.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

2. **Title: Implement Windows Credential Manager Secure Credential Store**
   - **Target Epic**: APO-2 - Windows Platform & Application Foundation
   - **Type**: Story (New Implementation)
   - **BRD Requirements**: §12.2, FR-PROJ-003, FR-AGENT-001
   - **Implementation Evidence**: Contract `ISecureCredentialStore` exists in `Application/Security/`.
   - **Acceptance Criteria**: Concrete `WindowsCredentialManagerStore` implements `ISecureCredentialStore` using Win32 Credential Manager APIs; secrets are stored securely without writing to JSON/logs; unit tests with mocked/temporary credentials pass.
   - **Validation Required**: xUnit tests in `Infrastructure.Tests`.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Required (Security/Credential safety).
   - **Recommended Jira Status**: **To Do**

3. **Title: Refactor Application Identity and Technical Naming for APO**
   - **Target Epic**: APO-2 - Windows Platform & Application Foundation
   - **Type**: Story (Refactoring)
   - **BRD Requirements**: §1, §18
   - **Implementation Evidence**: Rebranding inventory in Section 13 of this document.
   - **Acceptance Criteria**: User-facing strings updated to AI Project Orchestrator; `Directory.Build.props` product name updated; logging strings updated. (Namespace and project file renames executed under planner guidance).
   - **Validation Required**: Full solution build and test run.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required.
   - **Recommended Jira Status**: **To Do**

### 14.2 Epic: APO-3 — Local Persistence, Resilience & Security Foundation

1. **Title: Preserve and Validate Schema-Versioned Atomic JSON Document Store**
   - **Target Epic**: APO-3 - Local Persistence, Resilience & Security Foundation
   - **Type**: Story (Historical Backfill)
   - **BRD Requirements**: §8, §12.1
   - **Implementation Evidence**: `JsonFileStore.cs`, `VersionedJsonCollectionStore.cs`, `JsonDocumentPersistenceTests.cs` (6 tests).
   - **Acceptance Criteria**: Atomic temporary file write and replacement, schema version verification, in-process synchronization, corrupt file quarantine (`.corrupt.<timestamp>`), missing/empty file handling.
   - **Validation Required**: 6 tests in `AIUsageMonitor.Infrastructure.Tests`.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

2. **Title: Preserve and Validate Monthly Partitioned JSONL Event Store with Tail Resilience**
   - **Target Epic**: APO-3 - Local Persistence, Resilience & Security Foundation
   - **Type**: Story (Historical Backfill)
   - **BRD Requirements**: §8, §12.1, FR-AUD-001
   - **Implementation Evidence**: `JsonlEventStore.cs`, `JsonlHistoryPersistenceTests.cs` (5 tests).
   - **Acceptance Criteria**: Monthly file partitioning (`yyyy-MM.jsonl`), asynchronous streaming queries (`IAsyncEnumerable`), date-range filtering, latest lookup optimization, unterminated line detection and newline injection on append.
   - **Validation Required**: 5 tests in `AIUsageMonitor.Infrastructure.Tests`.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

3. **Title: Preserve and Validate Storage Startup Resilience and Degraded Mode**
   - **Target Epic**: APO-3 - Local Persistence, Resilience & Security Foundation
   - **Type**: Story (Historical Backfill)
   - **BRD Requirements**: §8, §12.1, §13
   - **Implementation Evidence**: `StorageStartup.cs`, `StorageStartupTests.cs` (3 tests).
   - **Acceptance Criteria**: `StorageStartup.TryInitialize()` handles unavailable or read-only LocalAppData without throwing exceptions; reports available status and failure reason; enables host to start in degraded mode.
   - **Validation Required**: 3 tests in `AIUsageMonitor.Infrastructure.Tests`.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

4. **Title: Extend Storage Layout and Stores for APO Projects and Orchestration Records**
   - **Target Epic**: APO-3 - Local Persistence, Resilience & Security Foundation
   - **Type**: Story (New Implementation / Extension)
   - **BRD Requirements**: §8, §12.1, FR-PROJ-001, FR-AUD-001
   - **Implementation Evidence**: `ApplicationDataPaths.cs` and `JsonFileStore.cs`.
   - **Acceptance Criteria**: Extend `ApplicationDataPaths` with paths for `projects.json`, `agents.json`, `routing-policy.json`, and directories for `projects/`, `runs/`, `evidence/`, `reviews/`; implement corresponding repositories.
   - **Validation Required**: New unit tests in `Infrastructure.Tests`.
   - **Suggested Executor**: GPT-5.6 Luna Max
   - **Opus Review**: Required (Storage architecture).
   - **Recommended Jira Status**: **To Do**

### 14.3 Epic: APO-4 — AI Usage, Subscription & Capacity Monitoring

1. **Title: Preserve and Validate Dynamic Quota Normalization and Mathematical Invariants**
   - **Target Epic**: APO-4 - AI Usage, Subscription & Capacity Monitoring
   - **Type**: Story (Historical Backfill)
   - **BRD Requirements**: FR-CAP-002, FR-CAP-003, FR-CAP-004, FR-CAP-005
   - **Implementation Evidence**: `QuotaWindow.cs`, `QuotaDefinition.cs`, `QuotaWindowTests.cs` (16 tests), `DynamicQuotaCollectionTests.cs` (3 tests).
   - **Acceptance Criteria**: Mathematical percentage normalization (`used + remaining == 100`), contradiction rejection, arbitrary dynamic window support, timezone offset preservation, non-negative limits.
   - **Validation Required**: 19 tests in `AIUsageMonitor.Domain.Tests`.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

2. **Title: Preserve and Validate Subscriptions, Alert Rules, and Sync Event Models**
   - **Target Epic**: APO-4 - AI Usage, Subscription & Capacity Monitoring
   - **Type**: Story (Historical Backfill)
   - **BRD Requirements**: FR-CAP-001, §8.3, §10, §11
   - **Implementation Evidence**: `Subscription.cs`, `AlertRule.cs`, `SyncEvent.cs`, `SubscriptionTests.cs` (5 tests), `AlertRuleTests.cs` (1 test).
   - **Acceptance Criteria**: Subscription field validation, billing period order validation, alert rule thresholds and cooldowns, sync event tracking.
   - **Validation Required**: 6 tests in `AIUsageMonitor.Domain.Tests`.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

3. **Title: Preserve and Validate Usage Snapshot Change Detection and Repository**
   - **Target Epic**: APO-4 - AI Usage, Subscription & Capacity Monitoring
   - **Type**: Story (Historical Backfill)
   - **BRD Requirements**: FR-CAP-008, §11
   - **Implementation Evidence**: `UsageSnapshot.cs`, `UsageSnapshotChangeDetector.cs`, `JsonUsageSnapshotRepository.cs`.
   - **Acceptance Criteria**: Redundant snapshot suppression on non-material changes; monthly JSONL persistence; exclusive lock coordination per provider/quota pair.
   - **Validation Required**: Unit tests in `Infrastructure.Tests`.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

4. **Title: Implement Official Provider Capacity Adapters (Codex, Claude, Kimi, Copilot, Antigravity)**
   - **Target Epic**: APO-4 - AI Usage, Subscription & Capacity Monitoring
   - **Type**: Story (New Implementation)
   - **BRD Requirements**: §10, FR-CAP-001 through FR-CAP-007
   - **Implementation Evidence**: `IAiUsageProvider` contract and `AIUsageMonitor.Providers` project.
   - **Acceptance Criteria**: Verified collection adapters for all 5 providers; truthful data mapping (Official API, OAuth/Device, Authenticated endpoint, Local CLI, Local metadata, Manual fallback); last-known-good retention on error; no fabricated data.
   - **Validation Required**: Unit tests with sanitized fixtures in `AIUsageMonitor.Provider.Tests`.
   - **Suggested Executor**: GPT-5.6 Luna Max
   - **Opus Review**: Required (Provider truthfulness and credential safety).
   - **Recommended Jira Status**: **To Do**

### 14.4 Epic: APO-17 — Packaging, Compatibility, CI & Release Quality

1. **Title: Preserve Cross-Windows Multi-Architecture Publish Configuration**
   - **Target Epic**: APO-17 - Packaging, Compatibility, CI & Release Quality
   - **Type**: Story (Historical Backfill)
   - **BRD Requirements**: §13, §14
   - **Implementation Evidence**: `win-x64.pubxml`, `win-x86.pubxml`, `win-arm64.pubxml`, `app.manifest`.
   - **Acceptance Criteria**: Self-contained, single-file, untrimmed publish profiles for `win-x64`, `win-x86`, `win-arm64`; minimum target Windows 10 build 17763.
   - **Validation Required**: `dotnet publish` smoke check on x64.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required (already validated).
   - **Recommended Jira Status**: **Done**

2. **Title: Establish GitHub Actions CI Workflow for Automated Build, Test, and Multi-RID Packaging**
   - **Target Epic**: APO-17 - Packaging, Compatibility, CI & Release Quality
   - **Type**: Story (New Implementation)
   - **BRD Requirements**: §14, §15
   - **Implementation Evidence**: `Directory.Build.props`, solution structure.
   - **Acceptance Criteria**: GitHub Actions workflow running `dotnet restore`, `dotnet build`, `dotnet test`, and `dotnet publish` for `win-x64`, `win-x86`, and `win-arm64` on every pull request and push to `main`.
   - **Validation Required**: CI run execution on GitHub.
   - **Suggested Executor**: Claude Sonnet 5
   - **Opus Review**: Not required.
   - **Recommended Jira Status**: **To Do**

### 14.5 Historical Superseded Milestones (Archival Traceability Only)

- **HIST-01: Session 03 — EF Core 10 + SQL Server LocalDB Persistence** (Status: `Superseded`; Evidence: commits `41db8b4`, `774e268`).
- **HIST-02: Session 01 — WinUI 3 / Windows App SDK Desktop Shell** (Status: `Superseded`; Evidence: Session 01 commits).
- **HIST-03: Provider Sequence Sessions 04–20** (Status: `Superseded`; Evidence: `docs/SESSION_PROMPTS.md`).

---

## 15. Dependency and Sequencing Recommendations

```text
Phase 1: Governance & Foundation Consolidation (Current)
  APO-1 (APO-18 complete, APO-19 complete)
       │
       ▼
Phase 2: Platform & Local Persistence Extension
  APO-2 (Platform, Credentials, Naming) & APO-3 (Persistence Extension, Project Stores)
       │
       ▼
Phase 3: Workspaces & Core Integrations
  APO-5 (Project Registry) ──► APO-6 (Git/GitHub) ──► APO-7 (Jira/Azure DevOps)
       │                              │
       ▼                              ▼
Phase 4: Capacity & Agent Registry
  APO-4 (Provider Adapters & Engine) ──► APO-8 (Agent/Model Registry & Connectivity)
       │                                       │
       └───────────────────┬───────────────────┘
                           ▼
Phase 5: Intelligent Routing & Execution Runtime
  APO-9 (Quota-Aware Routing Engine) ──► APO-10 (Execution Contracts) ──► APO-11 (Autonomous Runtime)
                                                                                   │
       ┌───────────────────────────────────────────────────────────────────────────┘
       ▼
Phase 6: Quality, Review & Human Gates
  APO-12 (Validation Engine) ──► APO-13 (Independent Review) ──► APO-14 (Human Approval Gates)
                                                                                   │
       ┌───────────────────────────────────────────────────────────────────────────┘
       ▼
Phase 7: Command Center UX, Audit & Release Qualification
  APO-15 (Command Center UX) & APO-16 (Activity/Audit Streams) ──► APO-17 (CI/CD & Release Qualification)
```

---

## 16. Risks and Open Questions

1. **Namespace & Project Naming Migration**:
   - *Risk*: Renaming solution, projects, and namespaces from `AIUsageMonitor` to `AIProjectOrchestrator` / `APO` in one step risks breaking Git history tracking and active file paths.
   - *Mitigation*: Perform technical rebranding in a dedicated, isolated Jira Story approved by Sol, maintaining backwards compatibility for `%LOCALAPPDATA%` paths.
2. **Provider API Entitlements vs Consumer Subscriptions**:
   - *Risk*: Users may expect consumer web subscriptions (e.g. ChatGPT Plus, Claude Pro) to automatically enable CLI/API execution.
   - *Mitigation*: Maintain strict distinction between capacity monitoring (read-only quota inspection) and agent execution (requiring explicit API/CLI credentials and connectivity).
3. **Multi-Architecture Release Testing**:
   - *Risk*: While x86, x64, and ARM64 builds compile and publish, ARM64 hardware testing has not yet been executed locally.
   - *Mitigation*: Clearly label ARM64 publish as "compiled/published; hardware validation pending" until validated on ARM64 hardware.

---

## 17. Planner Handoff

- **Assigned Story APO-19**: COMPLETE.
- **Durable Mapping Artifact**: Delivered at `docs/LEGACY_IMPLEMENTATION_MAP.md`.
- **Product Source Code Changes**: ZERO (Strictly preserved).
- **Next Planner Boundary**:
  - **No APO product source refactoring was performed.**
  - **GPT-5.6 Sol must review this legacy implementation map, create/approve the Jira backfill and implementation Stories, select the first implementation Story, and issue its execution contract.**
