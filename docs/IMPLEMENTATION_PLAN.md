# AI Usage Monitor — Implementation Plan

**Version:** 1.0  
**Date:** 08 August 2026  
**Local Project Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**GitHub Repository:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**Default Branch:** `main`  
**Primary Requirements:** `docs/BRD v1.0.md`  
**Executors:** Terra / Luna / Sonnet  
**Reviewer:** Opus 5  
**Planner:** GPT-5.6 Sol  

---

## 1. Purpose

This file is the authoritative implementation sequence for AI Usage Monitor.

The project is a personal Windows desktop utility. It is not a SaaS product and does not require Jira.

The repository itself must contain enough context for any supported AI model to continue the project without depending on previous chat history.

Source-of-truth files:

1. `docs/BRD v1.0.md` — product/business requirements.
2. `AGENTS.md` — universal AI execution contract.
3. `.ai/CURRENT_STATE.md` — single mutable handoff/status file.
4. `docs/IMPLEMENTATION_PLAN.md` — implementation sequence and gates.
5. `docs/SESSION_PROMPTS.md` — exact prompts used to execute each session.
6. `CLAUDE.md` — Claude/Sonnet/Opus adapter instructions.

---

## 2. Frozen V1 Scope

### Approved technology stack

| Layer | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 10 LTS |
| Desktop UI | WinUI 3 |
| Windows framework | Windows App SDK |
| UI pattern | MVVM |
| Architecture | Modular Clean Architecture |
| ORM | EF Core 10 |
| Database | Microsoft SQL Server LocalDB |
| SQL driver | Microsoft.Data.SqlClient |
| HTTP | HttpClient |
| Resilience | Microsoft.Extensions.Http.Resilience |
| DI | Microsoft.Extensions.DependencyInjection |
| Logging | Serilog |
| Secrets | Windows Credential Manager / DPAPI where justified |
| Tests | xUnit, targeted only |
| Source control | Git + GitHub |
| CI | GitHub Actions |
| Packaging | Windows packaged release / MSIX where appropriate |

### Initial providers

- OpenAI Codex
- Anthropic Claude
- Kimi / Kimi Code
- GitHub Copilot
- Google Antigravity

### Explicit V1 exclusions

- Jira
- Angular
- web dashboard
- application-owned cloud backend
- multi-user/team management
- prompt/chat execution
- automatic model switching
- browser-cookie extraction
- password extraction
- telemetry
- commercial billing/payment functionality

---

## 3. Authority Order

If instructions conflict, follow:

1. `docs/BRD v1.0.md`
2. `AGENTS.md`
3. approved decisions/current facts in `.ai/CURRENT_STATE.md`
4. `docs/IMPLEMENTATION_PLAN.md`
5. assigned section of `docs/SESSION_PROMPTS.md`
6. current implementation
7. executor preference

No AI model may silently override a higher authority.

---

## 4. Repository Layout

Target:

```text
AI Usage Monitor Tool/
│
├── AGENTS.md
├── CLAUDE.md
├── README.md
├── AIUsageMonitor.sln
├── Directory.Build.props
├── .editorconfig
├── .gitignore
│
├── docs/
│   ├── BRD v1.0.md
│   ├── IMPLEMENTATION_PLAN.md
│   └── SESSION_PROMPTS.md
│
├── .ai/
│   └── CURRENT_STATE.md
│
├── src/
│   ├── AIUsageMonitor.Desktop/
│   ├── AIUsageMonitor.Domain/
│   ├── AIUsageMonitor.Application/
│   ├── AIUsageMonitor.Infrastructure/
│   └── AIUsageMonitor.Providers/
│
├── tests/
│   ├── AIUsageMonitor.Domain.Tests/
│   └── AIUsageMonitor.Provider.Tests/
│
└── .github/
    └── workflows/
```

Do not create numerous handoff/status Markdown files.  
`.ai/CURRENT_STATE.md` is intentionally the single mutable project status.

---

## 5. Architecture

### Dependency direction

```text
AIUsageMonitor.Desktop
          ↓
AIUsageMonitor.Application
          ↓
AIUsageMonitor.Domain

AIUsageMonitor.Infrastructure ──→ Application/Domain contracts
AIUsageMonitor.Providers      ──→ Application/Domain contracts
```

### Domain rules

The Domain project must not depend on:

- WinUI
- EF Core
- SQL Server
- HttpClient
- provider-specific SDKs
- Windows UI APIs

### Desktop rules

Desktop/UI must never:

- parse provider-specific JSON
- parse CLI output
- inspect provider session files
- know provider token formats
- call undocumented provider endpoints directly

### Provider rules

Each provider owns:

- installation detection
- authentication-state detection
- safe collection
- provider-specific parsing
- normalization
- provider-specific error translation

Everything outside provider modules consumes normalized models.

---

## 6. Dynamic Quota Model

The entire architecture must support arbitrary quota windows.

Never design fixed fields such as:

```text
CodexFiveHourUsage
ClaudeWeeklyUsage
KimiMonthlyUsage
```

Use:

```text
Provider
  └─ QuotaDefinition
       └─ QuotaSnapshot/QuotaWindow
            ├─ Type
            ├─ Unit
            ├─ UsedValue
            ├─ RemainingValue
            ├─ LimitValue
            ├─ UsedPercentage
            ├─ RemainingPercentage
            ├─ WindowStart
            ├─ ResetAt
            ├─ Source
            └─ Confidence
```

The primary dashboard convention is **remaining capacity**.

---

## 7. Provider Truth Rule

Provider integrations are expected to change over time.

Never invent an API, quota, reset timestamp, plan, subscription date, or local-file schema.

Collection priority:

1. official provider API
2. official OAuth/device authorization
3. official CLI/status command
4. verified safe local metadata
5. verified read-only provider endpoint
6. manual fallback

Allowed UI states:

- Connected
- Local Detected
- Partial
- Authentication Required
- Not Available
- Unsupported
- Stale
- Rate Limited
- Error
- Disabled

Truthful partial data is acceptable. Fake completeness is not.

---

## 8. Security Baseline

Never:

- save passwords
- copy browser cookies
- log access tokens
- commit tokens
- store tokens in LocalDB
- store secrets in appsettings
- commit raw authenticated payloads
- transmit prompt/source-code content

Use:

- official OAuth/device flows
- Windows Credential Manager
- DPAPI only when technically justified
- redacted logs
- sanitized test fixtures

---

## 9. Git Workflow

Repository:

`https://github.com/Hossam1104/AI-Usage-Monitor-Tool`

Default branch:

`main`

Recommended session branch pattern:

```text
feature/session-01-foundation
feature/session-02-domain
feature/session-03-persistence
feature/session-04-provider-feasibility
...
```

Rules:

- inspect `git status` before each session
- preserve unrelated user changes
- never `git reset --hard` or destructive-clean user files
- one execution session = one coherent scope
- commit only session-related work
- never commit credentials
- review final diff before completion

Suggested commit prefixes:

- `feat:`
- `fix:`
- `refactor:`
- `docs:`
- `chore:`

---

# 10. Master Delivery Sequence

```text
PHASE 0 — FOUNDATION
01 Repository & solution foundation
02 Domain/application architecture
03 EF Core + SQL Server LocalDB
04 Provider feasibility investigation
   ↓
REVIEW GATE A — OPUS 5

PHASE 1 — EXPERIENCE
05 WinUI design system
06 Main dashboard
07 System tray + Focus HUD

PHASE 2 — PROVIDERS
08 Codex
09 Claude
10 Kimi
11 GitHub Copilot
12 Antigravity
   ↓
REVIEW GATE B — OPUS 5

PHASE 3 — INTELLIGENCE
13 Subscription management
14 History & analytics
15 Capacity recommendation engine
16 Monitoring & notifications

PHASE 4 — HARDENING
17 Settings, security & resilience
18 UX & performance polish
   ↓
REVIEW GATE C — OPUS 5
19 Packaging, CI & release engineering
20 Final stabilization
   ↓
FINAL RELEASE REVIEW — OPUS 5
```

Sessions execute sequentially unless the planner explicitly changes the plan.

---

# 11. Phase 0 — Foundation

## Session 01 — Repository & Solution Foundation

### Objective
Create the clean .NET 10/WinUI 3 project structure and validate the toolchain.

### Deliverables
- local Git repository connected to `https://github.com/Hossam1104/AI-Usage-Monitor-Tool`
- solution file
- WinUI desktop project
- Domain class library
- Application class library
- Infrastructure class library
- Providers class library
- Domain test project
- Provider test project
- correct project references
- central build props
- `.editorconfig`
- `.gitignore`
- concise README
- minimal DI/logging bootstrapping
- CURRENT_STATE synchronized to actual repo

### Exit
- restore succeeds
- solution builds
- shell launches where environment supports it
- no business feature is prematurely implemented

---

## Session 02 — Domain & Application Architecture

### Objective
Implement provider-independent domain models and contracts.

### Core concepts
- Provider
- ProviderAccount
- ProviderConnection
- Subscription
- QuotaDefinition
- QuotaWindow/current quota state
- UsageSnapshot
- AlertRule
- AlertEvent
- SyncEvent
- DataSource
- ConfidenceLevel
- BillingCadence
- provider status
- quota type/unit

