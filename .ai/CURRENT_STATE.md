# AI Project Orchestrator (APO) - Current State

**Last Updated:** 21 August 2026
**Product:** AI Project Orchestrator (APO)
**Previous Product Identity:** AI Usage Monitor
**Repository:** `https://github.com/Hossam1104/AI-Project-Orchestrator`
**Local Root:** `D:\AI Tools\Hossam\AI-Project-Orchestrator`
**APO-18:** COMPLETE / ACCEPTED
**APO-19:** COMPLETE / SOL ACCEPTED
**APO-20:** COMPLETE — repository and physical local-root rename complete
**APO-22:** IMPLEMENTATION COMPLETE — AWAITING CLAUDE OPUS 5 INDEPENDENT REVIEW / SOL ACCEPTANCE
**Repository/local-folder rename:** COMPLETE; repository and physical local-root rename complete
**Jira Project:** `APO`
**Default Branch:** `main`
**Current Story:** APO-22 - Implement Windows Credential Manager Secure Credential Store
**Current Epic:** APO-2 - Windows Platform & Application Foundation
**Status:** APO-22 implementation delivered on `feat/APO-22-windows-credential-manager`; NOT merged to `main`; NOT marked Done
**Next implementation:** Claude Opus 5 independent review, then GPT-5.6 Sol acceptance; do not execute APO-31 or any other Story automatically
**Release state:** Reusable foundation validated; APO implementation and release qualification not complete

> SINGLE MUTABLE HANDOFF FILE.
> This file is the factual live state and historical validation handoff.
> APO-22 adds a concrete secure-credential-store adapter only. It does not change repository
> identity, provider adapters, or any other Story's scope. APO-20 remains complete; it was not
> rerun by APO-22.

---

## 0. APO-22 Current State & Delivery Summary

APO-22 implemented `WindowsCredentialManagerStore`, the first concrete adapter behind the existing
`ISecureCredentialStore` Application contract (`src/AIUsageMonitor.Application/Security/ISecureCredentialStore.cs`).
The contract signature was NOT changed.

