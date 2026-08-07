# AI Usage Monitor — Direct Execution Session Prompts

**Version:** 1.0  
**Local Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**GitHub:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**BRD:** `docs/BRD v1.0.md`  

Use each prompt in a separate executor context/session.

Executors: **Terra / Luna / Sonnet**  
Reviewer: **Opus 5**

---

# SESSION 01 — Repository & Solution Foundation

You are the implementation executor for AI Usage Monitor.

EXECUTE SESSION 01 ONLY.

PROJECT:
Local root: D:\AI Tools\Hossam\AI Usage Monitor Tool
GitHub: https://github.com/Hossam1104/AI-Usage-Monitor-Tool
Default branch: main

MANDATORY STARTUP:
1. Read AGENTS.md completely.
2. Read docs/BRD v1.0.md completely.
3. Read .ai/CURRENT_STATE.md.
4. Read docs/IMPLEMENTATION_PLAN.md.
5. Read this Session 01 prompt.
6. Inspect git status.
7. Inspect the project directory.
8. Preserve every pre-existing user file/change.

OBJECTIVE:
Create the clean production foundation for a .NET 10 + WinUI 3 personal Windows desktop application.

VERIFY FIRST:
- .NET 10 SDK availability
- Visual Studio/Windows App SDK/WinUI prerequisites as discoverable
- Git installed
- whether the local folder is already a Git repository
- whether origin is configured

GIT:
The repository must target:
https://github.com/Hossam1104/AI-Usage-Monitor-Tool.git

Do not create another GitHub repository.
Use main as the default target branch.
Do not push secrets.

CREATE:
- AIUsageMonitor.sln
- src/AIUsageMonitor.Desktop — WinUI 3
- src/AIUsageMonitor.Domain — class library
- src/AIUsageMonitor.Application — class library
- src/AIUsageMonitor.Infrastructure — class library
- src/AIUsageMonitor.Providers — class library
- tests/AIUsageMonitor.Domain.Tests
- tests/AIUsageMonitor.Provider.Tests
- Directory.Build.props
- .editorconfig
- .gitignore
- concise README.md
- .github/workflows directory if needed for later

PROJECT REFERENCES:
Desktop -> Application
Desktop -> Infrastructure only through composition root where appropriate
Application -> Domain
Infrastructure -> Application + Domain
Providers -> Application + Domain
Tests -> projects they test

Domain must NOT reference:
WinUI, EF Core, SQL Server, HTTP, provider SDKs, or Windows UI APIs.

CONFIGURE:
- .NET 10 targets appropriate to each project
- nullable reference types
- implicit usings where suitable
- modern C#
- warnings/build consistency
- basic dependency injection
- basic Serilog integration/skeleton only

WINUI:
Create the smallest legitimate app shell that builds and launches.
Do not build dashboard functionality yet.

DO NOT:
- implement database schema
- implement provider logic
- implement fake usage
- add Angular
- add cloud backend
- add Jira
- add unrelated libraries

VALIDATE:
- dotnet restore as applicable
- full solution build
- launch WinUI shell if environment supports it
- run test projects
- inspect git diff/status
- ensure bin/obj/.vs/build artifacts are ignored
- inspect changed files for secrets

UPDATE .ai/CURRENT_STATE.md:
- actual git state
- origin
- branch
- SDK/toolchain state
- solution state
- build/test result
- Session 01 status
- blockers
- next Session 02

STOP. Do not execute Session 02.

FINAL RESPONSE:
Use the AGENTS.md executor completion format.

---

# SESSION 02 — Domain & Application Architecture

