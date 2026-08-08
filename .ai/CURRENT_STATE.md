# AI Usage Monitor — Current State

**Last Updated:** 08 August 2026  
**Project:** AI Usage Monitor Tool  
**Local Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**GitHub:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**Default Branch:** `main`  
**Current Phase:** Phase 0 — Foundation  
**Last Completed Session:** Session 03 — EF Core 10 + SQL Server LocalDB Persistence  
**Next Session:** Session 04 — Provider Feasibility  
**Release State:** NOT STARTED

> SINGLE MUTABLE HANDOFF FILE.
> Every executor/reviewer must read this file.
> Every implementation session must update it before stopping.
> Keep it concise and factual.

---

## 1. Frozen Baseline

Approved V1:

- .NET 10
- C#
- WinUI 3
- Windows App SDK
- MVVM
- EF Core 10
- Microsoft SQL Server LocalDB
- Serilog
- targeted xUnit tests
- GitHub / GitHub Actions
- no Jira
- no Angular in V1
- no cloud backend in V1

Providers:

- Codex
- Claude
- Kimi
- GitHub Copilot
- Antigravity

---

## 2. Repository State

**GitHub repository:** `https://github.com/Hossam1104/AI-Usage-Monitor-Tool`  
**Remote target:** `https://github.com/Hossam1104/AI-Usage-Monitor-Tool.git`  
**Default branch:** `main`  
**Repository verified:** Yes  
**Local Git initialized:** Yes (pre-existing)  
**Origin configured:** Yes — matched required remote, unchanged  
**Session branch used:** `feature/session-01-repository-foundation`  
**Merged to main:** Yes  
**origin/main updated:** Yes  

**Pre-existing state found at session start:** The working tree already contained an uncommitted governance-file reorganization (root `BRD v1.0.md` deleted, `docs/BRD v1.0.md`, `docs/IMPLEMENTATION_PLAN.md`, `docs/SESSION_PROMPTS.md`, `AGENTS.md`, `CLAUDE.md`, `README.md` (empty), `.ai/CURRENT_STATE.md` untracked/modified) on top of one prior commit on `main` ("Implement feature X to enhance user experience and optimize performance"). This session committed that reorganization together with the Session 01 foundation as one coherent scope; nothing was discarded.

**Solution created:** Yes — `AIUsageMonitor.sln` (classic `.sln` format; `dotnet new sln` in this SDK defaults to `.slnx`, explicitly overridden with `--format sln` to match the BRD-specified filename)  
**Build status:** SUCCESS — 0 warnings, 0 errors, all 7 projects  
**Test status:** SUCCESS — 2/2 tests passed (1 per test project, smoke tests)  
**WinUI launch status:** SUCCESS — verified live (see §9)  
**Packaged release:** No (not in scope)  
**LocalDB database:** Created and migrated this session — `(localdb)\MSSQLLocalDB`, database `AIUsageMonitor`, no username/password (trusted connection); see §9E  

---

## 3. Session Progress

| Session | Scope | Status | Validation |
|---|---|---|---|
| 01 | Repository & solution foundation | COMPLETE | restore/build/test/launch all verified |
| 02 | Domain/application architecture | COMPLETE | build (default + explicit x64)/tests verified; merged to `main` and `origin/main` verified — see §9C |
| 02R | Domain integrity remediation | COMPLETE | build (x64)/tests verified (28/28, 7/7); merged to `main` and `origin/main` verified — see §9D |
| 03 | EF Core + LocalDB | COMPLETE | build/migration/round-trip/duplicate-prevention/full solution tests verified; merged to `main` and `origin/main` verified — see §9E |
| 04 | Provider feasibility | NOT STARTED | — |
| Gate A | Opus architecture review | NOT STARTED | — |
| 05 | WinUI design system | NOT STARTED | — |
| 06 | Main dashboard | NOT STARTED | — |
| 07 | Tray + Focus HUD | NOT STARTED | — |
| 08 | Codex provider | NOT STARTED | — |
| 09 | Claude provider | NOT STARTED | — |
| 10 | Kimi provider | NOT STARTED | — |
| 11 | GitHub Copilot provider | NOT STARTED | — |
| 12 | Antigravity provider | NOT STARTED | — |
| Gate B | Opus provider review | NOT STARTED | — |
| 13 | Subscription management | NOT STARTED | — |
| 14 | History + analytics | NOT STARTED | — |
| 15 | Capacity recommendation engine | NOT STARTED | — |
| 16 | Monitoring + notifications | NOT STARTED | — |
| 17 | Settings/security/resilience | NOT STARTED | — |
| 18 | UX/performance polish | NOT STARTED | — |
| Gate C | Opus pre-release review | NOT STARTED | — |
| 19 | Packaging/CI/release | NOT STARTED | — |
| 20 | Final stabilization | NOT STARTED | — |
| Final Gate | Opus release review | NOT STARTED | — |

Allowed status:
- NOT STARTED
- IN PROGRESS
- COMPLETE
- PARTIAL
- BLOCKED
- REQUIRES REVIEW

---

## 4. Current Implementation

### Toolchain (verified this session)

- Git: 2.55.0.windows.3
- .NET SDKs installed: 7.0.410, 8.0.423, 9.0.316, 10.0.110, **10.0.302 (active)**
- Visual Studio: 2026 Community 18.8.1 installed, but no Windows SDK / WindowsAppSDK VS workload/component detected in the VS installer inventory or registry (`HKLM\...\Windows Kits\Installed Roots` absent)
- No official `dotnet new` WinUI 3 templates available (`Microsoft.WindowsAppSDK.ProjectTemplates` does not exist as an installable `dotnet new` package) — the Desktop project was hand-authored as an **unpackaged** WinUI 3 app (`WindowsPackageType=None`, `UseWinUI=true`, `net10.0-windows10.0.19041.0`)
- Despite no local Windows SDK install, `dotnet restore`/`build` succeeded: `Microsoft.WindowsAppSDK` 2.3.1 and `Microsoft.Windows.SDK.BuildTools` 10.0.26100.6901 (restored from nuget.org, confirmed reachable) bundle the XAML compiler/rc.exe/makepri.exe needed to build WinUI without a system-wide Windows SDK

### Files/areas created

Governance path corrections (see §6) plus:

