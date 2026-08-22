# AI Project Orchestrator (APO) - Current State

**Last Updated:** 22 August 2026
**Product:** AI Project Orchestrator (APO)
**Previous Product Identity:** AI Usage Monitor
**Repository:** `https://github.com/Hossam1104/AI-Project-Orchestrator`
**Local Root:** `D:\AI Tools\Hossam\AI-Project-Orchestrator`
**APO-18:** COMPLETE / ACCEPTED
**APO-19:** COMPLETE / SOL ACCEPTED
**APO-20:** COMPLETE — repository and physical local-root rename complete
**APO-22:** COMPLETE — Sol-accepted PR #1 merged into main
**APO-23:** IMPLEMENTATION COMPLETE — AWAITING GPT-5.6 SOL ACCEPTANCE
**Repository/local-folder rename:** COMPLETE; repository and physical local-root rename complete
**Jira Project:** `APO`
**Default Branch:** `main`
**Current Story:** APO-23 - Refactor Application Identity and Technical Naming for APO
**Current Epic:** APO-2 - Windows Platform & Application Foundation
**Status:** APO-23 branding implementation delivered on `feat/APO-23-branding`; Draft PR pending final delivery metadata; not merged to `main`
**Next implementation:** GPT-5.6 Sol acceptance of APO-23; do not execute APO-24, APO-27, APO-31, APO-33, or any other Story automatically
**Release state:** Reusable foundation and branded WPF shell validated; APO implementation and release qualification are not complete

> SINGLE MUTABLE HANDOFF FILE.
> This file is the factual live state and historical validation handoff.
> APO-22 adds the concrete secure-credential-store adapter and is now merged at main SHA
> `f6f04dca89313c9964add3c403a151a7b8b6919e`. APO-23 establishes product identity and a
> branded WPF foundation surface only. Technical identifiers, persistence identity, provider
> adapters, and orchestration runtime remain outside this Story.

---

## -2. APO-23 Branding & WPF Product Identity (Current Delivery)

**Starting main SHA:** `f6f04dca89313c9964add3c403a151a7b8b6919e` (APO-22 squash merge)

**Branch:** `feat/APO-23-branding`, rebased from the owner-approved asset commit lineage
`74fc81eb3b11e7a6c1acf7641acbd83ffc952e23`.

**Approved assets and preservation:**

- `assets/Logo.png` — authoritative logo; original bytes preserved.
- `assets/Colors.png` — authoritative palette/usage reference; original bytes preserved.
- `assets/runtime/apo-icon.ico` — derived compact symbol for Windows application/window surfaces.
- `assets/readme/apo-flow.svg` — repository-controlled, lightweight target-flow animation.
- `assets/readme/apo-shell.png` — clean screenshot from the self-contained x64 WPF shell.

**WPF theme/resource structure:**

- `src/AIUsageMonitor.Desktop/Resources/Colors.xaml` — approved palette tokens.
- `src/AIUsageMonitor.Desktop/Resources/Brushes.xaml` — surfaces, accents, states, and gradients.
- `src/AIUsageMonitor.Desktop/Resources/Typography.xaml` — Windows-native display/body typography.
- `src/AIUsageMonitor.Desktop/Resources/Controls.xaml` — cards, buttons, navigation, chips, and focus states.

**Visible product identity changes:**

- Main window title and taskbar/window icon now identify **AI Project Orchestrator**.
- Shell header, welcome surface, README hero, footer, and product metadata use **AI Project Orchestrator (APO)**.
- Startup/shutdown log messages use the APO product identity; the existing log filename remains
  compatibility-sensitive under the preserved `AIUsageMonitor` storage identity.
- `AIUsageMonitor.sln`, project/directories, namespaces, assembly names, test names, and
  `%LOCALAPPDATA%\AIUsageMonitor` remain deliberately unchanged.

**README redesign:**