```text
EXECUTE SESSION 02 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.

Read AGENTS.md, docs/BRD v1.0.md, .ai/CURRENT_STATE.md, docs/IMPLEMENTATION_PLAN.md, this prompt, relevant Session 01 code, then inspect git status.

OBJECTIVE:
Implement provider-independent domain models and application contracts.

IMPLEMENT DOMAIN CONCEPTS:
- Provider
- ProviderAccount
- ProviderConnection
- Subscription
- QuotaDefinition
- QuotaWindow/current normalized quota state
- UsageSnapshot
- AlertRule
- AlertEvent
- SyncEvent

ADD WELL-NAMED TYPES/ENUMS/VALUE OBJECTS AS NEEDED:
- provider code/type
- connection status
- quota type
- quota unit
- data source
- confidence
- billing cadence
- alert type/severity

DYNAMIC QUOTA REQUIREMENTS:
- zero to many quota windows per provider
- optional used value
- optional remaining value
- optional limit
- used percentage
- remaining percentage
- start/reset DateTimeOffset
- provider external key
- source
- confidence
- no fixed five-hour/weekly database/domain properties

APPLICATION CONTRACTS:
- IAiUsageProvider or equivalent
- provider registry
- discovery service
- refresh orchestration
- normalized refresh result
- aggregation service contract
- subscription service contract
- usage snapshot repository abstraction
- settings abstraction
- secure credential abstraction
- alert evaluator
- clock/time abstraction if it materially improves deterministic behavior

RESULT MODEL:
Must represent:
- success
- partial
- auth required
- unsupported
- stale
- provider error

Do not create concrete provider parsers.

TARGETED TESTS ONLY:
- quota percentage validation/normalization
- used/remaining consistency
- arbitrary quota windows
- DateTimeOffset behavior
- important invalid states

Avoid test bloat.

VALIDATE:
build, targeted tests, project references, git diff, secrets.

UPDATE CURRENT_STATE:
Session 02 status, actual decisions, validation, next Session 03.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 03 — EF Core 10 + SQL Server LocalDB

```text
EXECUTE SESSION 03 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.

Read mandatory governance/state and inspect git status.

OBJECTIVE:
Implement production-quality local persistence with EF Core 10 + Microsoft SQL Server LocalDB.

REQUIRED:
- DbContext
- entity configurations
- migrations
- local connection strategy
- repositories/persistence services
- safe initialization/migration path
- graceful LocalDB unavailable handling

PERSIST:
Providers
ProviderConnections
Subscriptions
QuotaDefinitions
UsageSnapshots
AlertRules
AlertEvents
SyncEvents
Settings

DATABASE RULES:
- database name: AIUsageMonitor or equivalent
- no username/password required for LocalDB
- no provider secrets
- no raw tokens
- use useful indexes
- use sensible constraints
- preserve history
- never drop data as upgrade shortcut
- support cancellation/transactions appropriately

DUPLICATE SNAPSHOT RULE:
Do not write a new history row every refresh when nothing materially changed.
Define a clear material-change rule and test it.

LOCALDB MISSING:
Return a user-readable prerequisite/error result.
Do not crash the app.

VALIDATE:
- migration generated
- clean DB creation
- migration apply
- write/read
- duplicate prevention
- build
- tests
- migration review
- git diff
- secret check

Do not implement providers/dashboard.

Update CURRENT_STATE with migration/database/build status and next Session 04.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 04 — Provider Feasibility Investigation

