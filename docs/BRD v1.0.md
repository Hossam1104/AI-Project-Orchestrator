# AI Usage Monitor

## Business Requirements Document, Technical Plan & AI Execution Plan

**Document Version:** 1.0
**Date:** 08 August 2026
**Product Type:** Personal Windows Desktop Utility
**Owner:** Hossam
**Planner:** GPT-5.6 Sol
**Executors:** Luna / Terra / Sonnet
**Reviewer:** Opus 5
**Source Control:** GitHub
**Project Management:** Repository-driven; no Jira
**Target Platform:** Windows 11 primarily; Windows 10 1809+ compatible where supported

---

# 1. Executive Summary

AI Usage Monitor is a personal Windows desktop application for monitoring usage capacity, quota resets, subscription periods, and historical consumption across multiple AI coding subscriptions.

Initial providers:

1. OpenAI Codex
2. Anthropic Claude
3. Kimi / Kimi Code
4. GitHub Copilot
5. Google Antigravity

The application will run locally on the user's PC, detect installed/authenticated AI tools where possible, and use supported provider authentication methods where local detection does not provide sufficient information.

The application is not intended to become a commercial SaaS product, team platform, or organization-management product.

Its purpose is simple:

> Provide one reliable, visually excellent control center showing how much AI capacity is available before starting or continuing work.

Primary information displayed includes:

* Current subscription/plan
* Current quota windows
* Five-hour quota where applicable
* Weekly quota where applicable
* Monthly/credit quota where applicable
* Usage percentage
* Remaining percentage
* Reset date/time
* Reset countdown
* Subscription original start date where available
* Current billing-period start
* Current billing-period end/renewal date
* Historical consumption
* Usage velocity
* Estimated exhaustion
* Provider health
* Recommended provider based on remaining capacity

The application will be local-first, privacy-conscious, resilient to individual provider failures, and designed so provider integrations can change without requiring major modifications to the application.

---

# 2. Product Vision

The product should answer the following question within approximately two seconds of opening it:

> "Which AI subscription has enough capacity for the work I want to do right now?"

The user should not need to open:

* ChatGPT settings
* Claude settings
* Kimi console
* GitHub billing
* Antigravity settings

individually.

The application consolidates the information into one workspace.

---

# 3. Product Principles

The following principles are mandatory.

## 3.1 Local First

The application operates primarily from the local PC.

No application-owned cloud backend is required.

No mandatory external AI Usage Monitor account exists.

No telemetry will be transmitted.

No application analytics will be transmitted.

---

## 3.2 Provider Truth Over UI Symmetry

Different providers expose different quota systems.

The application SHALL NOT fabricate a five-hour or weekly value simply because another provider exposes one.

For example, current verified provider behavior includes:

* Claude Pro uses a five-hour session limit plus a weekly usage limit.
* Kimi Code documents a rolling five-hour window plus a weekly quota, while Kimi membership also uses monthly credits.
* Google Antigravity paid plans currently use five-hour quota refreshes and weekly limits.
* Current individual GitHub Copilot plans are metered using monthly GitHub AI Credits.
* Current Codex usage varies according to plan and draws from the broader agentic usage/credit system; the Codex usage surface remains the authoritative source for an account's actual limits.

Therefore the data model SHALL support arbitrary quota windows.

---

## 3.3 No Credential Theft

The application SHALL NOT:

* extract browser passwords
* copy browser cookies
* decrypt unrelated provider credentials
* capture passwords
* intercept authentication traffic
* store raw account passwords
* silently upload authentication tokens

Authentication priority:

1. Official provider API
2. Official OAuth/device authentication
3. Existing authenticated CLI integration
4. Stable local provider usage metadata
5. Manual configuration

---

# 4. Technology Baseline

## 4.1 Core Stack

The approved technology stack is:

| Area              | Technology                                 |
| ----------------- | ------------------------------------------ |
| Language          | C#                                         |
| Runtime           | .NET 10 LTS                                |
| Desktop UI        | WinUI 3                                    |
| Windows Framework | Windows App SDK                            |
| Pattern           | MVVM                                       |
| Architecture      | Modular Clean Architecture                 |
| ORM               | EF Core 10                                 |
| Database          | Microsoft SQL Server LocalDB               |
| DB Provider       | Microsoft.EntityFrameworkCore.SqlServer    |
| SQL Driver        | Microsoft.Data.SqlClient                   |
| JSON              | System.Text.Json                           |
| HTTP              | HttpClient                                 |
| Resilience        | Microsoft.Extensions.Http.Resilience       |
| DI                | Microsoft.Extensions.DependencyInjection   |
| Logging           | Serilog                                    |
| Secret Storage    | Windows Credential Manager / DPAPI         |
| Source Control    | Git + GitHub                               |
| CI                | GitHub Actions                             |
| Packaging         | MSIX / appropriate WinUI packaging         |
| Tests             | xUnit for critical domain/provider parsers |

.NET 10 is an active LTS release supported through November 2028.

Current Microsoft WinUI guidance supports .NET 10 development for WinUI 3 applications.

---

# 5. Scope

## 5.1 Version 1.0 Scope

V1 shall include:

* Native WinUI 3 desktop application
* Dashboard
* Compact monitoring mode
* Windows tray operation
* Provider auto-detection
* Provider connection management
* Codex monitoring
* Claude monitoring
* Kimi monitoring
* Copilot monitoring
* Antigravity monitoring
* Dynamic quota windows
* Five-hour quota where supported
* Weekly quota where supported
* Monthly credits where supported
* Reset countdowns
* Subscription dates
* Historical usage
* Usage charts
* Usage velocity
* Estimated exhaustion
* Alerts
* Provider recommendation
* SQL Server LocalDB persistence
* Secure credentials
* Manual refresh
* automatic refresh
* dark/light/system theme
* local settings
* logging
* GitHub CI
* packaged release

---

# 6. Explicit Non-Goals

V1 SHALL NOT include:

* Jira
* Azure DevOps
* multi-user accounts
* team management
* SaaS backend
* mobile application
* cloud synchronization
* browser extension
* commercial subscription
* payment processing
* AI prompt execution
* model chat
* automatic switching between AI providers
* automatic upgrading/downgrading subscriptions
* organization billing administration
* usage manipulation
* reverse-engineered password capture
* browser cookie extraction

Angular is intentionally excluded from V1.

Angular may be introduced in a future version only if remote/web access becomes required.

---

# 7. User Persona

The application has one primary user:

A developer/QA professional operating several AI coding subscriptions concurrently and needing to allocate demanding tasks according to remaining capacity.