- Branded hero with approved local logo and factual .NET/WPF/Windows/C# badges.
- Target orchestration flow animation and Mermaid target-flow/architecture diagrams.
- Current status, architecture, operating model, security, compatibility, build/test, roadmap,
  governance, repository tree, documentation links, identity mapping, and screenshot preview.
- No fake CI/build badges, provider claims, routing claims, or completion percentages.

**Validation evidence:**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln` | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; 85/85 passing (28 Domain, 7 Provider, 50 Infrastructure) |
| `dotnet publish` `win-x64` | SUCCESS; self-contained single-file publish |
| `dotnet publish` `win-x86` | SUCCESS; self-contained single-file publish; compile/publish evidence only |
| `dotnet publish` `win-arm64` | SUCCESS; self-contained single-file publish; compile/publish evidence only |
| Self-contained WPF launch smoke | SUCCESS on this Windows x64 development machine; title, icon, logo, resources, and shell rendered |
| README local-link check | SUCCESS; 10 local paths checked, none missing |
| README Mermaid fence check | SUCCESS; 2 Mermaid blocks present |
| Approved asset hash check | SUCCESS; `Logo.png` and `Colors.png` match the approved asset commit |
| `git diff --check` | SUCCESS; only expected LF/CRLF normalization notices |

**Scope and limitations:** No provider adapters, routing engine, autonomous runtime, project
registry backend, tracker runtime, full APO-15 dashboard, LocalAppData migration, technical
namespace rename, updater/installer, cloud backend, or CI implementation was started. x86 and
ARM64 were publish-validated from the x64 development machine; no corresponding hardware runtime
execution is claimed. No independent Opus review is required for this Story unless Sol identifies
a new critical issue.

---

## -1. APO-22 Remediation (Opus Review 1 -> Sol Decisions -> Bounded Fix)

Claude Opus 5 performed independent Review 1 against implementation head `de2290a0fb070dd59f8a4c76248a140e019fd35d`.

**Opus Review 1 verdict:** CHANGES REQUIRED (architecture sound; native interop verified correct;
secret handling substantially sound). 1 MAJOR + 7 MINOR findings.

**Sol decisions (final for APO-22):**

- `credentialReference` is a **case-insensitive** identifier. Canonical Windows target casing =
  `credentialReference.ToUpperInvariant()` (invariant, not current-culture; no trimming beyond the
  approved case normalization).
- Permanent APO V1 Windows Credential Manager namespace = `AIProjectOrchestrator:Credential:`
  (replacing the legacy `AIUsageMonitor:Credential:` prefix). No production/provider credentials
  exist yet and APO-31 has not started, so **no migration, dual-read, or old-prefix handling was
  implemented** — none was required.

**Remediation commit:** applied on `feat/APO-22-windows-credential-manager` (see Section 9 Delivery
Record for the exact SHA once pushed).

**Findings addressed:**

| Finding | Resolution |
|---|---|
| MAJOR-1 (case identity undefined) | `WindowsCredentialManagerStore.BuildTargetName` now canonicalizes with `ToUpperInvariant()`; new namespace `AIProjectOrchestrator:Credential:`; case-identity tests added (store/retrieve/remove across differing casings; exact-target-name assertion). |
| MINOR (fake store used ordinal, not case-insensitive, comparer) | `FakeCredentialManagerNativeStore` dictionary now uses `StringComparer.OrdinalIgnoreCase`, matching real Windows Generic Credential TargetName identity. |
| MINOR (unsupported claim that `CRED_PERSIST_ENTERPRISE` has a lower blob-size limit) | Comment in `WindowsCredentialManagerNativeStore` corrected to state only supported, documented behavior (durable across logons, per-user, per-machine, non-roaming); the false blob-size claim was removed. |
| MINOR (native read blob not zeroed before `CredFree`) | `TryRead` now zeroes the native `CredentialBlob` bytes in a `finally` block, immediately before `CredFree`, with `CredFree` still called exactly once and no use-after-free/double-free/leak. |
| MINOR (no deterministic secret-size validation) | `StoreAsync` now validates the UTF-8-encoded secret against the 2560-byte Generic Credential blob limit (`MaxCredentialBlobSizeBytes`) **before** calling the native store, throwing `ArgumentException` (param `secret`, no secret content) without an unmanaged allocation for oversized input; the managed `secretBytes` are still zeroed in `finally`. |
| MINOR (non-asserting file test) | `StoreAsync_DoesNotWriteTheSecretToAnyFileUnderApplicationStorage` now materializes the enumerated file list with `.ToArray()` and asserts it is empty, instead of a `foreach` that could execute zero assertions. |
| MINOR (thin high-value contract coverage) | Added case-identity, exact-target-name, native read/delete failure, retrieve/remove validation, retrieve/remove cancellation, empty-secret, Unicode-secret, and exact/over the 2560-byte boundary tests. |
| Documentation (opaque/case-insensitive contract) | `ISecureCredentialStore` and `ProviderConnection.CredentialReference` XML docs now state the reference is opaque, case-insensitive, and never contains the secret. Method signatures and the storage location of `CredentialReference` were not changed. |

**Corrected blob-size documentation:** the Windows Generic Credential blob limit is
`CRED_MAX_CREDENTIAL_BLOB_SIZE = 5 * 512 = 2560` bytes. This is a property of the Generic Credential
type itself, not of `CRED_PERSIST_LOCAL_MACHINE` specifically — the prior note incorrectly implied a
persistence-mode-specific limit. APO now pre-validates this size at the `WindowsCredentialManagerStore`
boundary before any native call.

**Validation evidence (remediation):**

| Check | Result |
|---|---|
| `dotnet restore AIUsageMonitor.sln` | SUCCESS |
| `dotnet build AIUsageMonitor.sln` (Debug) | SUCCESS; 0 warnings, 0 errors |
| `dotnet test AIUsageMonitor.sln` | SUCCESS; **85/85** passing (28 Domain, 7 Provider, 50 Infrastructure) — up from the 64/64 Review-1 baseline (29 prior Infrastructure security/other tests + 21 new case-identity/validation/cancellation/unicode/oversize tests) |
| `git diff --check` | Clean (only the same benign LF/CRLF normalization notice seen in the Review-1 baseline) |
| `dotnet build` Desktop, `-p:Platform=x64 -r win-x64` (Release, self-contained) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=x86 -r win-x86` (Release, self-contained) | SUCCESS; 0 warnings, 0 errors — **compile-only** |
| `dotnet build` Desktop, `-p:Platform=ARM64 -r win-arm64` (Release, self-contained) | SUCCESS; 0 warnings, 0 errors — **compile-only**; no ARM64 hardware execution performed or claimed |
| Secret/diff review | SUCCESS; diff limited to the 6 files listed below; no secret literals outside test fixtures; no assets/README/UI touched |