```text
EXECUTE SESSION 04 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Provider detection must not assume Windows 11-only paths/registry/shell behavior unless the provider itself requires that OS; distinguish a provider limitation from an AI Usage Monitor limitation.

THIS IS AN EVIDENCE SESSION, NOT A MASS PROVIDER IMPLEMENTATION SESSION.

Read AGENTS.md, docs/BRD v1.0.md, CURRENT_STATE, docs/IMPLEMENTATION_PLAN.md, provider contracts, then inspect git status.

Investigate the ACTUAL installed/authenticated environment for:

1. Codex
2. Claude
3. Kimi / Kimi Code
4. GitHub Copilot
5. Google Antigravity

FOR EACH PROVIDER DETERMINE WITH EVIDENCE:
- local installation detection method
- account/authentication-state detection
- official API availability
- official OAuth/device authentication
- official CLI/status/usage command
- safe local metadata
- actual quota windows exposed
- whether source values mean USED or REMAINING
- absolute used/remaining/limit if available
- reset timestamp and timezone
- plan
- original subscription start
- billing period / renewal
- network requirement
- request/rate-limit constraints
- authentication expiration behavior
- safest fallback

SECURITY:
- no browser-cookie extraction
- no password capture
- no token printing
- no raw credential commits
- no secrets in fixtures
- sanitize sample payloads

DO NOT:
assume an online document means the user's account exposes a programmatic endpoint.

Where safe, compare results with the provider's own visible usage surface.

CLASSIFY EVERY CAPABILITY:
VERIFIED
NOT AVAILABLE
MANUAL FALLBACK
FURTHER INVESTIGATION

UPDATE .ai/CURRENT_STATE.md:
- provider capability matrix
- concise evidence notes
- recommended safe implementation source per provider for Sessions 08–12
- unresolved risks
- Session 04 validation/status

You may create tiny investigation utilities if necessary, but remove credential-bearing/debug artifacts.

Build the solution before stopping.

Do not execute Sessions 08–12.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# REVIEW GATE A — OPUS 5

```text
You are Opus 5 acting as principal architect and adversarial reviewer.

REVIEW GATE A ONLY.

CROSS-WINDOWS COMPATIBILITY REVIEW:
Check target framework, minimum OS, architecture configuration, OS-independent domain/application design, and any dependency that could raise the minimum OS above build 17763 (AGENTS.md §3A). Classify violations as findings, not stylistic notes.

Read:
AGENTS.md
docs/BRD v1.0.md
docs/IMPLEMENTATION_PLAN.md
.ai/CURRENT_STATE.md
all source/tests/migrations relevant to Sessions 01–04
git history/diff where useful

REVIEW:
- solution structure
- dependency direction
- domain correctness
- dynamic quota model
- provider contracts
- EF Core schema/migrations
- LocalDB strategy
- credential boundaries
- provider feasibility evidence
- undocumented assumptions
- unnecessary complexity
- missing foundations that will cause later rework

For Session 04, demand evidence. Reject invented/convenient provider claims.

Classify:
BLOCKER
HIGH
MEDIUM
LOW

Output:
Architecture verdict
Security verdict
Provider-feasibility verdict
Findings by severity
Required remediation before Session 05

Final verdict exactly one:
APPROVED TO CONTINUE
or
REJECTED — BLOCKERS LISTED

If instructed to record review in repo, update only the relevant reviewer/current-gate portion of .ai/CURRENT_STATE.md.
Do not implement features.
```

---

# SESSION 05 — WinUI Design System

```text
EXECUTE SESSION 05 ONLY after Gate A approval/remediation.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Mica/Acrylic/modern backdrop usage must be capability checked and have a compatible fallback; the design must remain attractive even when advanced effects are unavailable.

OBJECTIVE:
Create the reusable WinUI 3 design system.

STYLE:
Modern developer command center.
Dark-first.
Mica.
Subtle Acrylic only where useful.
Restrained gradients.
Rounded cards.
Strong typography.
High-density but uncluttered.
Subtle motion.
Excellent quota readability.
No neon overload.
No marketing landing-page look.

IMPLEMENT:
- app shell/navigation
- Mica backdrop
- Dark / Light / System theme
- typography hierarchy
- spacing/layout system
- reusable card control/style
- provider identity treatment
- quota progress visualization
- status pills/indicators
- warning/critical states
- loading skeletons
- empty state
- stale state
- error state
- keyboard focus
- accessibility names/labels

Use clearly isolated design-time/sample data only.

Do not implement providers.
Do not hardcode five-hour + weekly.

MANUAL VALIDATION:
themes, resizing, keyboard, DPI/text scaling where possible, readability.

Build.
Review diff.
Update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 06 — Main Dashboard