There is no need to design multi-user permissions.

---

# 8. Primary User Journey

The ideal workflow is:

1. Windows starts.

2. AI Usage Monitor launches minimized.

3. Providers refresh automatically.

4. User begins working.

5. User presses the tray icon or global shortcut.

6. Compact monitor appears.

7. User immediately sees:

   * Codex capacity
   * Claude capacity
   * Kimi capacity
   * Copilot credits
   * Antigravity capacity

8. User sees which provider has the best remaining capacity.

9. User chooses the appropriate AI tool.

10. Application continues monitoring.

11. User receives a notification if a relevant quota becomes low.

12. Application informs the user when a previously exhausted quota becomes available again.

---

# 9. Provider Capability Model

The system must not contain provider-specific quota columns.

Wrong design:

```
CodexFiveHourUsage
ClaudeFiveHourUsage
CodexWeeklyUsage
```

Correct design:

```
Provider
    └── QuotaWindow[]
            ├── Type
            ├── Used
            ├── Remaining
            ├── Limit
            ├── Unit
            ├── StartAt
            ├── ResetAt
            └── Source
```

Supported quota types must include:

* Rolling5Hour
* Session
* Daily
* Weekly
* Rolling7Day
* Monthly
* BillingCycle
* Credits
* AI credits
* ModelSpecific
* RequestAllowance
* Tokens
* ExtraUsage
* Custom

Additional quota types must be addable without schema redesign.

---

# 10. Provider Capability Matrix

Initial expected model:

| Capability         |              Codex |             Claude |               Kimi |            Copilot |        Antigravity |
| ------------------ | -----------------: | -----------------: | -----------------: | -----------------: | -----------------: |
| Local detection    |                Yes |                Yes |                Yes |                Yes |                Yes |
| Account detection  |        Investigate |        Investigate |        Investigate |        GitHub auth |        Investigate |
| 5-hour             |            Dynamic |                Yes |                Yes |                N/A |         Paid plans |
| Weekly             |            Dynamic |                Yes |                Yes |                N/A |         Paid plans |
| Monthly credits    |           Possible |      Usage credits |                Yes |                Yes |    Overage credits |
| Reset timestamp    |        Investigate |                Yes |                Yes |            Monthly |                Yes |
| Plan               |        Investigate |        Investigate |                Yes |                Yes |                Yes |
| Subscription dates | Fallback supported | Fallback supported | Fallback supported | Fallback supported | Fallback supported |
| Local data source  |        Investigate |        Investigate |          CLI/local |        Investigate |        Investigate |
| Manual fallback    |                Yes |                Yes |                Yes |                Yes |                Yes |

"Investigate" is intentional.

Executors MUST verify integrations instead of assuming unofficial endpoints.

---

# 11. Provider Data Source Priority

Every provider SHALL implement collectors using this priority:

```
Official API
      ↓
Official OAuth/API
      ↓
Official CLI command
      ↓
Stable local application data
      ↓
Safe read-only authenticated endpoint
      ↓
Manual entry
```

Every resulting data field shall record:

```
DataSource
CollectionMethod
CapturedAt
Confidence
```

Confidence enum:

```
Official
VerifiedLocal
Inferred
Manual
```

The UI should not overwhelm the user with this information, but Provider Details must expose it.

---

# 12. Provider Connection Status

Supported statuses:

* Connected
* LocalDetected
* AuthenticationRequired
* Partial
* Unsupported
* Disabled
* RateLimited
* Stale
* Error
* Updating

A provider failing must NEVER prevent the rest of the dashboard from loading.

---

# 13. Functional Requirements

## FR-001 — Provider Detection

On first launch the application shall automatically detect supported local AI clients.

Detection must be read-only.

The application should detect:

* installed application/CLI
* known configuration location
* current provider availability

It must not assume a detected installation means authenticated usage data is available.

---

## FR-002 — Provider Setup

The setup screen displays all providers.

Example:

```
Codex              Detected       Configure
Claude             Detected       Configure
Kimi               Detected       Configure
GitHub Copilot     Detected       Connect
Antigravity        Detected       Configure
```

Provider configuration should require minimal intervention.

---

## FR-003 — Dynamic Quota Display

Each provider may expose zero or more quota windows.

Example Claude:

```
Current session     68% remaining
Weekly              41% remaining
```

Example Kimi:

```
5-hour              72%
Weekly              61%
Monthly credits     48%
```

Example Copilot:

```
AI Credits          76%
```

The UI adapts automatically.

---

## FR-004 — Usage Representation

Every percentage must clearly state whether it represents:

* Used
  or
* Remaining

The primary UI convention shall be:

> Remaining capacity.

Example:

```
WEEKLY
68% AVAILABLE
```

This prevents ambiguity.

Provider detail may additionally show:

```
Used: 32%
Remaining: 68%
```

---

# 14. Subscription Requirements

The subscription model shall contain:

* Provider
* Plan name
* Original subscription start date
* Current billing-period start
* Current billing-period end
* Renewal date
* Cancelled date where applicable
* Auto-renew status
* Billing cadence
* Currency
* Price
* Source
* Last verified timestamp

Not every provider exposes all values.

Missing values shall display:

```
Not available
```

not invented data.

Manual editing shall be supported.

---

# 15. Subscription Interpretation

"End of subscription" must distinguish between:

### Active recurring subscription

```
Billing period:
03 Aug → 03 Sep

Renewal:
03 Sep
```

### Cancelled subscription

```
Paid through:
03 Sep

Auto-renew:
Off
```

### Expired subscription

```
Ended:
03 Aug
```

No ambiguous "End Date" label should be used.

---

# 16. Historical Monitoring

A UsageSnapshot shall be saved whenever:

* usage changes
* quota resets
* provider status changes materially
* manual refresh obtains newer values

Do not write duplicate snapshots every minute when nothing changes.

---

# 17. Usage Analytics

Analytics must calculate:

## 17.1 Consumption Since Last Snapshot

Example:

```
Claude weekly
Previous: 71% remaining
Current: 64% remaining
Consumed: 7%
```

---

## 17.2 Burn Rate

Calculate normalized consumption velocity.

Examples:

```
3.8% / hour
11.4% / day
```

Do not calculate a misleading rate when data is insufficient.

---

## 17.3 Estimated Exhaustion

Where enough historical data exists:

```
Estimated exhaustion:
Today 18:35
```

or:

```
Expected remaining at reset:
29%
```

Forecasts shall explicitly be estimates.

---

# 18. Capacity Recommendation Engine

The system shall calculate a capacity score for each applicable provider.

Initial factors:

* short-window capacity
* weekly capacity
* monthly capacity
* time until reset
* consumption velocity
* provider availability
* stale data penalty
* exhausted quota penalty

Do NOT use AI to calculate the score.

Use deterministic business rules.

Example:

```
Best Available Capacity

1. Kimi            88
2. Codex           81
3. Antigravity     72
4. Claude          37
5. Copilot         31
```

Recommendation wording:

```
Recommended for a long session:
Kimi

Claude weekly capacity is relatively low.
```

This is a capacity recommendation, not a claim about model intelligence or coding quality.

---

# 19. Dashboard UX

The primary dashboard must answer three questions immediately:

1. How much capacity do I have?
2. When does it reset?
3. Which provider has the best remaining capacity?

---

# 20. Visual Design Direction

Design theme:

> Modern developer command center.

Use:

* WinUI 3
* Mica
* subtle Acrylic where appropriate
* dark-first design
* restrained gradients
* rounded cards
* subtle shadows
* smooth transitions
* provider accents
* strong typography
* spacious layout
* small motion effects
* high contrast percentages
* progress rings/bars
* high information density without clutter

Avoid:

* oversized marketing UI
* excessive animation
* glowing neon everywhere
* tiny text
* unclear charts
* colors as the only status mechanism
* too many screens

---

# 21. Main Dashboard Concept

Desktop layout:

```
┌──────────────────────────────────────────────────────────────┐
│ AI USAGE MONITOR                     LIVE ●       12:31 AM  │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ AVAILABLE AI CAPACITY                                        │
│                                                              │
│             72%                                              │
│        Overall availability                                  │
│                                                              │
│ BEST CAPACITY                 NEXT RESET                     │
│ Kimi                          Claude · 01:38                  │
│                                                              │
├──────────────┬──────────────┬──────────────┬────────────────┤
│ CODEX        │ CLAUDE       │ KIMI         │ ANTIGRAVITY    │
│ Plus         │ Pro          │ Plan         │ AI Pro         │
│              │              │              │                │
│ SHORT        │ 5 HOUR       │ 5 HOUR       │ 5 HOUR         │
│ 84%          │ 63%          │ 91%          │ 78%            │
│ ████████░░   │ ██████░░░░   │ █████████░   │ ███████░░░     │
│              │              │              │                │
│ WEEK         │ WEEK         │ WEEK         │ WEEK           │
│ 67%          │ 21%          │ 83%          │ 66%            │
│ ███████░░░   │ ██░░░░░░░░   │ ████████░░   │ ███████░░░     │
│              │              │              │                │
│ Reset 2:41   │ Reset 1:32   │ Reset 4:12   │ Reset 4:43     │
└──────────────┴──────────────┴──────────────┴────────────────┘

┌──────────────────────────────────────────────────────────────┐
│ GITHUB COPILOT                                              │
│ AI Credits                                      74%         │
│ █████████████████████░░░░░░░                                │
│ Monthly renewal: 01 Sep                                    │
└──────────────────────────────────────────────────────────────┘

CAPACITY INSIGHT
Kimi currently has the highest available capacity.
Claude weekly availability is approaching your warning threshold.
```

---

# 22. Provider Card

Each provider card includes:

* provider logo/icon
* provider name
* plan
* connectivity indicator
* primary quota
* secondary quota
* reset countdown
* trend indicator
* refresh status

Clicking the card opens provider details.

---

# 23. Focus HUD

This is an important usability feature.

The application shall provide an optional compact floating monitor while the user works.

Example:

```
┌─────────────────────────────┐
│ AI CAPACITY            ●    │
│                             │
│ Codex         84% / 67%     │
│ Claude        63% / 21%     │
│ Kimi          91% / 83%     │
│ Antigravity   78% / 66%     │
│ Copilot            74%      │
│                             │
│ Best: Kimi                  │
└─────────────────────────────┘
```

Features:

* always-on-top optional
* position remembered
* adjustable opacity
* compact mode
* expanded mode
* close returns to tray
* manual refresh
* click provider to open details

This allows the user to monitor quotas without leaving the IDE.

---

# 24. System Tray

Tray menu:

```
Open Dashboard
Show Focus HUD
Refresh All
Pause Monitoring
Provider Status
Settings
Exit
```

Tooltip example:

```
AI Usage Monitor
Best capacity: Kimi 83%
Claude weekly: 21%
```

---

# 25. Provider Details

Provider detail screen contains:

### Header

* provider
* plan
* connection status

### Current Usage

All available quota windows.

### Reset Information

Exact timestamp + relative countdown.

### Subscription

Plan and billing information.

### Usage History

Selectable:

* 24 hours
* 7 days
* 30 days
* billing cycle

### Connection

Collection method and last successful update.

### Troubleshooting

Last synchronization error if applicable.

---

# 26. History Screen

Charts shall support:

* remaining capacity over time
* usage consumption over time
* provider comparison
* reset markers

Charts must not fabricate data between gaps.

Stale periods should visually appear as gaps.

---

# 27. Alerts

Default thresholds:

```
Warning: 30% remaining
Critical: 15% remaining
```

Configurable per provider/window.

Supported alerts:

* quota below threshold
* quota exhausted
* quota restored/reset
* provider stopped updating
* authentication expired
* subscription renewal approaching
* subscription expired

Avoid excessive notifications.

A notification should not repeat every refresh cycle.

---

# 28. Refresh Strategy

Default:

```
Refresh interval:
60 seconds
```

Also refresh:

* application launch
* resume from sleep
* dashboard foreground
* manual refresh
* detected local usage change where possible

Remote provider calls must support appropriate throttling.

On provider rate limiting:

```
exponential backoff
```

The monitor must never itself create meaningful quota consumption.

---

# 29. Database

Default:

> Microsoft SQL Server LocalDB.

Database name:

```
AIUsageMonitor
```

LocalDB installation is a deployment prerequisite and must be checked by the installer/application prerequisite flow.

No external SQL Server should be required.

---

# 30. Core Tables

## Providers

```
Id
Code
DisplayName
Enabled
SortOrder
CreatedAt
UpdatedAt
```

## ProviderConnections

```
Id
ProviderId
ConnectionType
Status
AccountDisplayName
LastSuccessfulSync
LastAttempt
LastErrorCode
LastErrorMessage
```

No secret value stored here.

## Subscriptions

```
Id
ProviderId
PlanName
OriginalStartDate
BillingPeriodStart
BillingPeriodEnd
RenewalDate
CancelledDate
AutoRenew
Price
Currency
BillingCadence
Source
LastVerifiedAt
```

## QuotaDefinitions