### Services/contracts
- AI provider abstraction
- provider registry/discovery
- refresh orchestration
- aggregation
- snapshot persistence
- subscription service
- alert evaluation
- settings abstraction
- secure credential abstraction
- clock abstraction if useful

### Exit
- no fixed quota schema
- important invariants covered by targeted tests
- project boundaries remain correct

---

## Session 03 — Persistence

### Objective
Implement EF Core 10 + Microsoft SQL Server LocalDB.

### Required persistence
- Providers
- ProviderConnections
- Subscriptions
- QuotaDefinitions
- UsageSnapshots
- AlertRules
- AlertEvents
- SyncEvents
- Settings

### Requirements
- migrations
- upgrade-safe initialization
- useful indexes
- duplicate snapshot prevention
- no secrets
- graceful LocalDB missing/unavailable handling

---

## Session 04 — Provider Feasibility Investigation

### Objective
Verify real collection possibilities against the actual local/authenticated environment before provider coding.

### For all five providers determine
- installation detection
- auth/account detection
- official API
- OAuth/device auth
- official CLI/status/usage command
- safe local metadata
- actual quota windows
- source semantic: used vs remaining
- reset timestamp + timezone
- plan
- subscription/billing dates
- rate limits
- network requirement
- fallback

### Capability status
Every field is classified:

- `VERIFIED`
- `NOT AVAILABLE`
- `MANUAL FALLBACK`
- `FURTHER INVESTIGATION`

### Exit
Capability matrix and evidence are saved in `.ai/CURRENT_STATE.md`.

No speculative provider coding.

---

# 12. Review Gate A — Opus 5

Review Sessions 01–04.

Focus:

- architecture
- dependencies
- dynamic quota domain
- LocalDB design
- provider abstraction
- Session 04 evidence
- security
- complexity
- future maintainability

Gate verdict:

- `APPROVED TO CONTINUE`
- `REJECTED — BLOCKERS LISTED`

BLOCKER/HIGH findings must be resolved before Session 05.

---

# 13. Phase 1 — Experience

## Session 05 — WinUI Design System

Build:

- navigation shell
- Mica
- dark/light/system themes
- typography system
- spacing
- card surfaces
- provider identity controls
- quota bars/rings
- status pills
- loading skeleton
- empty/stale/error states
- warning/critical states
- keyboard/accessibility states

Visual direction:

**modern developer command center**

Polished but restrained.

---

## Session 06 — Main Dashboard

Build:

- overall capacity
- best-capacity provider
- next reset
- dynamic provider cards
- arbitrary quota rows
- exact + relative reset
- status/stale state
- refresh all
- refresh progress
- insight panel
- intentional credit-only layout

No provider-specific parsing in UI.

---

## Session 07 — System Tray & Focus HUD

Build:

- tray icon/menu
- open dashboard
- Focus HUD toggle
- refresh
- pause/resume monitoring hook
- settings
- explicit exit
- close/minimize to tray
- compact HUD
- expanded HUD
- always-on-top
- position persistence
- size/mode persistence
- optional opacity
- provider click-through

The HUD must remain comfortable beside VS Code/another IDE.

---

# 14. Phase 2 — Provider Integrations

All Sessions 08–12 consume verified Session 04 findings.

If provider behavior changed, re-verify before implementation.

## Session 08 — Codex
Implement only verified detection/auth/usage/quota/reset/plan/subscription sources and manual fallbacks.

## Session 09 — Claude
Implement verified session/5h, weekly, model-specific, credits/extra-usage and metadata where exposed.

## Session 10 — Kimi
Implement verified rolling 5h, weekly, monthly membership credits, Extra Usage, plan and reset data.

## Session 11 — GitHub Copilot
Implement supported GitHub identity/auth and current AI-credit/billing usage. Do not invent 5h/weekly windows.

## Session 12 — Antigravity
Implement safe verified quota/account integration. Partial/manual fallback is acceptable if programmatic access is unreliable.

### Common provider requirements
- detector
- collector
- parser
- normalizer
- structured failures
- stale/auth-expired handling
- persistence
- dashboard/HUD integration
- sanitized fixtures
- focused parser/normalization tests
- manual comparison with provider's own usage surface

---

# 15. Review Gate B — Opus 5

Audit all provider implementations for:

- invented endpoints
- brittle scraping
- cookie extraction
- token leakage
- wrong quota interpretation
- used/remaining inversion
- timezone/reset bugs
- fabricated subscription data
- stale data displayed as fresh
- last-known values replaced by zero
- duplicate snapshots
- aggressive polling
- UI/provider coupling

BLOCKER/HIGH findings must be resolved before Phase 3.

---

# 16. Phase 3 — Product Intelligence