```text
EXECUTE SESSION 06 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.

OBJECTIVE:
Implement the main dashboard bound to provider-independent view models/services.

REQUIRED:
- app header/live refresh status
- overall available capacity
- best-capacity provider
- next reset
- responsive provider cards
- dynamic quota rows
- remaining percentage/value
- reset countdown
- exact reset timestamp/details
- provider connection status
- stale/partial/error states
- Refresh All
- refresh-in-progress
- capacity insight panel binding placeholder

CARD RULES:
- zero/one/multiple quotas
- credit-only provider layout is intentional
- never invent missing 5h/weekly
- primary values explicitly say AVAILABLE/REMAINING
- stale last-known values remain visible with stale status

No provider payload parsing in UI.

Validate compact/normal/large windows and high DPI.
Build.
Update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 07 — System Tray & Focus HUD

```text
EXECUTE SESSION 07 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Verify tray/window behavior does not unnecessarily depend on Windows 11-only APIs.

OBJECTIVE:
Make monitoring continuously useful while working.

SYSTEM TRAY:
- icon
- Open Dashboard
- Show/Hide Focus HUD
- Refresh All
- Pause/Resume Monitoring hook
- Settings
- Exit

LIFECYCLE:
- close/minimize main window to tray according to product behavior
- explicit Exit terminates
- no orphan process
- predictable restore

FOCUS HUD:
- polished compact floating monitor
- provider rows
- most relevant remaining quotas
- best provider
- stale/error indicators
- compact/expanded mode
- always-on-top option
- remember position
- remember size/mode
- optional opacity if stable
- manual refresh
- provider/dashboard click-through

Keep it readable beside VS Code/IDE and non-intrusive.

Build/manual validate.
Update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 08 — Codex Provider

```text
EXECUTE SESSION 08 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Provider detection must not assume Windows 11-only paths/registry/shell behavior unless Codex itself requires that OS.

Read Codex evidence in .ai/CURRENT_STATE.md first.

If actual current Codex behavior differs from Session 04, re-verify and update the evidence before coding.

OBJECTIVE:
Implement only the safest VERIFIED Codex capabilities.

IMPLEMENT AS VERIFIED:
- installation detection
- auth/account status
- usage collection
- actual quota windows
- used/remaining normalization
- reset timestamp
- plan/account metadata
- subscription metadata
- manual fallback for unavailable subscription fields
- structured errors/stale behavior

Keep:
detector, collector, parser, normalizer and provider facade separated conceptually.

SECURITY:
no browser cookies
no raw tokens in logs
no unsanitized payloads committed

Persist normalized snapshots only.

TEST:
sanitized parser fixtures
used/remaining transformation
reset timezone

INTEGRATE:
generic dashboard + HUD contracts only.

MANUALLY COMPARE monitor values with the real Codex usage surface.

Do NOT force a 5h+weekly layout if the actual account exposes different windows.

Build/test/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 09 — Claude Provider

```text
EXECUTE SESSION 09 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Provider detection must not assume Windows 11-only paths/registry/shell behavior unless Claude itself requires that OS.

Use VERIFIED Session 04 Claude mechanisms.

TARGET WHEN VERIFIED:
- Claude/Claude Code detection
- connected account status
- plan
- 5-hour/session quota
- session reset
- weekly quota
- weekly reset
- model-specific quota
- extra usage/credits
- safe subscription metadata

Normalize source semantics into generic remaining-capacity domain values.

Handle:
auth expiration
partial data
stale data
malformed source
timeout/network errors

Never scrape browser credentials.

Persist normalized snapshots.
Add sanitized fixtures/tests.
Integrate dashboard/HUD.
Compare with Claude's own usage display.
Verify timezone/reset carefully.

Build/test/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 10 — Kimi Provider