| Item | Result |
|---|---|
| Starting SHA (expected, verified) | `aab184b6e8dcd1aa9f87012bec0845d4070fb410` (`origin/main` matched local `main`) |
| Branch | `feat/APO-22-windows-credential-manager` |
| Windows mechanism | Windows Credential Manager Generic Credential (`CRED_TYPE_GENERIC`), `CRED_PERSIST_LOCAL_MACHINE` persistence |
| Native APIs | `CredWriteW`, `CredReadW`, `CredDeleteW`, `CredFree` (Advapi32.dll), verified against Microsoft Learn `wincred.h` documentation before implementation |
| Secret encoding on the wire | UTF-8 bytes into `CredentialBlob`; decoded back with `Encoding.UTF8` on read |
| Target name | `AIUsageMonitor:Credential:{credentialReference}` (namespaces APO's Generic Credentials in the shared vault) |
| DI registration | `ISecureCredentialStore -> WindowsCredentialManagerStore` (singleton), in `InfrastructureServiceCollectionExtensions.AddInfrastructure` |
| Non-Windows behavior | Throws `PlatformNotSupportedException` before any native call (`OperatingSystem.IsWindows()` guard, injectable for tests) |

### Files changed/added

- `src/AIUsageMonitor.Infrastructure/Security/ICredentialManagerNativeStore.cs` (new, internal) — testable seam over the native calls.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerNativeStore.cs` (new, internal) — P/Invoke implementation.
- `src/AIUsageMonitor.Infrastructure/Security/CredentialManagerNativeException.cs` (new, public) — carries operation, target name, and Win32 error code; never the secret.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerStore.cs` (new, public) — `ISecureCredentialStore` implementation.
- `src/AIUsageMonitor.Infrastructure/AIUsageMonitor.Infrastructure.csproj` (modified) — added `InternalsVisibleTo` for `AIUsageMonitor.Infrastructure.Tests` only, to keep the native seam internal per the execution contract.
- `src/AIUsageMonitor.Infrastructure/InfrastructureServiceCollectionExtensions.cs` (modified) — DI registration.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/FakeCredentialManagerNativeStore.cs` (new) — in-memory native-call fake.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/WindowsCredentialManagerStoreTests.cs` (new) — 14 focused tests.
- `TASK.md` (replaced with the APO-22 contract, now the review checkpoint below).

### Validation evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln` (Debug) | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 64/64 passing (28 Domain, 7 Provider, 29 Infrastructure) — up from the 50/50 baseline (15 prior Infrastructure tests + 14 new) |
| `git diff --check` | Clean (only a benign LF/CRLF normalization notice on `TASK.md`, not a real whitespace defect) |
| `dotnet build` Desktop, `-p:Platform=x64 -r win-x64` (Release) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=x86 -r win-x86` (Release) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=ARM64 -r win-arm64` (Release) | SUCCESS; 0 warnings, 0 errors — **compile-only**; no ARM64 hardware execution performed or claimed |
| Real native round-trip smoke check | Performed manually on this Windows x64 dev machine only (temporary test, added, run, then deleted before commit): write/read-back exact bytes/update/delete/confirm-not-found all succeeded against the actual Windows Credential Manager vault, then the credential and the scratch test file were both removed. `cmdkey /list` confirmed no `AIUsageMonitor` credential remained afterward. This is not part of the committed automated suite (the committed suite uses the fake per the execution contract), so it is not repeatable evidence for the reviewer to rerun — it only records that the native P/Invoke path was exercised once against real Windows Credential Manager during implementation. |
| Secret/diff review | SUCCESS; searched the diff for the test secret literals used in fixtures (e.g. `super-secret-token`, `codex-secret-value`) — they appear only in test files, never in generated output, logs, or persisted JSON/JSONL |

### Known limitations

- Test requirement "non-Windows guard behavior where reasonably testable" is covered by an
  injectable `Func<bool> isWindows` seam exercised in
  `NonWindowsPlatform_FailsTruthfullyWithoutCallingTheNativeStore` — this proves the guard logic
  deterministically, but does not (and cannot, on this Windows-only dev/CI environment) prove
  actual execution under a non-Windows OS.
- ARM64/x86 evidence above is compile-only, performed on an x64 development machine; no ARM64 or
  x86 hardware runtime execution was performed or is claimed.
- `CRED_MAX_CREDENTIAL_BLOB_SIZE` is 2560 bytes (`CRED_PERSIST_LOCAL_MACHINE`); a secret encoded to
  more than 2560 UTF-8 bytes will fail with a `CredentialManagerNativeException` surfacing the
  native Win32 error code. No APO code pre-validates this size; the OS enforces it.
- APO-31 (provider capacity adapters) was explicitly NOT started. No provider connection UX, no
  Codex/Claude/Kimi/Copilot/Antigravity adapter code, no LocalAppData migration, and no
  solution/project/namespace rename were touched.

### Review boundary

APO-22 implementation is delivered on `feat/APO-22-windows-credential-manager` and has **NOT**
been merged to `main` and has **NOT** been marked Done. Claude Opus 5 independent review and
GPT-5.6 Sol acceptance are required next. `main` remains unchanged at
`aab184b6e8dcd1aa9f87012bec0845d4070fb410`.

---

## 1. APO-20 Current State & Identity Rename Summary

APO-20 completed the controlled repository and physical local project-root identity rename. The
active GitHub repository is `Hossam1104/AI-Project-Orchestrator`, and the local root is
`D:\AI Tools\Hossam\AI-Project-Orchestrator`.

| Item | Result |
|---|---|
| Historical old repository | `Hossam1104/AI-Usage-Monitor-Tool` |
| New repository | `Hossam1104/AI-Project-Orchestrator` |
| Historical old local root | `D:\AI Tools\Hossam\AI Usage Monitor Tool` |
| New local root | `D:\AI Tools\Hossam\AI-Project-Orchestrator` |
| Verified starting SHA | `9659bf65bda4defc91b2383cf7f195637678485f` |
| Prior APO-20 implementation merge SHA | `861dc99` |
| Finalization starting SHA | `cba84d57c88d95d93fe360f50ff2e961bdb13168` |
| Origin | `https://github.com/Hossam1104/AI-Project-Orchestrator.git`; verified with fetch |
| Physical local-root rename | COMPLETE; verified from the authoritative hyphenated root |
| Historical first rename attempt | BLOCKED by a Windows process lock; resolved before this finalization |
| Technical rename | Not performed; solution/project/namespace/assembly/test/persistence identifiers remain unchanged |

### APO-20 Validation Evidence

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all projects up to date |
| `dotnet build AIUsageMonitor.sln` | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 50/50 passing (28 Domain, 7 Provider, 15 Infrastructure) |
| `git diff --check` | SUCCESS; clean formatting |
| Technical source scope | SUCCESS; no `src/`, `tests/`, `.csproj`, `.sln`, namespace, assembly, or persistence rename |
| Secret/artifact review | SUCCESS; no credentials or untracked generated artifacts introduced |

The older `ae712335696d827a7a1a2d2464cf667f43430c33` SHA in the APO-19 legacy-map assessment is
retained as historical evidence. The APO-19 accepted final main SHA is
`9659bf65bda4defc91b2383cf7f195637678485f`; the repository state verified for APO-20 began at
that accepted SHA.

---

## 2. APO-19 Current State & Inventory Summary

APO-19 completed the formal repository inventory, code inspection, and architectural reuse classification for the AI Project Orchestrator. All active solution files, Domain entities, Application contracts, Infrastructure persistence components, WPF desktop composition, publish profiles, and historical Git commits were analyzed against `docs/BRD.md` and the 17 approved APO Epics.

The comprehensive durable mapping artifact is delivered at `docs/LEGACY_IMPLEMENTATION_MAP.md`.

### Classification Summary Counts (17 Areas Evaluated)
- **Reuse As-Is (5)**: Dynamic Quota normalization & mathematical invariants (`QuotaWindow`), Subscriptions domain model (`Subscription`), UsageSnapshot model (`UsageSnapshot`), Capacity JSON/JSONL repositories, SystemClock abstraction (`IClock`).
- **Reuse With Extension (6)**: .NET 10 solution & platform foundation, Provider/Connection domain, Alerts & Sync domain, Schema-versioned JSON document persistence with atomic replacement & quarantine, Monthly partitioned JSONL event/history persistence with streaming and unterminated tail resilience, Self-contained multi-architecture publish profiles (`win-x64`, `win-x86`, `win-arm64`).
- **Refactor (3)**: Storage layout & `ApplicationDataPaths` (to add project/run/evidence directories and manage eventual root path migration), Desktop/WPF minimal smoke shell (to evolve into MVVM Command Center under APO-15), Providers project (to implement concrete collection adapters for Codex, Claude, Kimi, Copilot, Antigravity).
- **Superseded (3)**: Historical EF Core 10 + SQL Server LocalDB persistence (Session 03), Historical WinUI 3 / Windows App SDK shell (Session 01/02), Legacy numbered provider roadmap (Sessions 04–20 in `SESSION_PROMPTS.md`).
- **Remove (0)**: Zero dead/unsafe code in current working tree.

### Major Reusable Foundations
- Dynamic quota window normalization and strict mathematical invariants preventing double-inversion bugs and percentage/absolute value contradictions.
- Resilient local file persistence: schema-versioned JSON with atomic replacement, temporary files, in-process synchronization, and corrupt-file quarantine.
- Monthly-partitioned JSONL streaming queries with automatic unterminated-tail detection and newline recovery.
- Storage startup resilience: safe LocalAppData initialization with graceful fallback to degraded no-persistence mode.
- Self-contained single-file publish profiles targeting Windows 10 (build 17763) and Windows 11 on x64, x86, and ARM64.

### Key Refactor Areas
- `ApplicationDataPaths`: Expand directory hierarchy for registered projects, orchestration runs, validation evidence, review findings, and planning contracts.
- Desktop Shell (`MainWindow`): Refactor minimal smoke shell into rich MVVM Command Center with HUD and Tray components.
- Provider Adapters: Implement concrete, verified collection mechanisms for Codex, Claude, Kimi, GitHub Copilot, and Antigravity.

### Major Gaps for Full APO Capabilities
- Project Registry and workspace isolation (APO-5).
- Git and GitHub integration adapters (APO-6).
- Jira and Azure DevOps work-item integration adapters (APO-7).
- AI Agent / Model registry and execution connectivity (APO-8).
- Intelligent quota-aware model routing engine (APO-9).
- Bounded autonomous execution runtime (APO-11).
- Independent validation and evidence capture engine (APO-12).
- Independent review and remediation loop engine (APO-13).
- Acceptance and human approval gate enforcement (APO-14).
- Command center UI, HUD, and system tray (APO-15).
- Activity, audit, and notification event streams (APO-16).
- GitHub Actions CI/CD pipeline and multi-architecture release qualification (APO-17).

### Baseline Validation Results
| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS; all 8 projects restored |
| `dotnet build AIUsageMonitor.sln` | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 50/50 passing (28 Domain, 7 Provider, 15 Infrastructure) |
| `git diff --check` | SUCCESS; clean formatting |
| Active legacy dependency scan | SUCCESS; 0 active references to EF Core, SQL Server, LocalDB, WinUI, Windows App SDK, SQLite, Node, Chromium |
| Source/config scope scan | SUCCESS; no files in `src/`, `tests/`, or `.csproj` modified |
| Secret/artifact review | SUCCESS; no credentials or untracked artifacts introduced |

**NO PRODUCT SOURCE CODE WAS MODIFIED IN APO-19.**

---

## 3. Historical Planner Boundary After APO-19

Before APO-20, GPT-5.6 Sol was required to review the legacy implementation map and proceed with
Jira backlog management:

1. Review `docs/LEGACY_IMPLEMENTATION_MAP.md`.
2. Review the recommended backlog item titles and target Epics, then create and approve the real
   Jira backfill and implementation Stories with Jira-assigned keys.
3. Select the first approved implementation Story and issue its execution contract in `TASK.md`.

That historical boundary is superseded by APO-20's repository-identity work. Do not execute APO-2,
providers, orchestration, routing, Jira/GitHub adapters, UI redesign, or any other future capability
from this checkpoint.

---

## 4. Approved Active Target Architecture

```text
WPF + MVVM
      |
Application contracts/services
      |
Domain

Infrastructure -> JSON/JSONL, secure credentials, logging, OS integration
Providers/integrations -> verified collection and normalization

Release -> self-contained .NET 10 Windows artifacts
Database/ORM -> none in V1
```

The active V1 target is C#/.NET 10, WPF, MVVM, modular clean architecture, System.Text.Json, JSON/JSONL local persistence, secure external credential storage, focused xUnit tests, GitHub Actions, and self-contained Windows artifacts. Windows 10 build-17763/Windows 11 compatibility is the engineering goal with x86/x64/ARM64 consideration and x64 primary validation.

Historical EF Core, SQL Server, LocalDB, WinUI, and Windows App SDK work is not active runtime architecture. It remains historical project evidence only.

---

## 5. Jira Baseline

The approved initial Jira hierarchy is created under project `APO`:

| Key | Capability |
|---|---|
| APO-1 | APO Product Rebrand & Governance Rebaseline |
| APO-2 | Windows Platform & Application Foundation |
| APO-3 | Local Persistence, Resilience & Security Foundation |
| APO-4 | AI Usage, Subscription & Capacity Monitoring |
| APO-5 | Project Registry & Workspace Management |
| APO-6 | Git & GitHub Integration |
| APO-7 | Jira & Azure DevOps Work-Item Integration |
| APO-8 | AI Agent / Model Registry & Connectivity |
| APO-9 | Intelligent Model Routing & Quota-Aware Decisioning |
| APO-10 | Planning & Execution Contracts |
| APO-11 | Autonomous Execution Runtime |
| APO-12 | Validation & Evidence Engine |
| APO-13 | Independent Review & Remediation Engine |
| APO-14 | Acceptance & Human Approval Gates |
| APO-15 | Command Center & Project UX |
| APO-16 | Activity, Audit, History & Notifications |
| APO-17 | Packaging, Compatibility, CI & Release Quality |

Recommended backfill Stories under these Epics are detailed in `docs/LEGACY_IMPLEMENTATION_MAP.md` Section 14.

---

## 6. Current Source and Reusable Foundation

The repository currently contains:

- `AIUsageMonitor.sln` and the existing project structure;
- Domain models and invariants for providers, subscriptions, dynamic quota windows, usage snapshots, alerts, sync, and opaque credential references;
- Application contracts for provider discovery/refresh, repositories, usage aggregation, subscriptions, settings, alert evaluation, time, and secure credentials;
- WPF desktop composition and a resilient empty/no-provider shell with degraded mode;
- Infrastructure JSON state stores and monthly JSONL history/event stores under LocalAppData;
- Schema-aware JSON, atomic replacement, synchronized writes, corruption classification, optional state isolation, last-known-good behavior, and interrupted-tail handling;
- Material-change duplicate suppression and latest/range history queries;
- xUnit Domain, Provider, and Infrastructure tests (50 tests);
- x86/x64/ARM64 target configuration; and
- Self-contained publish profiles for the three Windows RIDs.

The current source uses `AIUsageMonitor` technical names and the minimal foundation shell. Those names and user-facing shell labels are retained during APO-19 because source-code refactoring is explicitly out of scope. They are future mapping/refactor candidates, not active product-governance claims.

---

## 7. Historical Implementation Record

### Session 01 - Repository and Solution Foundation
Completed under the earlier product identity and architecture. Created the solution, project layout, build/editor settings, Git ignore rules, initial README/governance, and a minimal desktop shell.

### Session 02 - Domain and Application Architecture
Completed 08 August 2026. Added provider-independent domain models and application contracts for dynamic quotas, used/remaining normalization, subscriptions, usage snapshots, alerts, sync, provider discovery/refresh, settings, and secure credentials.

### Session 02R - Domain Integrity Remediation
Completed 08 August 2026. Corrected last-known-data retention on provider failure, absolute and percentage quota consistency, subscription confidence, opaque credential references, and related contracts/tests.

### Session 03 - EF Core + SQL Server LocalDB Persistence (Historical / Superseded)
Completed 08 August 2026. Implemented EF Core 10, SQL Server LocalDB, migrations, repositories, duplicate-snapshot suppression, LocalDB initialization, and real LocalDB integration tests. Superseded in Session 03R by the zero-prerequisite consumer requirement.

### Session 03R - Portable Consumer Desktop Architecture Migration
Completed 08 August 2026. Converted desktop foundation to WPF, implemented JSON/JSONL file persistence, retained provider-independent contracts, and established self-contained publish profiles.

### Session 03R-F - Portable Foundation Resilience Remediation
Completed 08 August 2026. Guarded LocalAppData storage initialization, added degraded no-persistence startup behavior, optimized newest-partition latest lookup, and added unterminated JSONL tail handling.

### APO-18 - Governance Rebaseline & Consolidated BRD
Completed 21 August 2026. Consolidated `docs/BRD.md` as the single authoritative BRD, updated repository governance for Jira project `APO`, and established the six-role AI operating model.

### APO-19 - Legacy Implementation Inventory & APO Reuse Map
Completed 21 August 2026. Conducted complete code inspection, categorized all components into 5 reuse classifications, documented in `docs/LEGACY_IMPLEMENTATION_MAP.md`, and formulated title- and target-Epic-based Jira backfill recommendations. Jira keys remain for Sol/Jira to assign.

---

## 8. Historical Architecture Decisions Retained

- Portable WPF/JSON/JSONL foundation is the approved active architecture.
- Historical EF/LocalDB and WinUI implementations remain recorded as completed historical milestones.
- Dynamic quota windows and mathematical normalization invariants are preserved.
- Used/remaining contradictions are rejected at construction.
- Last-known valid provider data is retained on refresh failure and marked stale/error.
- Opaque credential references are stored in JSON; raw secrets remain in Windows Credential Manager.
- JSON/JSONL stores enforce schema metadata, atomic temporary-file replacement, in-process synchronization, duplicate suppression, streaming queries, and corruption quarantine.
- Cross-Windows compatibility (Windows 10 1809+ / Windows 11) is a permanent contract.
- APO-20 renamed the active repository and local project folder identities; technical solution,
  project, namespace, assembly, test, and persistence identifiers remain unchanged.
- APO-22 implements the first concrete `ISecureCredentialStore` adapter: Windows Credential
  Manager Generic Credentials, `CRED_PERSIST_LOCAL_MACHINE` persistence, secrets never written to
  JSON/JSONL/logs.

---

## 9. Delivery Record

| Story | Branch | Implementation Commit | Merge Commit | Delivery Date | Status |
|---|---|---|---|---|---|
| APO-18 | `refactor/APO-18-product-governance-rebaseline` | `7d70dae` | `56f4ea7` | 21 Aug 2026 | COMPLETE |
| APO-19 | `docs/APO-19-legacy-implementation-map` | Pending | Pending | 21 Aug 2026 | COMPLETE |
| APO-20 | `docs/APO-20-finalize-local-root` | `138c51f` | `138c51f` (fast-forward) | 21 Aug 2026 | COMPLETE |
| APO-22 | `feat/APO-22-windows-credential-manager` | Pending | Not merged | 21 Aug 2026 | AWAITING OPUS REVIEW / SOL ACCEPTANCE |

---

## 10. Next Planner Boundary

APO-20 repository and physical local-root rename work is complete. APO-22 delivered the Windows
Credential Manager secure credential store adapter on `feat/APO-22-windows-credential-manager` and
is now the active review checkpoint in `TASK.md`. APO-19 remains complete and Sol-accepted and has
not been rerun.

Claude Opus 5 must independently review APO-22 (implementation, tests, native-memory safety,
credential-reference validation, non-Windows guard, DI wiring, and this file's evidence), then
GPT-5.6 Sol must accept or reject it. Only after acceptance may Sol select and issue the next
execution contract (e.g. APO-31, which depends on APO-22). Do not execute any Story automatically
from this checkpoint.