```
Id
ProviderId
ExternalKey
Name
Type
Unit
SortOrder
```

## UsageSnapshots

```
Id
ProviderId
QuotaDefinitionId
UsedValue
RemainingValue
LimitValue
UsedPercentage
RemainingPercentage
WindowStart
ResetAt
CapturedAt
Source
Confidence
```

## AlertRules

```
Id
ProviderId
QuotaDefinitionId
WarningThreshold
CriticalThreshold
Enabled
```

## AlertEvents

```
Id
AlertRuleId
TriggeredAt
ResolvedAt
Type
Value
```

## SyncEvents

```
Id
ProviderId
StartedAt
CompletedAt
Success
DataChanged
ErrorCode
ErrorSummary
```

## Settings

Non-sensitive application preferences only.

---

# 31. Credentials

Secrets SHALL NOT be stored in SQL Server.

Store credential references using:

* Windows Credential Manager
* DPAPI-protected data where required

Database only stores:

```
CredentialReference = "GitHub:Copilot:Primary"
```

Never:

```
AccessToken = "ghp_xxxxx"
```

---

# 32. Provider Interface

All integrations implement a common abstraction conceptually equivalent to:

```
IAiUsageProvider
```

Responsibilities:

```
DetectAsync()
GetConnectionStatusAsync()
GetAccountAsync()
GetSubscriptionAsync()
GetQuotasAsync()
RefreshAsync()
```

Collectors must return normalized domain objects.

The UI must never parse provider-specific JSON.

---

# 33. Provider Project Structure

```
AIUsageMonitor.Providers/

    Common/

    Codex/
        CodexProvider
        CodexDetector
        CodexCollectors
        CodexNormalizer

    Claude/
        ClaudeProvider
        ClaudeDetector
        ClaudeCollectors
        ClaudeNormalizer

    Kimi/
        KimiProvider
        KimiDetector
        KimiCollectors
        KimiNormalizer

    Copilot/
        CopilotProvider
        CopilotDetector
        CopilotCollectors
        CopilotNormalizer

    Antigravity/
        AntigravityProvider
        AntigravityDetector
        AntigravityCollectors
        AntigravityNormalizer
```

---

# 34. Provider Feasibility Rule

No provider is considered implemented until a real authenticated sample has been validated.

For each provider the executor must document:

```
Installation detection:
Working / Not working

Authentication source:
...

Usage source:
...

Plan source:
...

Subscription source:
...

Short-window quota:
...

Weekly quota:
...

Monthly quota:
...

Reset timestamp:
...

Failure behavior:
...

Fallback:
...
```

Never create production code based only on assumptions about undocumented file structures.

---

# 35. Codex Adapter

The Codex adapter should investigate:

1. installed Codex CLI/application
2. available local usage/session metadata
3. authenticated account metadata
4. official Codex usage information available to the account
5. quota window identifiers
6. remaining credit information
7. reset timestamps

Current Codex limits vary according to account/plan and share the wider agentic usage system. Therefore:

> Do not hardcode Codex as "5-hour + weekly."

The adapter discovers the windows available to the actual account.

---

# 36. Claude Adapter

Target information:

* plan
* five-hour session usage
* five-hour reset
* weekly usage
* weekly reset
* model-specific weekly quota where exposed
* usage credits where enabled

Claude officially documents its five-hour session reset and weekly limits for paid individual plans.

---

# 37. Kimi Adapter

Target:

* membership plan
* five-hour Kimi Code quota
* weekly Kimi Code quota
* monthly membership credits
* reset timestamps
* Extra Usage if enabled

Kimi officially exposes usage checking through its product/console and `/usage` CLI behavior, and documents five-hour plus weekly Kimi Code quota.

---

# 38. Copilot Adapter

Target:

* GitHub account
* Copilot plan
* AI credits remaining/used
* monthly reset/billing context where available

Do not create fake session/weekly windows.

Current individual Copilot plans are centered around monthly GitHub AI Credits.

Use GitHub-supported authentication.

---

# 39. Antigravity Adapter

Target:

* detected Antigravity installation
* Google AI plan
* baseline quota
* remaining percentage
* five-hour refresh where plan supports it
* weekly quota
* AI credit overage state where available

Current paid Google AI Pro/Ultra Antigravity plans expose five-hour and weekly quota concepts.

Usage is visible through Antigravity's settings/model interface.

No unofficial authentication bypass is permitted.

---

# 40. Error Handling

Every provider operation returns a structured result.

Never display raw exceptions as the primary UI message.

Example:

```
Claude
Could not refresh usage.

Last successful update:
12:36 AM

Current displayed information may be outdated.
```

Expandable technical details may show the actual diagnostic information.

---

# 41. Stale Data

Provider data becomes stale after configurable intervals.

Suggested initial statuses:

```
<5 minutes       Current
5–30 minutes    Slightly stale
>30 minutes      Stale
```

Stale values must remain visible but clearly marked.

Do not replace useful last-known data with zero.

---

# 42. Offline Operation

When offline:

* application launches normally
* stored history loads
* last-known quota remains visible
* providers show Offline/Stale
* analytics remain accessible

---

# 43. Startup

User setting:

```
Start AI Usage Monitor with Windows
```

Default:

```
Off
```

Once enabled:

* app starts minimized
* tray icon becomes available
* refresh starts automatically

---

# 44. Performance Targets

Targets, not fabricated guarantees:

* application usable within ~3 seconds on normal development PC
* dashboard navigation feels immediate
* refresh operations never block UI
* background refresh consumes minimal CPU
* no continuous unnecessary database queries
* no aggressive provider polling
* no significant IDE performance degradation

All I/O shall be asynchronous where appropriate.

---

# 45. Security Requirements

SEC-001:
No provider passwords stored.

SEC-002:
No browser cookie harvesting.

SEC-003:
OAuth tokens stored securely.

SEC-004:
Logs must redact secrets.

SEC-005:
GitHub commits must not contain credentials.

SEC-006:
Provider payload samples committed to repository must be sanitized.

SEC-007:
No secrets in appsettings.json.

SEC-008:
No secrets in LocalDB.

SEC-009:
No telemetry by default.

SEC-010:
External network requests shall only target configured provider services.

---

# 46. Privacy

The app may locally store:

* provider name
* usage percentages
* reset timestamps
* plan information
* subscription dates
* historical usage
* account display label if required

The app SHALL NOT store:

* prompt contents
* source code
* conversation contents
* Claude chat history
* Codex conversation content
* Kimi prompt history
* GitHub repository code
* Antigravity task content

Only usage metadata is relevant.

---