```text
EXECUTE SESSION 10 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Provider detection must not assume Windows 11-only paths/registry/shell behavior unless Kimi itself requires that OS.

Use VERIFIED Session 04 Kimi mechanisms.

TARGET WHEN VERIFIED:
- Kimi/Kimi Code detection
- account
- membership plan
- rolling 5-hour quota
- weekly quota
- monthly membership credits
- Extra Usage
- reset timestamps
- subscription metadata

Prefer official CLI/API/account mechanisms found in Session 04.

No browser-cookie extraction.

Normalize dynamic quota data.
Persist snapshots.
Support partial/stale/auth-required.
Add sanitized fixtures and parser tests.
Integrate dashboard/HUD.
Compare with Kimi's own usage surface.

Build/test/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 11 — GitHub Copilot Provider

```text
EXECUTE SESSION 11 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Provider detection must not assume Windows 11-only paths/registry/shell behavior unless Copilot itself requires that OS.

Use the GitHub-supported authentication approach verified during Session 04.

TARGET WHEN AVAILABLE:
- GitHub identity
- Copilot plan
- AI Credits used/remaining
- monthly reset/billing-cycle context
- subscription/renewal metadata

CRITICAL:
Do NOT fabricate 5-hour or weekly Copilot limits.

If personal Copilot APIs do not expose a requested value:
- return unavailable
- support manual subscription data where appropriate
- do not scrape browser cookies/pages as a workaround

Store authentication through the secure credential abstraction.
No token in DB/log/config.

Persist normalized snapshots.
Integrate dashboard/HUD.
Add focused tests.
Manually compare with GitHub's own usage/billing view.

Build/test/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 12 — Antigravity Provider

```text
EXECUTE SESSION 12 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Provider detection must not assume Windows 11-only paths/registry/shell behavior unless Antigravity itself requires that OS.

Use only VERIFIED safe mechanisms from Session 04.

TARGET WHEN VERIFIED:
- installation detection
- account/connection
- Google AI plan
- actual quota/model pool
- 5-hour window where exposed
- weekly window where exposed
- reset timestamps
- AI-credit/overage state
- subscription metadata

Do not weaken credential security to get additional fields.
No browser-cookie extraction.

If programmatic access is unreliable:
- implement the safe partial adapter
- keep detection/status
- provide manual fallback
- mark unavailable fields honestly

Normalize/persist data.
Add sanitized parser tests where a machine-readable source exists.
Integrate UI/HUD.
Build/test/manual verify/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# REVIEW GATE B — OPUS 5

```text
You are Opus 5 performing provider-integrity review.

CROSS-WINDOWS COMPATIBILITY REVIEW:
Check provider detection compatibility, Windows-specific assumptions, and that provider-specific OS requirements are correctly isolated from the rest of the app (AGENTS.md §3A). Classify violations as findings, not stylistic notes.

Review Sessions 08–12 against Session 04 evidence.

AUDIT:
- invented endpoints
- undocumented brittle scraping
- browser cookies
- token leakage
- unsafe token persistence
- source semantic errors
- used/remaining inversion
- quota labels
- reset time/timezone
- subscription fabrication
- stale data displayed current
- last-known values replaced by zero
- one provider breaking all providers
- excessive provider traffic
- duplicate snapshots
- provider-specific logic in UI

Classify BLOCKER/HIGH/MEDIUM/LOW.

Output:
Provider-by-provider verdict
Security verdict
Data correctness verdict
Findings
Required remediation

Final gate verdict exactly:
APPROVED TO CONTINUE
or
REJECTED — BLOCKERS LISTED
```

---

# SESSION 13 — Subscription Management

```text
EXECUTE SESSION 13 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.

OBJECTIVE:
Implement clear subscription and billing-period management.

SUPPORT:
- plan
- original subscription start
- current billing-period start
- billing-period end
- renewal date
- cancellation date
- paid-through semantics
- auto-renew
- price
- currency
- billing cadence
- source
- confidence
- last verified

RULES:
- auto-populate only verified provider data
- do not infer dates as facts
- missing values are valid
- manual entry/edit supported
- detail UI indicates Manual vs Provider source
- recurring subscriptions use billing/renewal language
- cancelled subscription shows paid-through correctly