## Session 13 — Subscription Management

Support:

- plan
- original start
- billing-period start
- billing-period end
- renewal date
- cancellation/paid-through
- auto-renew
- price
- currency
- billing cadence
- source/confidence
- manual fallback

Never present inferred dates as provider facts.

---

## Session 14 — History & Analytics

Implement:

- 24h
- 7d
- 30d
- billing-cycle history
- remaining-capacity trend
- consumption delta
- burn rate
- reset detection/markers
- gaps/stale periods
- estimated exhaustion
- estimated remaining at reset

Do not forecast from insufficient data.

---

## Session 15 — Capacity Recommendation Engine

Deterministic capacity score only.

Inputs:

- current remaining
- short-window remaining
- weekly remaining
- monthly/credits remaining
- reset proximity
- burn rate
- provider health
- stale confidence
- exhaustion

Outputs:

- score
- ranking
- explanation
- long-session recommendation
- constrained-provider warning

No LLM call.  
No claim about intelligence/model quality.

---

## Session 16 — Monitoring & Notifications

Implement:

- periodic refresh (~60 sec default)
- startup refresh
- resume refresh
- foreground refresh where sensible
- safe local event refresh
- timeout/cancellation
- provider isolation
- retry/backoff/rate-limit handling

Alerts:

- warning under 30%
- critical under 15%
- exhausted
- reset/restored
- authentication failure
- stale provider
- renewal approaching

Implement notification deduplication/cooldown.

---

# 17. Phase 4 — Production Hardening

## Session 17 — Settings, Security & Resilience

Settings:

- provider enable/disable
- refresh interval
- Windows startup
- theme
- Focus HUD
- thresholds
- provider setup
- manual subscriptions

Resilience:

- offline
- LocalDB missing
- DB transient error
- provider timeout
- rate limit
- malformed payload
- provider removed
- auth expired
- partial data

Security:

- Windows Credential Manager
- DPAPI only where necessary
- log redaction
- credential disconnect/delete
- no secrets in DB/config

---

## Session 18 — UX & Performance Polish

No new product scope.

Review:

- information hierarchy
- card consistency
- quota wording
- reset display
- resizing
- DPI
- text scaling
- keyboard
- accessibility
- themes
- loading/stale/error
- HUD
- tray behavior
- UI-thread blocking
- DB/network chatter
- chart history performance
- cancellation/disposal

---

# 18. Review Gate C — Opus 5

Pre-release audit:

- BRD coverage
- security
- provider correctness
- architecture
- UI/UX
- accessibility
- performance
- resilience
- history math
- recommendation logic
- monitoring
- notifications

Resolve release blockers before Session 19.

---

# 19. Session 19 — Packaging, CI & Release Engineering

Build:

- Release configuration
- GitHub Actions restore/build/test
- no live provider secrets
- versioning
- Windows packaging
- LocalDB prerequisite strategy
- safe migration on upgrade
- release artifact
- concise README install/update instructions

No cloud infrastructure.

---

# 20. Session 20 — Final Stabilization

No new features.

Validate full V1 against BRD:

- fresh launch
- existing database
- missing LocalDB
- provider connected
- provider disabled
- provider partial
- provider stale
- auth expired
- offline
- quota change
- quota reset
- subscription manual entry
- dashboard
- provider details
- history
- recommendation
- alerts
- notifications
- HUD
- tray
- restart
- Windows startup
- packaged installation/update

Resolve all BLOCKER/HIGH defects.

Set `.ai/CURRENT_STATE.md` to `RELEASE CANDIDATE`.

---

# 21. Final Opus 5 Release Review

Every BRD requirement:

- PASS
- PARTIAL
- FAIL
- NOT APPLICABLE

Final verdict exactly:

`READY FOR PERSONAL PRODUCTION USE`

or:

`NOT READY FOR PERSONAL PRODUCTION USE`

No conditional approval while blockers remain.

---

# 22. Definition of Done — Every Executor Session

A session is Done only when:

1. assigned scope is implemented
2. unrelated scope was not added
3. solution builds
4. targeted validation/tests actually run
5. no session-created compiler errors remain
6. no secrets are present
7. final Git diff was reviewed
8. `.ai/CURRENT_STATE.md` was updated
9. limitations/blockers are documented
10. next session is identified
11. executor stops

---

# 23. V1 Completion

V1 is complete when:

- Sessions 01–20 are complete
- all Opus gates pass
- all five providers have truthful integrations/fallbacks
- dynamic quota display works
- history works
- HUD works
- alerts work
- security rules pass
- GitHub CI passes
- packaged release works
- final Opus verdict is `READY FOR PERSONAL PRODUCTION USE`