**Files changed in remediation:**

- `src/AIUsageMonitor.Application/Security/ISecureCredentialStore.cs` — doc-only.
- `src/AIUsageMonitor.Domain/Providers/ProviderConnection.cs` — doc-only.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerStore.cs` — new namespace, `ToUpperInvariant()` canonicalization, deterministic oversize validation.
- `src/AIUsageMonitor.Infrastructure/Security/WindowsCredentialManagerNativeStore.cs` — corrected persistence comment, read-blob zeroization before `CredFree`.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/FakeCredentialManagerNativeStore.cs` — `StringComparer.OrdinalIgnoreCase`.
- `tests/AIUsageMonitor.Infrastructure.Tests/Security/WindowsCredentialManagerStoreTests.cs` — fixed 2 tests, added 21 new test cases.

**Explicitly out of scope / not touched:** APO-31 provider adapters, `assets/` branding, `README.md`,
WPF UI, LocalAppData migration, solution/project/namespace rename, old-prefix migration/dual-read/
enumeration/cleanup (none required — no production credentials exist under the legacy prefix).

**Review boundary:** Claude Opus 5 must perform independent Review 2 against the remediated head.
GPT-5.6 Sol must accept the Story before merge or before APO-31 may begin.

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
| Target name | `AIProjectOrchestrator:Credential:{credentialReference.ToUpperInvariant()}` (namespaces APO's Generic Credentials in the shared vault; case-insensitive since APO-22 remediation — see Section -1) |
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
- `CRED_MAX_CREDENTIAL_BLOB_SIZE` is 2560 bytes, a property of the Generic Credential type itself
  (not specific to `CRED_PERSIST_LOCAL_MACHINE`). Since the APO-22 remediation (Section -1),
  `WindowsCredentialManagerStore.StoreAsync` pre-validates the UTF-8-encoded secret against this
  limit and throws `ArgumentException` before any native call, instead of relying on a native
  Win32/RPC failure.
- APO-31 (provider capacity adapters) was explicitly NOT started. No provider connection UX, no
  Codex/Claude/Kimi/Copilot/Antigravity adapter code, no LocalAppData migration, and no
  solution/project/namespace rename were touched.

### Review boundary

This section records the original APO-22 implementation as reviewed in Opus Review 1. Claude Opus 5
returned CHANGES REQUIRED against that state; the bounded remediation applied on top of it is
recorded in Section -1. The Sol-accepted APO-22 head was verified unchanged, PR #1 was marked
ready, and it was squash-merged into `main` at `f6f04dca89313c9964add3c403a151a7b8b6919e`.
`main` remains unchanged after the APO-23 branch work.

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
- xUnit Domain, Provider, and Infrastructure tests (85 tests; 28 Domain, 7 Provider, 50 Infrastructure);
- x86/x64/ARM64 target configuration; and
- Self-contained publish profiles for the three Windows RIDs.

The current source uses `AIUsageMonitor` technical names while the WPF surface now presents the
AI Project Orchestrator identity through the branded foundation shell. Technical names remain
future mapping/refactor candidates; they are not user-facing product-governance claims.

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
- APO-22 remediation (Section -1): `credentialReference` is case-insensitive; canonical Windows
  target casing is `ToUpperInvariant()`; the permanent vault namespace is
  `AIProjectOrchestrator:Credential:`.

---

## 9. Delivery Record

| Story | Branch | Implementation Commit | Merge Commit | Delivery Date | Status |
|---|---|---|---|---|---|
| APO-18 | `refactor/APO-18-product-governance-rebaseline` | `7d70dae` | `56f4ea7` | 21 Aug 2026 | COMPLETE |
| APO-19 | `docs/APO-19-legacy-implementation-map` | Pending | Pending | 21 Aug 2026 | COMPLETE |
| APO-20 | `docs/APO-20-finalize-local-root` | `138c51f` | `138c51f` (fast-forward) | 21 Aug 2026 | COMPLETE |
| APO-22 | `feat/APO-22-windows-credential-manager` | `de2290a`, `55e1b74`, `b1b97de` | `f6f04dc` (PR #1 squash) | 22 Aug 2026 | COMPLETE / SOL-ACCEPTED |
| APO-23 | `feat/APO-23-branding` | Pending final delivery commit | Not merged; Draft PR pending | 22 Aug 2026 | COMPLETE IMPLEMENTATION / AWAITING SOL ACCEPTANCE |

---

## 10. Next Planner Boundary

APO-20 repository and physical local-root rename work is complete. APO-22 was accepted and merged
through PR #1 at `f6f04dca89313c9964add3c403a151a7b8b6919e`. APO-23 implemented the approved
branding assets, WPF resources, product identity, shell polish, README redesign, and validation
evidence on `feat/APO-23-branding`. The final branch commit, push, and Draft PR metadata must be
recorded here before the handoff is complete.

The exact next authority is GPT-5.6 Sol acceptance of APO-23. Do not execute APO-24, APO-27,
APO-31, APO-33, or any other Story automatically from this checkpoint.