Implement service, persistence and UI.
No payment processing.

Build/test/manual validate/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 14 — History & Analytics

```text
EXECUTE SESSION 14 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.

OBJECTIVE:
Implement accurate usage history and analytics.

VIEWS:
24h
7d
30d
billing cycle where available

CALCULATE:
- remaining-capacity series
- consumed delta
- burn rate
- reset events
- trend
- estimated exhaustion
- estimated remaining at reset

RULES:
- no fake interpolation across gaps
- stale periods shown as gaps/stale
- no forecast with insufficient samples
- quota reset must not be counted as negative consumption
- DateTimeOffset-safe
- estimates clearly labelled

Create readable performant charts.
Add high-value math tests.

Build/test/manual validate/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 15 — Capacity Recommendation Engine

```text
EXECUTE SESSION 15 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.

OBJECTIVE:
Implement deterministic provider capacity scoring.

THIS IS NOT A MODEL-QUALITY RANKING.

INPUTS WHEN AVAILABLE:
- provider health
- current remaining
- shortest relevant quota
- weekly remaining
- monthly/credit remaining
- reset proximity
- burn rate
- stale confidence
- exhausted status

OUTPUT:
- score
- ranking
- explanation
- recommended provider for extended usage
- warning for constrained providers

RULES:
- deterministic
- inspectable formula
- no LLM/API call
- no claim about intelligence/coding quality
- missing data handled explicitly
- stale penalized
- exhausted/unavailable cannot rank normally
- credit-only providers handled without fake weekly equivalence

Centralize scoring weights/config.

TEST:
healthy
missing data
stale
exhausted
reset soon
high burn
credit-only
ties

Integrate dashboard + Focus HUD.

Build/test/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 16 — Monitoring & Notifications

```text
EXECUTE SESSION 16 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Require low idle CPU, reasonable memory usage, no aggressive polling, no unnecessary high-frequency timers.

OBJECTIVE:
Implement reliable low-overhead background monitoring.

REFRESH:
- default about 60 seconds
- startup
- manual
- resume from sleep
- foreground where sensible
- safe local change trigger where useful
- cancellation aware
- provider isolated
- no UI blocking
- no overlapping duplicate refresh

RESILIENCE:
- timeout
- retry/backoff
- rate-limit awareness
- avoid meaningful quota consumption by monitoring itself

ALERTS:
- warning <30% remaining default
- critical <15%
- exhausted
- reset/restored
- auth expired
- stale provider
- upcoming subscription renewal

DEDUPLICATE:
Do not send the same threshold alert every refresh.
Track/cooldown state.

Persist alert events as appropriate.
Use Windows notifications.

Build/test/manual validate/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 17 — Settings, Security & Resilience

```text
EXECUTE SESSION 17 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
OS capability detection and resilience must be reviewed: any post-17763 API/feature use must be guarded with a working fallback, never a crash.

OBJECTIVE:
Finish configuration and production hardening.

SETTINGS:
- provider enable/disable
- refresh interval
- Start with Windows
- Dark/Light/System
- Focus HUD options
- notification thresholds
- provider setup
- manual subscriptions

SECURITY:
- Windows Credential Manager implementation
- DPAPI only if necessary
- no secrets in LocalDB
- no secrets in config
- log redaction
- sanitized diagnostics
- secure disconnect/remove credential behavior

RESILIENCE:
- LocalDB unavailable
- DB transient error
- offline
- provider timeout
- rate limit
- malformed data
- installation removed
- authentication expired
- partial data
- individual provider exception

One provider failure must not break other refreshes.

Build/test/manual validate/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 18 — UX & Performance Polish