# 47. Accessibility

The UI must support:

* Windows text scaling
* keyboard navigation
* accessible labels
* tooltip descriptions
* adequate contrast

Warning severity must never depend only on color.

Use text/icon + color.

---

# 48. Repository Structure

Repository name recommendation:

```
ai-usage-monitor
```

Structure:

```
ai-usage-monitor/
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
├── docs/
│   └── BRD.md
│
├── .ai/
│   ├── CURRENT_STATE.md
│   ├── EXECUTION_PLAN.md
│   ├── DECISIONS.md
│   └── PROMPTS.md
│
├── .github/
│   └── workflows/
│
├── AGENTS.md
├── README.md
├── Directory.Build.props
├── .editorconfig
├── .gitignore
└── AIUsageMonitor.sln
```

Keep documentation intentionally compact.

Do not create dozens of redundant Markdown files.

---

# 49. AGENTS.md Responsibility

AGENTS.md is the permanent AI execution contract.

Every executor must:

1. Read AGENTS.md.
2. Read docs/BRD.md.
3. Read .ai/CURRENT_STATE.md.
4. Read .ai/EXECUTION_PLAN.md.
5. Read .ai/DECISIONS.md when relevant.
6. Inspect Git status.
7. Execute only the assigned session.
8. Do not redesign approved architecture without documenting a blocking reason.
9. Do not invent provider APIs.
10. Do not add unnecessary dependencies.
11. Build the solution.
12. Run targeted validation relevant to changed code.
13. Review its Git diff.
14. Update CURRENT_STATE.md.
15. Update DECISIONS.md only for real architectural decisions.
16. Never commit credentials.
17. Never mark unverified provider capability as complete.

---

# 50. Git Strategy

Keep the Git workflow simple.

Primary branch:

```
main
```

Development:

```
feature/session-XX-description
```

Each session produces:

```
one logical implementation scope
clean commit(s)
no unrelated modifications
```

Commit format:

```
feat:
fix:
refactor:
docs:
chore:
```

Examples:

```
feat: add provider abstraction layer
feat: implement Claude usage adapter
fix: preserve stale quota snapshots
feat: add focus HUD
```

No Jira IDs.

---

# 51. CI

GitHub Actions on pull/push shall:

1. restore dependencies
2. build Release
3. execute targeted automated tests
4. fail on compile errors
5. retain useful build logs

No live-provider credentials in CI.

Provider tests use sanitized fixtures.

---

# 52. Testing Strategy

Testing must remain focused.

Required automated coverage:

* quota normalization
* percentage calculations
* reset calculation
* burn-rate calculation
* recommendation scoring
* provider parsers
* database persistence
* secret redaction

Do not create thousands of artificial tests.

Do not perform live provider calls in CI.

UI acceptance may remain manual for V1.

---

# 53. Definition of Done

A session is Done only when:

* objective implemented
* project builds
* related tests pass
* no new compiler warnings attributable to session
* secrets not present
* UI remains responsive
* Git diff reviewed
* CURRENT_STATE updated
* unfinished items explicitly recorded

"Code written" is not Done.

---

# 54. Delivery Plan

Implementation is divided into controlled sessions.

## Phase 0 — Foundation

Session 01 — Repository & solution foundation
Session 02 — Architecture/domain foundation
Session 03 — SQL Server LocalDB persistence
Session 04 — Provider feasibility investigation

## Phase 1 — User Experience Foundation

Session 05 — WinUI shell and design system
Session 06 — Dashboard and provider cards
Session 07 — Tray + Focus HUD

## Phase 2 — Provider Integrations

Session 08 — Codex
Session 09 — Claude
Session 10 — Kimi
Session 11 — Copilot
Session 12 — Antigravity

## Phase 3 — Product Intelligence

Session 13 — Subscriptions
Session 14 — History and analytics
Session 15 — Recommendation engine
Session 16 — Notifications and monitoring

## Phase 4 — Production Readiness

Session 17 — Settings/security/resilience
Session 18 — Performance and UX polish
Session 19 — Packaging/CI/release
Session 20 — Final stabilization

---

# 55. Execution Rule

Sessions MUST execute sequentially unless the BRD explicitly permits otherwise.

Do not implement Session 08–12 before Session 04 has produced a verified provider capability matrix.

This prevents speculative provider implementation.

---

# 56. SESSION PROMPTS

The following prompts are intended for Luna, Terra, or Sonnet.

Each executor should work directly inside the repository.

---

## SESSION 01 — Repository Foundation

### Prompt

You are the implementation executor for AI Usage Monitor.

Read completely before modifying anything:

1. `AGENTS.md` if present.
2. `docs/BRD.md`.
3. `.ai/EXECUTION_PLAN.md` if present.
4. `.ai/CURRENT_STATE.md` if present.
5. Inspect Git status.

Execute Session 01 only.

OBJECTIVE:

Create the clean production foundation for the AI Usage Monitor GitHub repository.

Approved stack:

* C#
* .NET 10
* WinUI 3
* Windows App SDK
* EF Core 10
* Microsoft SQL Server LocalDB
* MVVM
* dependency injection
* Serilog
* xUnit for targeted tests

Create the solution structure defined in the BRD.

Create:

* Desktop project
* Domain project
* Application project
* Infrastructure project
* Providers project
* Domain test project
* Provider test project

Configure project references correctly.

Add:

* `.editorconfig`
* `Directory.Build.props`
* appropriate `.gitignore`
* minimal README
* `AGENTS.md`
* `docs/BRD.md` if not already present
* `.ai/CURRENT_STATE.md`
* `.ai/EXECUTION_PLAN.md`
* `.ai/DECISIONS.md`
* `.ai/PROMPTS.md`

Do not implement business features.

Ensure a WinUI application can build and launch.

Configure nullable reference types and modern C# conventions.

Do not add unnecessary packages.

Run restore/build.

Review final Git diff.

Update `.ai/CURRENT_STATE.md` with exactly what was created, what was validated, and the next session.

Stop after Session 01.

---

## SESSION 02 — Domain & Application Architecture

### Prompt

Read `AGENTS.md`, `docs/BRD.md`, `.ai/CURRENT_STATE.md`, `.ai/EXECUTION_PLAN.md`, relevant decisions, and inspect Git status.

Execute Session 02 only.

OBJECTIVE:

Implement the domain model and provider-independent application contracts.

Create clean domain concepts for:

* Provider
* ProviderConnection
* ProviderAccount
* Subscription
* QuotaDefinition
* QuotaWindow
* UsageSnapshot
* AlertRule
* SyncEvent
* provider status
* quota type
* quota unit
* data source
* confidence level