- `AIUsageMonitor.sln` (classic format)
- `Directory.Build.props` — .NET 10, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`, deterministic build, shared metadata
- `.editorconfig` — C# conventions, nullable diagnostics as warnings, `_camelCase` private fields
- `.gitignore` — bin/obj/.vs/TestResults/MSIX packaging output/local DB files etc.
- `README.md` — concise, reflects actual repo layout and stack
- `src/AIUsageMonitor.Domain` — class library, `net10.0`, empty except `AssemblyMarker.cs` (no business logic yet, per scope)
- `src/AIUsageMonitor.Application` — class library, `net10.0`, references Domain
- `src/AIUsageMonitor.Infrastructure` — class library, `net10.0`, references Application + Domain
- `src/AIUsageMonitor.Providers` — class library, `net10.0`, references Application + Domain, `AssemblyMarker.cs`
- `src/AIUsageMonitor.Desktop` — WinUI 3 unpackaged app, `net10.0-windows10.0.19041.0`, references Application only (Infrastructure reference deferred — nothing to compose yet); minimal `App.xaml(.cs)` with DI (`Microsoft.Extensions.DependencyInjection` + `Host.CreateDefaultBuilder`) and Serilog (console/debug + rolling file sink, no secrets logged) bootstrap; `MainWindow.xaml(.cs)` shows only the text "AI Usage Monitor"
- `tests/AIUsageMonitor.Domain.Tests` — xUnit, references Domain, one smoke test
- `tests/AIUsageMonitor.Provider.Tests` — xUnit, references Providers + Application + Domain, one smoke test

No business/domain/provider logic was implemented, per Session 01 scope.

### Session 02 additions (08 August 2026)

**`AIUsageMonitor.Domain`** — provider-independent domain models, no dependency on WinUI/EF Core/SQL Server/HTTP:

- `Common/` — `DataSource`, `ConfidenceLevel` enums
- `Providers/` — `Provider`, `ProviderAccount`, `ProviderConnection`, `ProviderCode`, `ProviderConnectionStatus`, `ProviderConnectionType`
- `Subscriptions/` — `Subscription`, `BillingCadence`
- `Quotas/` — `QuotaDefinition`, `QuotaWindow` (dynamic quota state — no fixed 5-hour/weekly properties; supports zero-to-many arbitrary windows per provider), `QuotaType`, `QuotaUnit`
- `Usage/` — `UsageSnapshot` (composes a `QuotaWindow` rather than duplicating its fields/validation)
- `Alerts/` — `AlertRule`, `AlertEvent`, `AlertType`, `AlertSeverity`
- `Sync/` — `SyncEvent`

`QuotaWindow.Create(...)` is the only way to construct a quota window. It normalizes used/remaining percentages exactly once from whatever combination of used/remaining/limit values or explicit percentages is supplied, throws on inconsistent explicit percentages (prevents silent double-inversion per AGENTS.md §8), throws if `ResetAt` precedes `WindowStart`, rejects negative values, and leaves percentages `null` (never fabricated) when there isn't enough data to compute them.

**`AIUsageMonitor.Application`** — provider-independent contracts consumed by Desktop/Infrastructure/Providers:

- `Time/` — `IClock`, `SystemClock`
- `Providers/` — `IAiUsageProvider`, `IProviderRegistry`, `IProviderDiscoveryService`, `IRefreshOrchestrator`, `ProviderDetectionResult`, `ProviderRefreshOutcome` (Success/Partial/AuthenticationRequired/Unsupported/Stale/ProviderError), `ProviderRefreshResult` (factory methods per outcome; `Stale`/`Partial` retain last-known quota windows rather than replacing them with zero, per AGENTS.md §20)
- `Usage/` — `IUsageAggregationService`, `ProviderUsageSummary`, `IUsageSnapshotRepository`
- `Subscriptions/` — `ISubscriptionService`
- `Alerts/` — `IAlertEvaluator`
- `Settings/` — `ISettingsService`
- `Security/` — `ISecureCredentialStore`

No provider-specific parsing, no EF Core/persistence implementation, no UI work — all deferred to later sessions per scope.

`tests/AIUsageMonitor.Domain.Tests/` gained 5 files (`QuotaWindowTests`, `DynamicQuotaCollectionTests`, `AlertRuleTests`, `SubscriptionTests`, `ProviderConnectionTests`) covering percentage normalization/validation, used/remaining consistency (including the "never treat used% as remaining%" invariant from AGENTS.md §8), arbitrary/mixed quota-type collections, `DateTimeOffset` offset preservation and start/reset ordering, and a few other important invalid-state guards (threshold ordering, billing-period ordering, price/currency pairing, sync-time ordering). 19/19 pass.

### Session 03 additions (08 August 2026)

**`AIUsageMonitor.Domain`** — one materialization-only change (see decision 25): `UsageSnapshot`'s four properties became `{ get; private set; }` and a `private UsageSnapshot(Guid id, Guid providerId, Guid quotaDefinitionId)` constructor was added for EF Core to bind. The public validating constructor is unchanged; no public setters were added; no EF Core package reference was added to Domain.

**`AIUsageMonitor.Application`** — new persistence-facing contracts and one pure application-logic class:

- `Providers/IProviderRepository.cs`, `Providers/IProviderConnectionRepository.cs`
- `Quotas/IQuotaDefinitionRepository.cs`
- `Alerts/IAlertRuleRepository.cs`, `Alerts/IAlertEventRepository.cs`
- `Sync/ISyncEventRepository.cs`
- `Usage/UsageSnapshotChangeDetector.cs` — pure/deterministic BRD §16 duplicate-snapshot rule (see §9E "Material-change rule")

**`AIUsageMonitor.Infrastructure`** — EF Core 10 + SQL Server LocalDB persistence layer, first real content in this project:

- `AIUsageMonitor.Infrastructure.csproj` — `Microsoft.EntityFrameworkCore`/`.SqlServer`/`.Design` 10.0.0, `Microsoft.Extensions.DependencyInjection.Abstractions`/`Logging.Abstractions` 10.0.0, plus an explicit direct pin of `System.Security.Cryptography.Xml` 10.0.10 to override a vulnerable (NU1903) transitive resolution pulled in by `Microsoft.Data.SqlClient` 6.1.1
- `Persistence/AIUsageMonitorDbContext.cs` — `DbSet<T>` for all 9 persisted entities; configurations applied from assembly
- `Persistence/PersistenceOptions.cs` — `DatabaseName = "AIUsageMonitor"`, default LocalDB connection string (`(localdb)\MSSQLLocalDB`, `Trusted_Connection=True`, no username/password)
- `Persistence/AIUsageMonitorDbContextFactory.cs` — `IDesignTimeDbContextFactory<T>` for `dotnet ef` CLI tooling only, not used at runtime
- `Persistence/SettingsEntry.cs` — the Infrastructure-only row shape backing `ISettingsService` (JSON value, never a secret)
- `Persistence/DatabasePrerequisiteStatus.cs`, `Persistence/DatabaseInitializationResult.cs`, `Persistence/IDatabaseInitializer.cs`, `Persistence/SqlLocalDbInitializer.cs` — graceful LocalDB-unavailable handling (see §9E)
- `Persistence/Configurations/*.cs` — one `IEntityTypeConfiguration<T>` per entity; every scalar property is explicitly declared (see decision 26 for why) and every `Id` is `ValueGeneratedNever()` (all IDs are domain-generated GUIDs)
- `Persistence/Repositories/Ef*.cs` — one EF Core-backed implementation per Session 02 Application repository/service contract
- `Persistence/Migrations/` — the `InitialCreate` migration (see §9E for manual schema-inspection result)
- `InfrastructureServiceCollectionExtensions.cs` — `AddInfrastructure()` DI composition-root extension

**`AIUsageMonitor.Desktop`** — now references `Infrastructure` (decision 14 resolved) and calls `services.AddInfrastructure()` in `ConfigureServices`; `OnLaunched` starts the host, calls `IDatabaseInitializer.InitializeAsync()`, logs a warning (never throws/crashes) if the database isn't ready, then activates the window regardless.

**`tests/AIUsageMonitor.Infrastructure.Tests/`** — new xUnit project (decision 27), 10 tests, all passing against real LocalDB: see §9E.

---

## 5. Provider Capability Matrix

Session 04 must replace investigation placeholders with evidence from the real environment.

Values:

- VERIFIED
- NOT AVAILABLE
- MANUAL FALLBACK
- FURTHER INVESTIGATION

| Capability | Codex | Claude | Kimi | Copilot | Antigravity |
|---|---|---|---|---|---|
| Installation detection | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Account/auth detection | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Plan | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Short/5h quota | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Weekly quota | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Monthly/credit quota | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Reset timestamp | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Original subscription start | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Current period/renewal | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Safe automatic collection | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION | FURTHER INVESTIGATION |
| Manual subscription fallback | VERIFIED | VERIFIED | VERIFIED | VERIFIED | VERIFIED |

### Provider Evidence Notes

#### Codex
Not investigated yet.

#### Claude
Not investigated yet.

#### Kimi
Not investigated yet.

#### GitHub Copilot
Not investigated yet.

#### Antigravity
Not investigated yet.

---

## 6. Architecture Decisions

Approved:

1. V1 is Windows desktop.
2. UI is WinUI 3.
3. Runtime is .NET 10.
4. Persistence is EF Core 10 + SQL Server LocalDB.
5. Quota model is dynamic.
6. Providers use adapters and normalized domain models.
7. Secrets are not stored in LocalDB.
8. Browser-cookie extraction is prohibited.
9. `.ai/CURRENT_STATE.md` is the single mutable handoff/status source.
10. Provider implementation requires evidence from Session 04.
11. GitHub is the only project/source-control platform; no Jira.
12. Solution file uses the classic `.sln` format (not the .NET 10 SDK's new default `.slnx`), to match the filename specified across the governance docs (`AIUsageMonitor.sln`).
13. `AIUsageMonitor.Desktop` is an **unpackaged** WinUI 3 app (`WindowsPackageType=None`, self-contained Windows App SDK), not MSIX-packaged. This was the only way to get a build/launch-verified WinUI 3 shell in an environment without a Windows SDK install or WindowsAppSDK VS workload; MSIX packaging can be introduced in Session 19 if the BRD's "packaged release" scope requires it.
14. `Desktop` does not yet reference `Infrastructure`. AGENTS.md permits `Desktop → Infrastructure` only through the composition root where actually required; Infrastructure has no services to compose yet, so the reference was deliberately omitted to avoid a meaningless dependency. Add it when Infrastructure gains registrable services (expected Session 03).
15. Governance-file path corrections (`BRD v1.0.md` → `docs/BRD v1.0.md`, `IMPLEMENTATION_PLAN.md` → `docs/IMPLEMENTATION_PLAN.md`, `SESSION_PROMPTS.md` → `docs/SESSION_PROMPTS.md`) were applied across `AGENTS.md`, `CLAUDE.md`, `docs/IMPLEMENTATION_PLAN.md`, and `docs/SESSION_PROMPTS.md`. `.ai/CURRENT_STATE.md` and `AGENTS.md`/`CLAUDE.md` themselves were left at their required root-level/`.ai/` locations, unchanged, per instruction.
16. **V1 Cross-Windows Compatibility Baseline (approved, permanent):**

    ```text
    Minimum OS:
    Windows 10 version 1809 / build 17763

    Supported OS:
    Windows 10 1809+
    Windows 11

    Architectures:
    x86
    x64
    ARM64

    Primary development validation:
    x64

    UI principle:
    Modern where available; compatible fallback everywhere supported.

    Core functionality must not depend on Windows 11-only APIs or modern hardware acceleration.
    ```

    This baseline is now the permanent contract in `AGENTS.md` §3A (inherited automatically by every Session 02+ executor/reviewer), reflected in `docs/BRD v1.0.md` §1A, `CLAUDE.md` §3A, `docs/IMPLEMENTATION_PLAN.md` §2A + relevant phase sections, and reminded at the top of every Session 02–20 prompt and review gate in `docs/SESSION_PROMPTS.md`. No application feature implementation occurred as part of establishing it — this was a governance/documentation-only update.

17. **`AIUsageMonitor.Application` project namespace collides with `Microsoft.UI.Xaml.Application`.** Once the Application project gained real source files under nested namespaces (e.g. `AIUsageMonitor.Application.Providers`), the sibling namespace `AIUsageMonitor.Application` became resolvable from `AIUsageMonitor.Desktop` (common `AIUsageMonitor` ancestor), and C# name lookup prefers an in-scope namespace over a `using`-imported type — so the unqualified `Application` base class in `App.xaml.cs` (`: Application`) started resolving to the namespace instead of `Microsoft.UI.Xaml.Application`, breaking the build (`CS0118`). Fixed by fully qualifying the base type: `public partial class App : Microsoft.UI.Xaml.Application`. This is a known, common clash in WinUI + Clean Architecture repos that name a layer "Application"; no project/namespace was renamed to avoid it, since the qualified reference is the minimal, standard fix. Any future Desktop-project code should qualify `Microsoft.UI.Xaml.Application` explicitly instead of relying on an unqualified `using Microsoft.UI.Xaml;` import.
18. **`QuotaWindow` is the single construction point for dynamic quota data.** `UsageSnapshot` composes a `QuotaWindow` rather than re-declaring/re-validating the same used/remaining/limit/percentage fields, so normalization logic exists in exactly one place. `ProviderRefreshResult` (Application layer) is the normalized shape every `IAiUsageProvider.RefreshAsync` returns; its factory methods keep construction to known-safe combinations (updated by decision 20 — see below).
19. **ARM64 solution-configuration gap (BRD §1A "Known Tracked Item") resolved before Session 02**, per planner instruction in the Session 02 prompt addendum — see §9A below and the corresponding BRD update. `docs/BRD v1.0.md` §1A now reflects the resolved state.

### Session 02R — Domain Integrity Remediation (08 August 2026)

Planner review of Session 02 raised 5 findings before EF Core turns the domain into a persistent database contract. All 5 were corrected; decisions recorded below.

20. **`ProviderRefreshResult.Error(...)` renamed/redesigned to `ProviderRefreshResult.Failed(...)`.** The old `Error(...)` unconditionally discarded `Account`/`Subscription`/`QuotaWindows` on any failure, contradicting AGENTS.md §20's last-known-data rule. `Failed(...)` now accepts optional `lastKnownAccount`/`lastKnownSubscription`/`lastKnownQuotaWindows`: if any are supplied, they are retained and the outcome resolves to `ProviderRefreshOutcome.Stale`; if none are supplied, the outcome is `ProviderRefreshOutcome.ProviderError` with no data (never fabricated). `ErrorCode`/`ErrorMessage` are retained in both branches — that's what distinguishes a `Failed(...)`-produced `Stale` result (has an error code) from a plain `Stale(...)`-produced one (periodic staleness, no active error, no error code). `Error(...)` had zero call sites anywhere in the repo (no provider exists yet), so this was a clean rename, not a breaking change to real code.
21. **`QuotaWindow.Create(...)` now validates absolute-value consistency, not just percentages.** Added: `UsedValue`/`RemainingValue` individually cannot exceed `LimitValue` (± a small `ValueTolerance` for provider-side rounding); when all three are supplied, `UsedValue + RemainingValue` must approximately equal `LimitValue`; and when explicit `usedPercentage`/`remainingPercentage` are supplied alongside absolute values, the two must materially agree (e.g. `Limit=100, Used=80, UsedPercentage=20` now throws instead of being silently accepted). Malformed provider input is rejected via `ArgumentException`/`ArgumentOutOfRangeException` rather than hidden behind `Math.Clamp`; `Math.Clamp` is now only a floating-point-rounding safety net after the true value has already passed validation, not a way to paper over a real contradiction.
22. **`Subscription` gained `ConfidenceLevel Confidence`.** It previously had `DataSource Source` but no confidence dimension, unlike `QuotaWindow`/`ProviderAccount`. Added as a required constructor parameter (not inferred automatically — the caller must state it) so persisted subscription data can distinguish Official/VerifiedLocal/Inferred/Manual before Session 03 gives it a database column.
23. **`ProviderConnection` gained an optional `string? CredentialReference`.** An opaque lookup key into secure storage (e.g. `"GitHub:Copilot:Primary"`), resolved through `ISecureCredentialStore` — never the secret itself. Rejects a whitespace-only value if one is supplied (via `ArgumentException`), but is otherwise unvalidated content since the actual reference format is provider/Infrastructure-defined. Windows Credential Manager implementation remains deferred to Session 17 per the BRD; this only adds the domain-level place to point at it.
24. **Git Delivery Contract gained a narrow metadata-only finalization exception (`AGENTS.md` §6A rule 15).** The contract's own `.ai/CURRENT_STATE.md` finalization step needs the merge/push/`origin/main` verification results to already exist, which is impossible to include in the commit that goes *into* that merge — Session 02 worked around this ad hoc with a small follow-up commit directly to `main`. Rule 15 now formally permits exactly that: a single post-merge commit touching only `.ai/CURRENT_STATE.md`/session metadata (never source code), still subject to branch protection (no bypass), requiring no feature branch of its own, and explicitly not requiring recursive documentation of itself.

### Session 03 — EF Core + LocalDB Persistence (08 August 2026)

25. **`UsageSnapshot` materialization strategy: private EF-only constructor + private setters, owned-type table-splitting via `OwnsOne`.** Per the mandatory Session 03 addendum, EF Core cannot constructor-bind the owned `Quota` (`QuotaWindow`) navigation when it is table-split onto `UsageSnapshots` (EF materializes the owner first, then the owned instance separately, then assigns it). Fix: `UsageSnapshot`'s four properties became `{ get; private set; }` (`Quota` defaults `= null!`), and a second `private UsageSnapshot(Guid id, Guid providerId, Guid quotaDefinitionId)` constructor was added, binding only the three scalar properties EF *can* set via the owning constructor; EF then assigns `Quota` afterward through its private setter. The public validating `UsageSnapshot(id, providerId, quotaDefinitionId, quota)` constructor is completely unchanged — application/domain code still cannot construct an `UsageSnapshot` without a valid `QuotaWindow`. No public setters were added. `QuotaWindow` itself needed no changes: its existing `private` constructor with parameter names matching property names 1:1 is already EF-constructor-bindable, and `UsageSnapshotConfiguration.OwnsOne(s => s.Quota, ...)` flattens all 13 of its properties onto `UsageSnapshots` columns (`ExternalKey`, `QuotaType`, `QuotaUnit`, `UsedValue`, `RemainingValue`, `LimitValue`, `UsedPercentage`, `RemainingPercentage`, `WindowStart`, `ResetAt`, `Source`, `Confidence`, `CapturedAt`) — no separate `QuotaWindows` table exists. Verified by a true fresh-`DbContext` round-trip test (§9E).
26. **Every `IEntityTypeConfiguration<T>` in this codebase must explicitly declare `.Property(...)` for every scalar property, not just the ones needing special treatment (conversions, max lengths, precision).** Discovered empirically: `dotnet ef migrations add` repeatedly failed with `No suitable constructor was found for type 'X'. Cannot bind '<param>' in 'X(...)'` for `AlertEvent` and then `AlertRule` — both fully-immutable, single-constructor domain entities — even though their constructor parameter names already matched property names 1:1 by ordinary EF convention, and even though other entities' *configured* properties bound fine. The properties that failed to bind were exactly the ones with no explicit `.Property()` call in their configuration. Root cause not fully isolated (candidate: EF Core 10's constructor-binding convention in this environment does not reliably fall back to convention-based scalar discovery for properties absent from the Fluent API when a type also has FK/relationship configuration present), but the reproducible fix is exhaustive explicit declaration. Applied to every entity configuration in `Persistence/Configurations/`, plus `.Property(x => x.Id).ValueGeneratedNever()` on every entity (independently correct regardless of this bug, since every ID in this codebase is domain-generated, never DB-generated). Any new entity configuration added in a later session must follow the same exhaustive pattern or risk the same migration-generation failure.
27. **New `tests/AIUsageMonitor.Infrastructure.Tests` xUnit project created**, deviating from the two-test-project layout implied by Sessions 01/02. Neither `AIUsageMonitor.Domain.Tests` (references only Domain — cannot reference EF Core/Infrastructure) nor `AIUsageMonitor.Provider.Tests` (scoped to provider adapters) is an appropriate home for real-LocalDB persistence/round-trip integration tests. References Domain, Application, and Infrastructure; added to `AIUsageMonitor.sln` via `dotnet sln add`. Each test class implements `IAsyncLifetime` and owns its own uniquely-named LocalDB database (created via the real `InitialCreate` migration, dropped on disposal) rather than sharing one fixture across classes/methods — required because `Providers.Code` is uniquely indexed and `ProviderCode` only has 5 fixed enum values, so concurrently-reused test data would collide if databases were shared.
28. **`Desktop → Infrastructure` reference added (resolves decision 14's deferral).** `AIUsageMonitor.Desktop.csproj` now references `AIUsageMonitor.Infrastructure`; `App.xaml.cs`'s `ConfigureServices` calls `services.AddInfrastructure()`, and `OnLaunched` (now `async void`, the standard WinUI pattern) starts the generic host and calls `IDatabaseInitializer.InitializeAsync()` before activating the main window. A non-ready result (LocalDB missing/unreachable) is logged via Serilog as a warning and the window is activated anyway — the application never crashes or blocks startup on a persistence failure, per the Session 03 "LOCALDB MISSING" rule. Surfacing this status in the UI itself is left to a later session (no UI work was in Session 03 scope).

Add only material decisions.

---

## 7. Open Blockers

None. Session 01 is COMPLETE with no unresolved blockers.

---

## 8. Known Limitations

- No local Windows SDK / WindowsAppSDK Visual Studio workload is installed on this machine; the Desktop project builds and runs entirely off NuGet-restored build tools (`Microsoft.Windows.SDK.BuildTools`). This is sufficient for CLI build/test/launch but should be re-verified if Visual Studio F5 debugging or MSIX packaging is attempted in a later session.
- `AIUsageMonitor.Desktop` builds for `x86` via plain `dotnet build` (MSBuild's default pick for this multi-platform WinUI project) and, as of Session 02, was also explicitly build-verified for `x64` via `dotnet build AIUsageMonitor.sln -c Debug -p:Platform=x64` (0 warnings/errors, all 7 projects — see §9B). `ARM64` was not built/launched this session (solution-configuration support exists per decision 16/19, but no actual ARM64 build/launch pass, hardware or emulated, has been performed yet); that remains outstanding and is expected no later than Session 18/19 per the Cross-Windows Compatibility Baseline. No platform was launched (only built) this session — Session 01's x86 launch verification is the most recent live-launch evidence.
- Actual provider collection methods have not yet been verified against the user's installed/authenticated environment — expected to be resolved/classified during Session 04 (unchanged from planning handoff).
- `.github/workflows/` was not created this session (no CI pipeline exists yet); deferred to Session 19 per the BRD.
- **Resolved this update:** `AIUsageMonitor.sln`'s `SolutionConfigurationPlatforms`/`ProjectConfigurationPlatforms` previously exposed only `x64`/`x86` even though the Desktop project already declared `Platforms=x86;x64;ARM64` and `TargetPlatformMinVersion=10.0.17763.0`. Added `Debug|ARM64`/`Release|ARM64` solution configurations, mapped to native `ARM64` for the Desktop project and to `Any CPU` for the six library/test projects (mirroring the existing `x64` mapping). Rebuilt (0 warnings/errors) and re-ran both test projects (2/2 passed) to confirm the change is safe. An actual ARM64 hardware/emulator build+launch pass is still outstanding (see the `x64`/`ARM64` validation item above).

---

## 9. Last Validation

Performed 08 August 2026, all commands actually executed (not assumed):

| Step | Command | Result |
|---|---|---|
| Restore | `dotnet restore AIUsageMonitor.sln` | SUCCESS — all 7 projects, including WinUI Desktop |
| Build | `dotnet build AIUsageMonitor.sln -c Debug` | SUCCESS — 0 Warning(s), 0 Error(s) |
| Test | `dotnet test tests/AIUsageMonitor.Domain.Tests/...csproj` | SUCCESS — 1/1 passed |
| Test | `dotnet test tests/AIUsageMonitor.Provider.Tests/...csproj` | SUCCESS — 1/1 passed |
| Launch | Ran `AIUsageMonitor.Desktop.exe` directly (x86, Debug) | SUCCESS — process stayed alive, `MainWindowTitle` = "AI Usage Monitor", `Responding = True`; Serilog wrote a structured startup line to `logs/aiusagemonitor-YYYYMMDD.log` next to the exe; process was then stopped cleanly |
| Secrets | `git diff --cached` scanned for password/token/key/PEM patterns | No secrets found — all matches were governance-document prose *about* not storing secrets |
| Build artifacts | Verified `git status`/staged diff contains no `bin/`, `obj/`, `.vs/` paths | Confirmed `.gitignore` is effective |

---

## 9A. Governance Update — Cross-Windows Compatibility (08 August 2026)

Performed as a mandatory pre-Session-02 governance/documentation update. No application feature implementation occurred.

| Step | Command | Result |
|---|---|---|
| Build (post-.sln ARM64 fix) | `dotnet build AIUsageMonitor.sln -c Debug` | SUCCESS — 0 Warning(s), 0 Error(s), all 7 projects |
| Test | `dotnet test tests/AIUsageMonitor.Domain.Tests/...csproj` | SUCCESS — 1/1 passed |
| Test | `dotnet test tests/AIUsageMonitor.Provider.Tests/...csproj` | SUCCESS — 1/1 passed |
| Documentation consistency | Repo-wide search for `Windows 11 only`, `Windows 11+`, `minimum Windows 11`, `x64 only`, `x86 only`, `modern Windows only`, `Windows 7`, `Windows 8`, `Windows 8.1` across `*.md` | No conflicting compatibility claims found; only rule statements (prohibiting Windows-11-only dependencies) and explicit non-support statements for Windows 7/8.1 |
| Secrets check | Reviewed diff of all governance-file edits and the `.sln` change | No secrets found — all changes are documentation/text and solution-configuration entries |

Updated: `docs/BRD v1.0.md` (§1A new), `AGENTS.md` (§3A new), `CLAUDE.md` (§3A new), `docs/IMPLEMENTATION_PLAN.md` (§2A new + phase/gate notes), `docs/SESSION_PROMPTS.md` (reminder added to all 19 Session 02–20 prompts + all 4 review gates, verified by count), `AIUsageMonitor.sln` (ARM64 solution/project configuration added), `.ai/CURRENT_STATE.md` (this file).

Session 02 was **not** started or executed as part of this update.

---

## 9B. Session 02 Validation (08 August 2026)

| Step | Command | Result |
|---|---|---|
| Build (default) | `dotnet build AIUsageMonitor.sln -c Debug` | SUCCESS — 0 Warning(s), 0 Error(s), all 7 projects (Desktop built `x86`, MSBuild's default) |
| Build (explicit x64, per Session 02 addendum) | `dotnet build AIUsageMonitor.sln -c Debug -p:Platform=x64` | SUCCESS — 0 Warning(s), 0 Error(s), all 7 projects (Desktop built native `x64`) |
| Test | `dotnet test tests/AIUsageMonitor.Domain.Tests/...csproj` | SUCCESS — 19/19 passed (17 new Session 02 domain-invariant tests + 2 pre-existing smoke/foundation tests) |
| Test | `dotnet test tests/AIUsageMonitor.Provider.Tests/...csproj` | SUCCESS — 1/1 passed (unchanged smoke test; Providers project intentionally untouched this session) |
| Secrets | Reviewed `git status --porcelain -uall` (excluding `bin`/`obj`) and `git diff` of all new/changed files for password/token/key/PEM/connection-string patterns | No secrets found — all new files are plain domain/application C# with no external I/O |
| Build artifacts | Confirmed no `bin/`, `obj/`, `.vs/` paths appear in the changed-file list | `.gitignore` remains effective |

Not validated this session (out of scope): ARM64 build/launch, WinUI launch/manual verification (no UI changes), EF Core/LocalDB (Session 03), any provider behavior (Session 04+).

---

## 9C. Git Delivery Contract Governance Update + Session 02 Integration (08 August 2026)

The project owner established a permanent Git Delivery Contract: every completed executor implementation/remediation session must commit, push its branch, merge into `main`, push `main`, and verify `origin/main` before stopping, without asking each time. This is now documented as `AGENTS.md` §6A (authoritative), with concise references in `CLAUDE.md` §3B, `docs/IMPLEMENTATION_PLAN.md` (Definition of Done + Git Workflow section), and a `GIT DELIVERY:` reminder block appended to every executor prompt in `docs/SESSION_PROMPTS.md` (Sessions 02–20 and the Opus remediation handoff prompt). Session 01 and the Opus-only review gates (A, B, C, Final Release Review) were intentionally left unchanged — reviewer gates stay review-only unless a reviewer is explicitly asked to modify repository files.

**Git integration — COMPLETE:**

| Step | Result |
|---|---|
| Branch | `feature/session-02-domain-application`, created from `main` (which was clean and up to date with `origin/main` at session start) |
| Implementation commit | `b23c076ce8a79c4a63e5b4232382396ccabc26d2` — "feat: establish domain and application architecture" |
| Governance commit | `6c341534f29b3b83f22db5428f2c1a9d959b12ce` — "docs: enforce automatic session git delivery" |
| Push branch | `feature/session-02-domain-application` pushed to `origin`, upstream tracking set |
| Post-merge build | `dotnet build AIUsageMonitor.sln -c Debug -p:Platform=x64` on `main` after merge — SUCCESS, 0 warnings/errors (merge was clean/fast, no manual conflict resolution needed, so full test re-run was not required per the Session 02 Git-delivery instructions; build re-verification was performed) |
| Merge to `main` | `git merge --no-ff feature/session-02-domain-application` — merge commit `ad8b6c97aebaeb650fcba1b92cd5bd6d1e6c65ab`, no conflicts |
| Push `main` | `git push origin main` — `64dd7bd..ad8b6c9 main -> main`, SUCCESS (no branch protection encountered) |
| Verify `origin/main` | `git fetch origin` + `git log -1 origin/main` — `ad8b6c97aebaeb650fcba1b92cd5bd6d1e6c65ab`, matches local `main` exactly |
| Working tree | clean (`git status` on `main` after push: "nothing to commit, working tree clean") |
| Current branch | `main` |

---

## 9D. Session 02R — Domain Integrity Remediation Validation + EF Core Readiness Notes (08 August 2026)

### Findings corrected

| # | Finding | Fix |
|---|---|---|
| 1 | `ProviderRefreshResult.Error(...)` discarded last-known Account/Subscription/QuotaWindows on failure | Replaced with `ProviderRefreshResult.Failed(...)`: retains any supplied last-known data and resolves to `Stale`; otherwise `ProviderError` with no data. ErrorCode/ErrorMessage always retained. See decision 20. |
| 2 | `QuotaWindow.Create(...)` only validated percentages, silently clamped contradictory absolute values | Added Used/Remaining ≤ Limit checks, Used+Remaining ≈ Limit check, and explicit-percentage-vs-implied-percentage cross-validation, all via `ArgumentException`/`ArgumentOutOfRangeException` rather than `Math.Clamp`. See decision 21. |
| 3 | `Subscription` had `DataSource Source` but no `ConfidenceLevel Confidence` | Added as a required constructor parameter. See decision 22. |
| 4 | `ProviderConnection` had no way to reference a securely stored credential | Added optional `string? CredentialReference` (opaque key only, whitespace-only rejected). See decision 23. |
| 5 | Git Delivery Contract had no way to record `.ai/CURRENT_STATE.md`'s own merge/push/verify outcome without a recursive merge | Added `AGENTS.md` §6A rule 15, a narrow metadata-only finalization-commit exception. See decision 24. |

### Validation

| Step | Command | Result |
|---|---|---|
| Build (x64) | `dotnet build AIUsageMonitor.sln -c Debug -p:Platform=x64` | SUCCESS — 0 Warning(s), 0 Error(s), all 7 projects |
| Test | `dotnet test tests/AIUsageMonitor.Domain.Tests/...csproj` | SUCCESS — 28/28 passed (19 prior + 9 new: 6 `QuotaWindow` absolute-value/percentage-conflict tests, 1 `Subscription` confidence test, 2 `ProviderConnection` credential-reference tests) |
| Test | `dotnet test tests/AIUsageMonitor.Provider.Tests/...csproj` | SUCCESS — 7/7 passed (1 prior smoke test + 6 new `ProviderRefreshResult` tests) |
| Secrets | Reviewed diff of all changed files for password/token/key/PEM patterns | No secrets found — `CredentialReference` is documented as an opaque lookup key only, never a real secret |
| Scope check | Confirmed no EF Core/persistence code was introduced and no fixed 5-hour/weekly schema was added | Confirmed — this remediation only touched Domain/Application contracts, tests, and governance docs |

### EF Core readiness notes for Session 03

Reviewed all Session 02/02R domain entities against what Session 03 will need to map:

- **`Provider`, `ProviderAccount`, `ProviderConnection`, `QuotaDefinition`, `AlertRule`, `AlertEvent`, `SyncEvent`** — straightforward EF Core aggregate roots: `Guid` key, simple scalar/enum properties, standard FK navigation (note `AlertRule.QuotaDefinitionId` is an *optional* FK — configure `IsRequired(false)`). No structural surprises. Enums (`ProviderCode`, `ProviderConnectionStatus`, `ProviderConnectionType`, `QuotaType`, `QuotaUnit`, `DataSource`, `ConfidenceLevel`, `BillingCadence`, `AlertType`, `AlertSeverity`) map to `int` by default; consider `HasConversion<string>()` for readability in LocalDB browsing, but this is a Session 03 style choice, not a blocker.
- **`Subscription.Price`** is `decimal?` with no declared precision/scale — SQL Server requires an explicit `HasPrecision(...)` (EF Core's default silently uses `decimal(18,2)` but emits a configuration warning if left implicit); Session 03 should set this explicitly rather than rely on the default.
- **`UsageSnapshot` → `QuotaWindow` needs an explicit owned-type mapping, not convention.** `QuotaWindow` has no `Id`/independent identity, a `private` constructor, and is only ever produced through its validating `Create(...)` factory — this is intentional (decision 18) and must **not** be weakened (no public parameterless constructor, no public setters) just to make EF's job easier. EF Core 10 supports constructor-binding materialization (matching constructor parameter names to property names) even for `private` constructors, so `QuotaWindow`'s existing shape is EF-compatible as-is. Session 03 must explicitly configure `UsageSnapshot` with `.OwnsOne(s => s.Quota, ...)`, mapping `QuotaWindow`'s properties as inline columns on the `UsageSnapshots` table (matching the BRD §30 flat-column schema) rather than a separate table/FK — relying on EF's default conventions for an owned type without this explicit call risks a modeling error (EF may otherwise try to treat `QuotaWindow` as a required reference navigation with its own key, which it doesn't have).
- **Duplicate-snapshot prevention (BRD §16 / Session 03 scope)**: a `(ProviderId, QuotaDefinitionId, ExternalKey)`-shaped index is a reasonable candidate for detecting "did this quota's value materially change since the last snapshot," but the actual material-change rule is Session 03's to define and test — no decision made here.

No EF Core code, migrations, or `Microsoft.EntityFrameworkCore.*` package references were added in this remediation.

### Git integration — COMPLETE

| Step | Result |
|---|---|
| Branch | `fix/session-02-domain-integrity`, created from `main` (clean, up to date with `origin/main` at session start) |
| Implementation commit | `2b70fc6465a85df09733d9a778e0c04529bd5a70` — "fix: strengthen domain integrity before persistence" |
| Governance/state commit | `e6c43e686d3e104faac4059a82c9c1af16e7b2e2` — "docs: record Session 02R findings and validation in CURRENT_STATE" |
| Push branch | `fix/session-02-domain-integrity` pushed to `origin`, upstream tracking set |
| Merge to `main` | `git merge --no-ff fix/session-02-domain-integrity` — merge commit `f2194f2447f3acc4b316ae553a272c45c2f53ef3`, no conflicts |
| Post-merge build | `dotnet build AIUsageMonitor.sln -c Debug -p:Platform=x64` on `main` after merge — SUCCESS, 0 warnings/errors (merge was clean/fast, no manual conflict resolution) |
| Push `main` | `git push origin main` — `44a38af..f2194f2 main -> main`, SUCCESS (transient network error on first attempt, succeeded on retry; no branch protection encountered) |
| Verify `origin/main` | `git fetch origin` + `git log -1 origin/main` — `f2194f2447f3acc4b316ae553a272c45c2f53ef3`, matches local `main` exactly |
| Working tree | clean (`git status` on `main` after push: "nothing to commit, working tree clean") |
| Current branch | `main` |
| Finalization commit | this commit, applying `AGENTS.md` §6A rule 15 (metadata-only, `.ai/CURRENT_STATE.md` only, no source changes) |

---

## 9E. Session 03 Validation — EF Core 10 + SQL Server LocalDB Persistence (08 August 2026)

### Materialization strategy and round-trip result

See decision 25 for the full strategy (private EF-only `UsageSnapshot` constructor + private setters + `OwnsOne` table-splitting; `QuotaWindow` unchanged). The mandatory round-trip test (`UsageSnapshotRoundTripTests.UsageSnapshot_SurvivesRoundTrip_ThroughFreshDbContext`) creates a `QuotaWindow` via `QuotaWindow.Create(...)`, wraps it in a `UsageSnapshot`, saves via one `DbContext`, disposes it, then loads by ID through a **second, fresh** `DbContext` (`AsNoTracking()`, no shared change tracker) and asserts equality on all 14 mandated fields (`Id`, `ProviderId`, `QuotaDefinitionId`, `ExternalKey`, `QuotaType`, `QuotaUnit`, `UsedValue`, `RemainingValue`, `LimitValue`, `UsedPercentage`, `RemainingPercentage`, `WindowStart`, `ResetAt`, `Source`, `Confidence`, `CapturedAt`). **Result: PASS**, run against a real, uniquely-named LocalDB database created via the actual `InitialCreate` migration (not `EnsureCreated`).

### Material-change rule (BRD §16 duplicate-snapshot prevention)

Implemented in `UsageSnapshotChangeDetector.HasMaterialChange(previous, current)` (pure/static, Application layer, no DB dependency):
- A `WindowStart` or `ResetAt` change is always material (new quota period), even if percentages happen to match.
- `UsedPercentage`/`RemainingPercentage` moves within 1.0 percentage point, and `UsedValue`/`RemainingValue`/`LimitValue` moves within 0.5 absolute units, are treated as noise.
- A value newly appearing or disappearing (`null` ↔ non-`null`) is always material — "no data" and "some data" are never treated as equivalent.

`EfUsageSnapshotRepository.AddAsync(...)` calls this against the most recently persisted snapshot for the same `(ProviderId, QuotaDefinitionId)` before writing; a non-material change is silently skipped (no new row), a material one is written. Verified by two dedicated integration tests against real LocalDB: one negligible-change case (asserts row count stays at 1) and one material-change case (asserts row count becomes 2).

### Initial migration — manual schema-inspection gate

`InitialCreate` (`src/AIUsageMonitor.Infrastructure/Persistence/Migrations/20260808164735_InitialCreate.cs`) was manually inspected line-by-line against the Session 03 gate:
- `UsageSnapshots` has the owned `QuotaWindow` fields flattened directly onto it as plain columns — confirmed no separate `QuotaWindows` table exists.
- `Subscriptions.Price` is `decimal(18,2)` with explicit `precision: 18, scale: 2` — not EF's implicit default.
- Regex scan (`AccessToken|RefreshToken|Password|Cookie|Secret|CredentialValue`, case-insensitive) across the full migration file: **zero matches**. `ProviderConnections.CredentialReference` (`nvarchar(256)`, nullable) is the only credential-adjacent column, documented in code as an opaque lookup key into a future `ISecureCredentialStore`, never the secret itself.
- `Settings` table has only `Key` (PK), `Value` (JSON text), `UpdatedAt` — no secret-shaped column.

`dotnet ef database update` was run against the real `(localdb)\MSSQLLocalDB` instance (database `AIUsageMonitor`) and applied cleanly on first try (clean-database creation verified).

### Test results

| Project | Result |
|---|---|
| `tests/AIUsageMonitor.Domain.Tests` | 28/28 passed (unchanged from Session 02R) |
| `tests/AIUsageMonitor.Provider.Tests` | 7/7 passed (unchanged from Session 02R) |
| `tests/AIUsageMonitor.Infrastructure.Tests` (new, decision 27) | **10/10 passed**, all against real LocalDB: 1 mandatory round-trip test, 2 duplicate-prevention tests, 5 write/read smoke tests (`ProviderConnection`+`CredentialReference`, `Subscription`+price precision, `AlertRule`+`AlertEvent`, `SyncEvent`, `Settings`), 2 `SqlLocalDbInitializer` tests (clean-DB-ready path, and unreachable-server path proving `InitializeAsync` never throws — returns a `DatabaseInitializationResult` with `IsReady=false` instead) |
| Full solution (`dotnet test AIUsageMonitor.sln`) | **45/45 passed** |

### Build

| Step | Command | Result |
|---|---|---|
| Build (full solution, incl. new Infrastructure.Tests + Desktop→Infrastructure wiring) | `dotnet build AIUsageMonitor.sln` | SUCCESS — 0 Warning(s), 0 Error(s), all 8 projects |
| Vulnerable transitive package | `dotnet list package --vulnerable --include-transitive` (Infrastructure) | `Microsoft.Data.SqlClient` 6.1.1 pulled `System.Security.Cryptography.Xml` 9.0.0 (NU1903, 8 high-severity advisories); fixed by an explicit direct pin to `10.0.10`. Re-checked after the pin: 0 vulnerabilities. |
| Secrets | Regex scan (`password=|pwd=|api[_-]?key|secret=|token=`) across all Session 03 new/changed `.cs` files | No secrets found — every match was `CancellationToken cancellationToken = default` (false positive on `token=`) |
| Build artifacts | `git status` reviewed | No `bin/`, `obj/`, `.vs/` paths staged; `.gitignore` remains effective |

### Not validated this session (out of scope)

- LocalDB-missing UI surfacing (Desktop only logs a warning today; a visible UI state is later-session scope).
- ARM64 build/launch (unchanged outstanding item from Session 01/02, tracked in §8).
- Any provider (Session 04+) or UI/dashboard (Session 05+) work.

---

## 10. Latest Reviewer Verdict

No review yet.

---

## 11. Next Execution

Run:

**Session 04 — Provider Feasibility**

Use the exact Session 04 prompt in `docs/SESSION_PROMPTS.md`.

Executor:
- Terra
- Luna
- or Sonnet

Session 04 must (per the Provider Discipline rules in `CLAUDE.md` §4 and `AGENTS.md`):
- investigate real, safe data-acquisition feasibility for Codex, Claude, Kimi, Copilot, and Antigravity
- never invent endpoint URLs or response fields; never infer billing data as fact; never extract browser cookies; never expose raw tokens; never commit unsanitized account payloads
- persist any findings using the Session 03 persistence layer's contracts (`IProviderRepository`, `IProviderConnectionRepository`, etc.) — do not bypass EF Core or hand-roll ad hoc storage
- a provider field that cannot be obtained safely must remain unavailable or use an approved manual fallback, never a fabricated value
- build + run targeted tests, review diff, update this file

Do not start dashboard/UI work (Session 05+).

---

## 12. Recent Handoff

### 08 August 2026 — Session 03 (EF Core 10 + SQL Server LocalDB Persistence) complete, merged to main

Implemented production-quality local persistence for the full Session 02/02R domain model using EF Core 10 + Microsoft SQL Server LocalDB: `AIUsageMonitorDbContext`, one `IEntityTypeConfiguration<T>` per entity, the `InitialCreate` migration, a design-time factory, `SqlLocalDbInitializer`/`IDatabaseInitializer` for never-throw graceful startup, one repository/service per aggregate (Providers, ProviderConnections, Subscriptions, QuotaDefinitions, UsageSnapshots, AlertRules, AlertEvents, SyncEvents, Settings), and `UsageSnapshotChangeDetector` for BRD §16 duplicate-history-row prevention. Followed the mandatory EF Materialization & Persistence Safety Addendum exactly: `QuotaWindow` stayed owned/value-like via `OwnsOne` table-splitting onto `UsageSnapshots` (no separate table), and `UsageSnapshot` gained only a private EF-only constructor plus private setters — its public validating constructor and all domain validation were left untouched (decision 25).

Along the way, discovered and fixed a real EF Core 10 constructor-binding limitation affecting every immutable, single-constructor domain entity: `dotnet ef migrations add` fails with "No suitable constructor was found ... Cannot bind '<param>'" unless literally every scalar constructor parameter has an explicit `.Property(...)` declaration in its configuration class, even when the parameter name already matches the property name 1:1 (decision 26) — applied across all nine entity configurations before the migration would generate cleanly.

Added a new `AIUsageMonitor.Infrastructure.Tests` xUnit project (10 tests, all against real LocalDB, no mocks): the mandatory `UsageSnapshot`/`QuotaWindow` round-trip test (write via one `DbContext`, read via a fresh second one, assert all 14 fields), two duplicate-prevention tests (negligible change skipped, material change written), five write/read smoke tests covering every other persisted entity including `ProviderConnection.CredentialReference`'s opaque round-trip and `Subscription.Price`'s explicit `decimal(18,2)` precision, and two `SqlLocalDbInitializer` tests proving initialization never throws even against an unreachable server. Each test class owns its own uniquely-named, freshly-migrated database via `IAsyncLifetime` (decision 27) after a shared-fixture design caused unique-index collisions on `Providers.Code`.

Manually inspected the generated `InitialCreate` migration against the mandatory secret-column gate: zero matches for `AccessToken|RefreshToken|Password|Cookie|Secret|CredentialValue`; `ProviderConnections.CredentialReference` remains the only credential-adjacent column and is confirmed opaque-only both in the schema and in tests. Wired `AIUsageMonitor.Desktop`'s `App.xaml.cs` to call `AddInfrastructure()` and `IDatabaseInitializer.InitializeAsync()` at startup, logging a warning (not crashing) if LocalDB is unavailable (decision 28), resolving the Session 02 deferral recorded as decision 14.

Full solution build: 0 warnings/errors across all 9 projects. Full test suite: 45/45 passing (28 Domain + 7 Provider + 10 Infrastructure, no regressions). Fixed one NU1903 transitive vulnerability advisory (`System.Security.Cryptography.Xml` pulled in via `Microsoft.Data.SqlClient`) by pinning a direct `PackageReference`. Full detail in §9E.

Branch `feature/session-03-ef-localdb-persistence` created from `main`, committed, pushed, merged to `main`, pushed, and `origin/main` verified to match exactly. Working tree clean on `main`.

### 08 August 2026 — Session 02R (Domain Integrity Remediation) complete, merged to main

Corrected 5 planner-identified findings in the Session 02 domain/application layer before EF Core (Session 03) turns it into a persistent contract: `ProviderRefreshResult.Failed(...)` now retains last-known data on failure instead of discarding it (decision 20); `QuotaWindow.Create(...)` now rejects internally contradictory absolute values instead of silently clamping them (decision 21); `Subscription` gained `ConfidenceLevel Confidence` (decision 22); `ProviderConnection` gained an optional opaque `CredentialReference` (decision 23); and the Git Delivery Contract gained a narrow metadata-only finalization-commit exception (`AGENTS.md` §6A rule 15, decision 24) to resolve the recursive-merge problem Session 02 worked around ad hoc — this very handoff entry is the first real use of that exception. Added 15 new targeted tests (9 Domain, 6 Provider/Application) — 28/28 and 7/7 respectively pass. Performed the requested EF Core readiness review (§9D) without implementing any EF Core code. `git status` was clean at session start (`main`, up to date with `origin/main`).

Branch `fix/session-02-domain-integrity` created from `main`, two commits, pushed, merged to `main` (`f2194f2`, no conflicts), pushed, and `origin/main` verified to match exactly. Working tree clean on `main`. Full detail in §9D.

### 08 August 2026 — Session 02 complete

Implemented the provider-independent domain model in `AIUsageMonitor.Domain` (Provider, ProviderAccount, ProviderConnection, Subscription, QuotaDefinition, QuotaWindow, UsageSnapshot, AlertRule, AlertEvent, SyncEvent, plus supporting enums) and the application-level contracts in `AIUsageMonitor.Application` (`IAiUsageProvider`, provider registry/discovery, refresh orchestration, `ProviderRefreshResult`, usage aggregation, subscription service, snapshot repository, settings, secure credential store, alert evaluator, clock). `QuotaWindow` supports a genuinely arbitrary, unbounded set of quota windows per provider with no fixed 5-hour/weekly schema, and normalizes used/remaining percentages exactly once with explicit invalid-state guards (see §4 and decision 18).

Hit and fixed one real build break: the new `AIUsageMonitor.Application` namespace collided with `Microsoft.UI.Xaml.Application` in the Desktop project's `App.xaml.cs` (see decision 17) — fixed with a fully-qualified base type reference, no renaming.

Per the Session 02 prompt's cross-Windows-compatibility addendum: built the full solution explicitly with `-p:Platform=x64` (success, see §9B) and updated `docs/BRD v1.0.md` §1A's "Known Tracked Item" wording to record that the ARM64 solution-configuration gap was resolved before Session 02 (it was fixed as part of the pre-Session-02 governance update, §9A) — an actual ARM64 build/launch pass is still outstanding for a later session.

Added 17 new targeted xUnit tests to `AIUsageMonitor.Domain.Tests` (19/19 total pass) covering quota percentage normalization/validation, used/remaining consistency, arbitrary/mixed quota-type collections, `DateTimeOffset` offset preservation and start/reset ordering, and other important invalid-state guards. No persistence, provider parsing, or UI work was done — all deferred per scope.

`git status` was clean at session start (`main`, up to date with `origin/main`).

### 08 August 2026 — Session 02 Git delivery + permanent Git Delivery Contract established

Per explicit project-owner instruction, established a permanent Git Delivery Contract (`AGENTS.md` §6A, referenced from `CLAUDE.md` §3B, `docs/IMPLEMENTATION_PLAN.md`'s Definition of Done/Git Workflow, and a `GIT DELIVERY:` reminder appended to every executor prompt in `docs/SESSION_PROMPTS.md` for Sessions 02–20 and the Opus remediation handoff): every completed executor session must now commit, push its branch, merge to `main`, push `main`, and verify `origin/main` before stopping, by default and without asking each time. Review-only Opus gates (A, B, C, Final Release Review) and Session 01 were left unchanged.

Completed Session 02's own Git integration under that contract: created `feature/session-02-domain-application` from `main`, two commits (`b23c076` implementation, `6c34153` governance), pushed the branch, merged to `main` (`ad8b6c9`, no conflicts), pushed `main`, and verified `origin/main` matches exactly. Working tree is clean on `main`. Full detail in §9C.

Next action is Session 03 — SQL Server LocalDB Persistence.

### 08 August 2026 — Cross-Windows Compatibility governance update complete

Established the permanent V1 Cross-Windows Desktop Compatibility baseline (decision 16, §6) across all governance documents ahead of Session 02, per planner instruction. Minimum OS Windows 10 1809/build 17763, supported OS Windows 10 1809+ and Windows 11, architectures x86/x64/ARM64, x64 primary validation. Also corrected the known Session 01 ARM64 solution-configuration gap in `AIUsageMonitor.sln` (safe, mechanical, build/test-validated — see §9A). No business/domain/provider logic was implemented; Session 02 remains next and was not started.

Branch `docs/cross-windows-compatibility` created from `main`, changes committed, pushed, merged into `main`, and `origin/main` verified. See git log for exact commit.

### 08 August 2026 — Session 01 complete

Repository foundation created and validated end-to-end (restore/build/test/WinUI launch all actually run, not assumed).

Found the working tree already mid-reorganization (governance files moved into `docs/`/`.ai/` but uncommitted, on top of one prior commit on `main`). Completed that reorganization, fixed all stale intra-doc path references, and added the full solution/project structure in the same coherent session scope.

Branch `feature/session-01-repository-foundation` created from `main`, committed, pushed, merged into `main` via fast-forward/merge (no force-push, no history rewrite), and `origin/main` updated. See §2 for exact git state.

Next action is Session 02 — Domain & Application Architecture.