```text
EXECUTE SESSION 18 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Explicitly validate reduced/no visual effects, older supported Windows behavior, x64 primary build, x86 build, ARM64 build where the environment permits, hardware-independent functionality, and high-DPI/normal-DPI behavior.

NO NEW FEATURES.

Perform a focused V1 UX/performance pass.

UX:
- scanability
- information hierarchy
- remaining wording
- reset wording
- provider cards
- hover/focus
- keyboard
- accessibility names
- text scaling
- high DPI
- resizing
- themes
- loading
- empty
- stale
- partial
- error
- Focus HUD
- tray lifecycle

PERFORMANCE:
- UI-thread blocking
- unnecessary DB queries
- unnecessary provider requests
- background CPU
- chart/history scaling
- cancellation
- disposal
- event leaks
- memory/resource use where obvious

Keep animations restrained.

Do not redesign core architecture unless fixing a real problem.

Build/test/manual regression/update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# REVIEW GATE C — OPUS 5

```text
You are Opus 5 performing the pre-release review.

CROSS-WINDOWS COMPATIBILITY REVIEW:
Check UI graceful degradation, older-system resource usage, API capability checks, background performance, and architecture outputs across the supported OS/architecture matrix (AGENTS.md §3A). Classify violations as findings, not stylistic notes.

Read all governance/state plus implementation through Session 18.

REVIEW:
- BRD V1 coverage
- architecture
- provider correctness
- security
- WinUI/MVVM boundaries
- UX/accessibility
- performance
- LocalDB/history
- resilience
- monitoring
- alerts
- notification dedupe
- analytics
- recommendation engine

Do not propose speculative features.

Classify:
BLOCKER
HIGH
MEDIUM
LOW

Output:
Release blockers
Security verdict
Architecture verdict
UX verdict
Provider reliability verdict
Required remediation

Gate verdict exactly:
APPROVED TO CONTINUE
or
REJECTED — BLOCKERS LISTED
```

---

# SESSION 19 — Packaging, CI & Release Engineering

```text
EXECUTE SESSION 19 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Explicitly require release consideration for win-x86, win-x64, win-arm64, and verify no packaging decision silently raises the minimum OS beyond build 17763.

OBJECTIVE:
Create repeatable personal installation/release.

IMPLEMENT:
- Release configuration
- GitHub Actions restore/build/test
- no live provider credentials in CI
- version metadata
- appropriate WinUI Windows packaging
- LocalDB prerequisite detection/documentation
- upgrade-safe EF migrations
- release artifact
- concise README install/update instructions

GitHub repository:
https://github.com/Hossam1104/AI-Usage-Monitor-Tool

Do not create:
- cloud infrastructure
- Jira
- online AI Usage Monitor account
- Angular site

If repository remote is not correct, fix it safely without destroying user history.

Validate:
- local Release build
- tests
- packaged startup where possible
- migration behavior on upgrade path
- GitHub workflow syntax/logic
- no secrets

Update CURRENT_STATE.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# SESSION 20 — Final Stabilization

```text
EXECUTE SESSION 20 ONLY.

CROSS-WINDOWS COMPATIBILITY:
The Cross-Windows Compatibility Contract in AGENTS.md is mandatory for this session.
Minimum OS: Windows 10 1809 / build 17763. Supported architectures: x86, x64, ARM64. x64 is the primary validation target.
Do not introduce Windows 11-only dependencies without a compatible runtime fallback.
Do not introduce dependencies that raise the approved minimum OS without planner approval.
Add final acceptance validation for the Windows 10 1809 compatibility contract, later Windows 10 releases, Windows 11, x86, x64, ARM64, and modern-effects-unavailable fallback. Testing may use real hardware, VMs, build validation, compatibility guards, or a justified combination of what is realistically available — do not claim hardware/OS execution that was not actually performed.

THIS IS FINAL IMPLEMENTATION.
NO NEW FEATURES.

Read the full BRD and compare every V1 requirement against actual code.

Create a concise requirement-compliance section in .ai/CURRENT_STATE.md or another planner-approved location only if needed; do not create documentation bloat.

VALIDATE:
- fresh launch
- existing LocalDB/history
- LocalDB unavailable
- all providers enabled
- provider disabled
- provider partial
- provider stale
- auth expired
- offline startup
- quota consumption update
- quota reset
- manual subscription
- Dashboard
- Provider Details
- History
- recommendation
- thresholds
- notifications
- Focus HUD
- tray
- app restart
- Windows startup option
- Release package

Resolve all remaining BLOCKER/HIGH defects.

Run all targeted tests.
Review project for:
- dead code
- debug artifacts
- secrets
- stale TODOs that represent release blockers
- accidental mock/sample runtime data

Update .ai/CURRENT_STATE.md:
- Session 20 status
- final validation
- remaining MEDIUM/LOW known issues
- Release State = RELEASE CANDIDATE if appropriate
- Next = Final Opus Review

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

STOP.
```