Create the provider abstraction required by the BRD.

The domain must support arbitrary quota windows.

Do NOT hardcode the entire system around 5-hour and weekly limits.

Implement application-level service interfaces for:

* provider discovery
* provider refresh
* usage aggregation
* subscription management
* snapshot persistence
* alert evaluation

No provider-specific parsing yet.

No UI redesign.

Add targeted tests for important domain invariants and normalization.

Build and test.

Review diff.

Update CURRENT_STATE.

Stop.

---

## SESSION 03 — SQL Server LocalDB

### Prompt

Execute Session 03 only after reading the project governance files and inspecting existing implementation.

OBJECTIVE:

Implement the persistence layer using:

* EF Core 10
* SQL Server LocalDB
* Microsoft.Data.SqlClient

Implement the BRD data model.

Create migrations.

Implement automatic development database initialization appropriately.

Do not store credentials or tokens.

Implement repositories/services necessary for:

* providers
* connections
* subscriptions
* quota definitions
* snapshots
* alert rules
* alert events
* sync events
* settings

Prevent unnecessary duplicate UsageSnapshots when provider values have not changed.

Implement graceful handling when LocalDB is unavailable and provide a user-readable prerequisite error path.

Add targeted persistence validation.

Build/test.

Update CURRENT_STATE.

Stop.

---

## SESSION 04 — Provider Capability Investigation

### Prompt

This is a feasibility and evidence session.

Do NOT blindly implement providers.

Read project governance and BRD.

Investigate the actual currently installed/authenticated environment for:

* Codex
* Claude
* Kimi
* GitHub Copilot
* Google Antigravity

For each provider determine:

1. local installation detection method
2. authentication mechanism
3. safe available local metadata
4. official command/API capable of returning usage
5. quota windows actually exposed
6. remaining/used percentage availability
7. reset timestamp availability
8. subscription plan availability
9. subscription date availability
10. whether external network access is necessary
11. expected authentication expiration behavior
12. safest fallback method

Do not extract browser cookies.

Do not expose tokens.

Do not commit raw credentials.

If sample provider payloads are useful for future parsers, sanitize them completely before committing fixtures.

Update `.ai/DECISIONS.md` with the verified capability matrix.

For every field clearly classify:

* VERIFIED
* NOT AVAILABLE
* REQUIRES MANUAL FALLBACK
* REQUIRES FURTHER INVESTIGATION

Do not claim unsupported APIs exist.

Only create small investigation utilities if necessary.

No large provider implementation yet.

Build existing solution before finishing.

Update CURRENT_STATE.

Stop.

---

# REVIEW GATE A — OPUS 5

After Session 04, give Opus 5:

### Reviewer Prompt

Act as principal architect and adversarial reviewer.

Read:

* AGENTS.md
* docs/BRD.md
* .ai/CURRENT_STATE.md
* .ai/EXECUTION_PLAN.md
* .ai/DECISIONS.md
* all source code
* test code
* Git history/diff where useful

Review Sessions 01–04.

Do NOT implement features unless fixing a blocking defect is explicitly requested.

Focus on:

* architecture correctness
* dependency direction
* provider abstraction
* dynamic quota design
* MSSQL design
* credentials/security
* feasibility claims
* undocumented provider assumptions
* unnecessary complexity
* missing requirements
* future maintainability

Classify findings:

BLOCKER
HIGH
MEDIUM
LOW

Explicitly state whether implementation may proceed to Session 05.

Do not approve unless provider feasibility findings are evidence-based.

---

# SESSION 05 — WinUI Design System

### Prompt

Execute Session 05.

OBJECTIVE:

Create the modern production-quality WinUI 3 visual system for AI Usage Monitor.

Follow BRD usability requirements.

Implement:

* application shell
* navigation
* Mica backdrop
* dark/light/system theme support
* typography hierarchy
* spacing system
* card system
* quota progress controls
* provider icon/avatar treatment
* warning/critical visual states
* skeleton/loading state
* empty/error/stale states

Visual direction:

Modern developer control center.

Fancy but restrained.

Use subtle motion only.

Prioritize quota readability over decoration.

Do not populate fake production values. Use clearly identified design-time/sample data where necessary.

Do not implement providers.

Build and manually verify layout.

Update CURRENT_STATE.

Stop.

---

# SESSION 06 — Dashboard

### Prompt

Execute Session 06 only.

OBJECTIVE:

Implement the primary dashboard.

Include:

* overall capacity summary
* best-capacity provider
* next-reset summary
* provider card grid
* dynamic quota rows
* reset countdown
* connection status
* stale indicator
* manual Refresh All
* synchronization progress
* insight panel
* Copilot-style credit-only card support

Cards must adapt to arbitrary quota counts.

Do not assume every provider has 5-hour + weekly limits.

Bind UI to application services, not provider-specific parsers.

Ensure resizing behaves correctly.

Optimize for quick glance while working.

Build and manually validate.

Update CURRENT_STATE.

Stop.

---

# SESSION 07 — Tray & Focus HUD

### Prompt

Execute Session 07.

OBJECTIVE:

Implement productivity-oriented desktop behavior.

Add:

* system tray presence
* minimize-to-tray
* tray menu
* Focus HUD compact monitor
* always-on-top option
* remembered position
* remembered size/mode
* adjustable opacity if practical
* manual refresh from HUD
* open main dashboard action

Do not let closing the dashboard accidentally terminate background monitoring unless Exit is explicitly selected.

Ensure there is always a clear way to exit the application.

Keep Focus HUD extremely readable and compact.

Build/manual validate.

Update CURRENT_STATE.

Stop.

---

# SESSION 08 — Codex Provider

### Prompt

Execute the Codex provider using ONLY verified Session 04 findings.

Do not invent APIs or file formats.

Implement:

* detection
* connection state
* verified safe usage collector(s)
* normalization
* actual quota windows exposed to this account
* reset timestamps when available
* plan/account metadata when available
* manual fallback where unavailable
* stale/error handling

Do not hardcode 5-hour + weekly if Codex does not expose those actual windows.

Persist normalized snapshots.

Add sanitized parser fixtures where appropriate.

Add focused parser/normalization tests.

Integrate with dashboard.

Build/test/manual validate against the real local environment without exposing credentials.

Update CURRENT_STATE.

Stop.

---

# SESSION 09 — Claude Provider

### Prompt

Execute Claude integration using verified capability findings.

Target:

* local Claude detection
* connected account status
* current five-hour/session usage
* five-hour reset
* weekly usage
* weekly reset
* additional model-specific quota where actually exposed
* usage credits state where safely available
* plan information if available

Never scrape browser credentials.

Normalize all Claude data into generic quota objects.

Handle authentication expiration and stale data.

Persist snapshots.

Integrate dashboard and Focus HUD.

Add focused tests using sanitized fixtures.

Build/test/manual verify.

Update CURRENT_STATE.

Stop.

---

# SESSION 10 — Kimi Provider

### Prompt

Execute Kimi integration using verified mechanisms.

Target when available:

* Kimi installation/account
* plan
* rolling five-hour quota
* weekly quota
* monthly membership credits
* reset timestamps
* Extra Usage state

Prefer official CLI/API/console-supported mechanisms verified in Session 04.

Do not implement browser cookie extraction.

Normalize data.

Persist snapshots.

Integrate UI.

Handle missing/partial information.

Build/test/manual validate.

Update CURRENT_STATE.

Stop.

---

# SESSION 11 — GitHub Copilot

### Prompt

Execute GitHub Copilot provider.

Use GitHub-supported authentication only.

Target:

* GitHub identity
* Copilot subscription/plan where exposed
* GitHub AI Credits usage/remaining
* billing/reset information where legitimately exposed

Do NOT create fake 5-hour or weekly limits.

If personal-account endpoints do not expose a requested field, mark the field unavailable and support manual subscription configuration instead of scraping it.

Secure authentication tokens using approved Windows secret storage.

Integrate provider card.

Build/test.

Update CURRENT_STATE.

Stop.

---

# SESSION 12 — Antigravity

### Prompt

Execute Antigravity provider using only mechanisms verified in Session 04.

Target:

* installation detection
* connection status
* Google AI plan where available
* baseline quota
* 5-hour window where applicable
* weekly quota
* reset timestamps
* overage/AI-credit state where available

Do not reverse engineer or extract unsafe credentials solely to obtain additional data.

If direct programmatic access is not reliably available, provide the best safe partial integration plus manual fallback.

Normalize quotas.

Persist snapshots.

Integrate dashboard.

Build/test/manual verify.

Update CURRENT_STATE.

Stop.

---

# REVIEW GATE B — OPUS 5

### Prompt

Review all five provider implementations adversarially.

Check specifically for:

* invented endpoints
* brittle scraping
* credential exposure
* cookie extraction
* unsafe token persistence
* provider-specific logic leaking into the UI
* incorrect percentages
* reversed used/remaining values
* incorrect reset time zones
* duplicate snapshots
* poor error handling
* fabricated subscription information

Verify each provider against the capability matrix.

Report BLOCKER/HIGH/MEDIUM/LOW.

Approve or reject Phase 3.

---

# SESSION 13 — Subscription Management

### Prompt

Implement the subscription-management domain and UI.

Support:

* original subscription start
* billing period start
* billing period end
* renewal date
* cancellation
* auto-renew
* price
* currency
* billing cadence
* source/confidence

Automatically populate only verified provider data.

Allow missing values.

Allow manual override.

Clearly differentiate manual information from provider-sourced information.

Add Subscription view and compact information on Provider Details.

Do not implement payment functionality.

Build/test.

Update CURRENT_STATE.

Stop.

---

# SESSION 14 — History & Analytics

### Prompt

Implement usage history and analytics.

Required:

* history retrieval
* 24h / 7d / 30d / billing-cycle views
* usage change
* burn rate
* reset event identification
* trend indicators
* estimated exhaustion only when mathematically justified
* estimated remaining-at-reset

Never invent values across missing data.

Visually represent stale/gap periods.

Keep charts readable and performant.

Build/test analytics carefully.

Update CURRENT_STATE.

Stop.

---

# SESSION 15 — Capacity Recommendation Engine

### Prompt

Implement the deterministic capacity recommendation engine.

Do not call an AI model.

Create a transparent scoring algorithm based on:

* current remaining quota
* relevant shortest window
* weekly capacity
* monthly capacity
* reset proximity
* consumption velocity
* provider availability
* stale-data confidence
* exhaustion

The algorithm shall compare capacity only.

It must not claim one AI model is smarter or better at a task.

Provide:

* overall score
* reason summary
* recommended provider for extended usage
* warning for constrained providers

Unit test scoring and edge cases.

Integrate dashboard insight panel.

Update CURRENT_STATE.

Stop.

---

# SESSION 16 — Monitoring & Notifications

### Prompt

Implement background monitoring.

Required:

* configurable periodic refresh
* default approximately 60 seconds
* refresh on startup
* refresh after resume
* foreground refresh where reasonable
* backoff on failures
* provider isolation
* notification deduplication

Implement alerts:

* warning threshold
* critical threshold
* quota exhausted
* quota reset
* authentication failure
* stale provider
* upcoming subscription renewal

Default remaining thresholds:

Warning 30%
Critical 15%

Make thresholds configurable.

Ensure monitor itself does not cause aggressive provider traffic.

Build/test.

Update CURRENT_STATE.

Stop.

---

# SESSION 17 — Settings, Security & Resilience

### Prompt

Execute a production-hardening pass focused on configuration and security.

Implement Settings for:

* enabled providers
* refresh interval
* start with Windows
* theme
* Focus HUD
* notification thresholds
* provider configuration
* manual subscriptions

Review credential handling end-to-end.

Ensure logs redact tokens.

Ensure configuration never contains secrets.

Add structured provider diagnostic logging.

Add safe recovery for:

* LocalDB unavailable
* provider unavailable
* auth expired
* malformed provider payload
* network offline
* provider timeout
* database transient error

One provider failure must never break another.

Build/test.

Update CURRENT_STATE.

Stop.

---

# SESSION 18 — UX & Performance Polish

### Prompt

Perform a focused UX/performance stabilization pass.

Do not introduce new product scope.

Review:

* dashboard readability
* information density
* provider card consistency
* typography
* hover/focus states
* keyboard navigation
* animations
* resizing
* Focus HUD
* tray behavior
* loading states
* empty states
* stale states
* error states
* high-DPI scaling
* dark/light modes

Profile obvious performance issues.

Eliminate unnecessary database/network refreshes.

Ensure no operation blocks the WinUI UI thread.

Do not redesign business logic.

Build/test/manual regression.

Update CURRENT_STATE.

Stop.

---

# REVIEW GATE C — OPUS 5

### Prompt

Perform a complete pre-release design and architecture review.

Review:

* business requirements coverage
* architecture
* provider correctness
* database
* security
* WinUI implementation
* MVVM boundaries
* UX
* accessibility
* performance
* resilience
* background monitoring
* notifications
* analytics
* recommendation logic