---

# FINAL RELEASE REVIEW — OPUS 5

```text
You are Opus 5, final independent reviewer for AI Usage Monitor v1.0.

CROSS-WINDOWS COMPATIBILITY REVIEW:
The compatibility verdict must explicitly cover Windows 10 1809+, Windows 11, x86, x64, ARM64, fallback behavior when modern effects are unavailable, and the absence of accidental Windows 11-only core dependencies (AGENTS.md §3A). A release-blocking compatibility regression must be classified per severity like any other finding.

Treat the implementation as a release candidate.

READ:
- AGENTS.md
- docs/BRD v1.0.md
- docs/IMPLEMENTATION_PLAN.md
- .ai/CURRENT_STATE.md
- complete source tree
- provider implementations
- tests
- migrations
- GitHub Actions
- packaging
- relevant git history

FOR EVERY BRD REQUIREMENT CLASSIFY:
PASS
PARTIAL
FAIL
NOT APPLICABLE

ADVERSARIALLY AUDIT:
1. fabricated provider data
2. incorrect quota semantics
3. used/remaining inversion
4. reset/timezone errors
5. insecure credentials
6. browser-cookie handling
7. secret logging
8. LocalDB reliability
9. migration safety/history preservation
10. provider isolation
11. UI thread blocking
12. stale data
13. notification spam
14. analytics math
15. forecast validity
16. capacity scoring
17. tray/window lifecycle
18. packaging
19. GitHub CI
20. release usability

OUTPUT:
- Release verdict
- Blockers
- High findings
- Medium findings
- Low findings
- Requirement compliance summary
- Security verdict
- Architecture verdict
- UX verdict
- Provider reliability verdict

FINAL VERDICT MUST BE EXACTLY ONE:

READY FOR PERSONAL PRODUCTION USE

or

NOT READY FOR PERSONAL PRODUCTION USE

Do not give conditional approval if any BLOCKER/HIGH release issue remains.
```

---

# OPTIONAL — OPUS REMEDIATION HANDOFF PROMPT

Use this only when an Opus gate rejects the project and you want Terra/Luna/Sonnet to fix exactly those findings.

```text
Act as implementation executor.

Read:
AGENTS.md
docs/BRD v1.0.md
.ai/CURRENT_STATE.md
docs/IMPLEMENTATION_PLAN.md
the latest Opus review findings
git status and affected code

OBJECTIVE:
Resolve ONLY the BLOCKER and HIGH findings from the latest review gate, plus any directly required supporting changes.

Do not add new product scope.
Do not proceed to the next planned session.

For each finding:
- confirm the defect against code/evidence
- implement the smallest production-quality correction
- add/update targeted validation
- avoid workarounds that violate BRD/security/provider truthfulness

Build/test.
Review diff.
Update .ai/CURRENT_STATE.md with each finding's resolution status.

GIT DELIVERY:
The Git Delivery Contract in AGENTS.md is mandatory.

Before stopping:
- update .ai/CURRENT_STATE.md
- commit session changes
- push the session branch
- merge into main
- push main
- verify origin/main
- leave the working tree clean

Do not ask whether completed session work should be committed or merged.
Do not execute the next session.

Stop and return the project to the same Opus review gate.
```