Create a release-blocker list.

Do not propose speculative features.

Focus only on what prevents this personal tool from being reliable and pleasant to use.

---

# SESSION 19 — Packaging, CI & GitHub Release

### Prompt

Prepare the application for repeatable personal installation.

Implement:

* Release build configuration
* GitHub Actions build
* test execution
* WinUI packaging appropriate to the application
* version metadata
* LocalDB prerequisite handling/documentation
* installation instructions
* upgrade-safe database migrations
* Release artifact production

Do not introduce cloud infrastructure.

Do not introduce Jira.

Do not require an AI Usage Monitor online account.

Create concise README setup instructions.

Build the exact Release artifact locally if possible and validate startup.

Update CURRENT_STATE.

Stop.

---

# SESSION 20 — Final Stabilization

### Prompt

This is the final implementation session.

Do not add new features.

Read the entire BRD and compare every V1 requirement against the implementation.

Create a requirement-compliance checklist.

Resolve remaining BLOCKER and HIGH issues.

Validate:

* fresh launch
* existing LocalDB
* no LocalDB
* all providers enabled
* provider disabled
* offline startup
* stale provider
* auth expired
* quota reset
* subscription manual entry
* Focus HUD
* tray
* dashboard
* history
* alerts
* recommendation engine
* application restart
* Windows startup behavior
* release packaging

Run final targeted tests.

Review entire task-related Git diff/history.

Clean dead code.

Clean debug artifacts.

Ensure there are no secrets.

Update CURRENT_STATE to RELEASE CANDIDATE.

Stop.

---

# FINAL REVIEW — OPUS 5

### Prompt

You are the final independent reviewer for AI Usage Monitor v1.0.

Treat the implementation as a release candidate.

Read:

* AGENTS.md
* full BRD
* architecture decisions
* current state
* complete source tree
* provider implementations
* tests
* build configuration
* GitHub Actions
* packaging
* relevant Git history

Perform a final adversarial release review.

For every BRD requirement classify:

PASS
PARTIAL
FAIL
NOT APPLICABLE

Audit especially:

1. fabricated provider usage
2. inaccurate quota interpretation
3. used-vs-remaining inversion
4. incorrect resets/time zones
5. insecure credentials
6. browser cookie handling
7. secret logging
8. LocalDB reliability
9. provider isolation
10. UI thread blocking
11. stale data behavior
12. notification spam
13. history calculation
14. forecast correctness
15. recommendation correctness
16. Windows lifecycle behavior
17. packaging
18. upgrade/migration safety

Output:

* Release verdict
* Blockers
* High findings
* Medium findings
* Low findings
* Requirement compliance
* Security verdict
* Architecture verdict
* UX verdict
* Provider reliability verdict

Verdict must be exactly one:

READY FOR PERSONAL PRODUCTION USE

or

NOT READY FOR PERSONAL PRODUCTION USE

Do not approve conditionally if blockers remain.

---

# 57. Acceptance Criteria for V1

V1 is accepted only when:

### Core

* App launches reliably.
* SQL LocalDB initializes.
* Existing history survives restart.
* UI remains responsive.

### Providers

* All five providers have working detection.
* Available usage is displayed accurately.
* Unsupported information displays N/A.
* No fabricated quota.
* Reset timestamps are correct.
* Partial provider failures are isolated.

### Dashboard

* Remaining quota visible immediately.
* Reset countdown readable.
* Best-capacity recommendation visible.
* No ambiguity between used and remaining.

### Focus HUD

* Can remain visible while working.
* Does not interfere significantly with IDE workflow.
* Position persists.
* Information remains readable.

### Subscription

* Dates supported.
* Manual fallback supported.
* Renewal semantics are clear.

### History

* Snapshots persist.
* No excessive duplicates.
* Trends calculated correctly.

### Security

* No plaintext credentials.
* No committed credentials.
* No browser cookie extraction.
* Logs redact secrets.

### Alerts

* Threshold alerts work.
* Exhaustion alert works.
* Reset alert works.
* Alerts do not spam.

### Production

* Release build succeeds.
* GitHub CI succeeds.
* Installation process documented.
* No BLOCKER/HIGH reviewer findings remain.

---

# 58. Future Backlog — Not V1

Potential later additions:

### V1.1

* mini graph inside Focus HUD
* custom provider ordering
* configurable global hotkey
* usage export CSV
* subscription cost dashboard

### V1.2

* cost-per-use analysis
* monthly subscription cost comparison
* estimated wasted subscription capacity
* "you are underusing this subscription" insight

### V2

Potential optional:

```
ASP.NET Core 10 API
      +
Angular 22
```

Only if remote access becomes useful.

Example:

```
Desktop PC
    ↓
optional sync API
    ↓
Angular dashboard
    ↓
phone/laptop browser
```

This is deliberately excluded from the current architecture.

---

# 59. Final Architectural Decision

The V1 architecture is frozen as:

```
Windows
   │
   ▼
WinUI 3
   │
   ▼
Application Layer
   │
   ├─────────────┐
   │             │
   ▼             ▼
Domain        Provider Engine
                 │
   ┌─────────────┼─────────────┐
   │             │             │
 Codex        Claude         Kimi
   │             │             │
   └───── Copilot / Antigravity
                 │
                 ▼
             Normalizer
                 │
                 ▼
          Application Services
                 │
          ┌──────┴───────┐
          ▼              ▼
      EF Core 10      Analytics
          │
          ▼
   SQL Server LocalDB
```

Secrets:

```
Windows Credential Manager
```

Source:

```
GitHub
```

Planning:

```
BRD + repository state
```

Executor:

```
Luna / Terra / Sonnet
```

Reviewer:

Opus 5


No Jira.

No cloud backend.

No Angular in V1.

No unnecessary accounts.

No speculative provider APIs.

---

# 60. Planning Authority

When an executor finds a conflict between:

1. its own preferred implementation
2. existing code
3. BRD

the priority is:

```
BRD
 ↓
explicit architectural decisions
 ↓
current implementation plan
 ↓
existing implementation
 ↓
executor preference
```

If a BRD requirement is genuinely impossible because a provider does not expose the necessary information, the executor must:

1. document evidence
2. implement the safest available fallback
3. mark the capability Partial or Manual
4. never fabricate the requested data

---

# 61. Product Success Definition

The project succeeds when, while actively working in an IDE, the user can glance at one compact panel and confidently determine:

> "I have enough capacity in Codex, Claude, Kimi, Copilot, or Antigravity to continue this session, and I know when the constrained providers become available again."

Everything else is secondary.
