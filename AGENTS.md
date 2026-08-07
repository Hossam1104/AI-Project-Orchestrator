# AGENTS.md — AI Usage Monitor AI Execution Contract

This is the universal execution contract for every AI model working in this repository.

**Repository:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**Local Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**Primary Requirements:** `docs/BRD v1.0.md`  
**Plan:** `docs/IMPLEMENTATION_PLAN.md`  
**Prompts:** `docs/SESSION_PROMPTS.md`  
**Live Handoff:** `.ai/CURRENT_STATE.md`

---

# 1. Roles

## Planner
GPT-5.6 Sol owns:
- BRD planning
- architecture planning
- implementation sequence
- session decomposition
- approved scope changes

## Executors
Terra / Luna / Sonnet execute implementation sessions.

Executor rule:

> One assigned session at a time.

They implement, validate, review their diff, update current state, and stop.

## Reviewer
Opus 5 performs independent review gates.

Reviewer mode does not add new product scope and does not implement fixes unless explicitly asked.

---

# 2. Mandatory Read Order

Before modifying code:

1. Read `AGENTS.md` completely.
2. Read `docs/BRD v1.0.md`.
3. Read `.ai/CURRENT_STATE.md`.
4. Read `docs/IMPLEMENTATION_PLAN.md`.
5. Read the exact assigned section of `docs/SESSION_PROMPTS.md`.
6. Inspect `git status`.
7. Inspect only source/config files relevant to the assigned session.

Do not depend on previous chat context.

The repository is the source of truth.

---

# 3. Authority Order

When instructions conflict:

1. `docs/BRD v1.0.md`
2. `AGENTS.md`
3. approved facts/decisions in `.ai/CURRENT_STATE.md`
4. `docs/IMPLEMENTATION_PLAN.md`
5. assigned session prompt
6. existing implementation
7. executor preference

Do not silently override a higher authority.

---

# 3A. Cross-Windows Compatibility Contract

This section is permanent and mandatory. **Every executor and reviewer session from Session 02 onward inherits it automatically**, even when the individual session prompt does not repeat it in full.

## Baseline

- Minimum OS: Windows 10 version 1809 / build 17763
- Supported OS: Windows 10 1809+ and Windows 11
- Supported architectures: x86, x64, ARM64
- Primary validation architecture: x64 (secondary: x86, then ARM64)

Design principle: **Modern where available. Compatible everywhere supported.**

## Mandatory Rules

1. **API guarding** — No Windows 11-only API, Windows App SDK feature, or post-17763 OS feature may be used without runtime capability/version detection. An unavailable modern feature must never crash the app.
2. **Core independence** — Domain, Application, Provider engine, quota calculation, subscription handling, history, analytics, recommendation engine, monitoring, persistence, and alert evaluation must not depend on Windows 11-only functionality.
3. **UI graceful degradation** — Mica/Acrylic/advanced backdrops/newer windowing or animation effects must fall back to a compatible solid/default surface with identical functionality when unavailable. Visual effects never gate functionality.
4. **No modern hardware prerequisites** — AVX/AVX2, a dedicated/recent GPU, TPM, NPU, an AI accelerator, or a recent-generation CPU must never be mandatory.
5. **Lightweight background operation** — low idle CPU, reasonable memory, no aggressive timers/polling/rendering; must not noticeably degrade the IDE or other running applications.
6. **Functional parity** — provider monitoring, LocalDB persistence, quota display/calculation, subscription info, history, alerts, notifications, tray, Focus HUD, analytics, and recommendations must all work without modern GPU functionality. Only optional visual effects may degrade.

## Dependency Rule

Before adopting any new package, framework dependency, Windows API, or native component beginning with Session 02: check it against the build-17763 minimum. If it would raise the minimum supported OS, do not silently accept it — identify the incompatibility, look for a compatible alternative, record the issue in `.ai/CURRENT_STATE.md`, and escalate to the planner. The executor does not have authority to silently raise the minimum Windows version.

## Review Responsibility

Reviewers (Gate A, Gate B, Gate C, Final Review) must treat a violation of this contract — an unguarded Windows 11-only dependency, a hard hardware-acceleration requirement, or a crash on unavailable modern features — as a classified finding (BLOCKER/HIGH/MEDIUM/LOW per §23), not a stylistic note.

---

# 4. Frozen V1 Stack

- C#
- .NET 10
- WinUI 3
- Windows App SDK
- MVVM
- EF Core 10
- Microsoft SQL Server LocalDB
- Microsoft.Data.SqlClient
- HttpClient
- Microsoft.Extensions.Http.Resilience
- Serilog
- Windows Credential Manager / DPAPI where justified
- GitHub
- GitHub Actions
- targeted xUnit tests

Do not introduce Angular, Electron, Tauri, SQLite, a cloud backend, or Jira unless the planner explicitly changes V1 architecture.

---

# 5. Current State Contract

`.ai/CURRENT_STATE.md` is the **single mutable handoff/status source**.

Every executor session must update it before stopping.

Do not keep duplicate status inside:

- AGENTS.md
- CLAUDE.md
- BRD
- docs/IMPLEMENTATION_PLAN.md
- random handoff files

The next model must be able to continue from the repository without previous chat history.

---

# 6. One Session Only

Execute only the session explicitly assigned.

Do not:

- combine sessions
- continue to the next session automatically
- implement future provider work early
- re-plan the project unless a blocker requires planner involvement

At session end:

1. build
2. test/validate assigned work
3. review Git diff
4. check for secrets
5. update CURRENT_STATE
6. report status
7. stop

---

# 7. Provider Truthfulness

Providers:

- Codex
- Claude
- Kimi
- GitHub Copilot
- Antigravity

Their behavior may change.

Mandatory rules:

- verify current behavior
- prefer official APIs/auth/CLI
- use local metadata only when verified safe
- do not guess endpoints
- do not guess file schemas
- do not assume plan/billing fields are available
- do not fabricate 5h/weekly values
- do not fabricate subscription dates
- do not label inferred data as official

If unavailable, use:

- Not Available
- Manual
- Partial
- Authentication Required
- Stale
- Unsupported

A truthful partial provider is acceptable.

---

# 8. Used vs Remaining Semantics

The UI's primary convention is **remaining capacity**.

For each provider:

- prove source semantics
- normalize once
- keep parsing/normalization separate
- test source-to-domain transformation
- prevent double inversion

Never interpret `used_percent = 80` as `80% available`.

---

# 9. Time & Reset Rules

Use `DateTimeOffset` for provider timestamps.

Preserve source offset where available.

Normalize for calculations safely.

Display reset time in the user's current Windows timezone.

Test:

- midnight
- week boundary
- daylight/timezone conversion where relevant
- reset detection
- source timestamp without explicit offset

Never guess a reset timezone.

---

# 10. Architecture Rules

## Domain
Must not depend on:
- WinUI
- EF Core
- SQL Server
- HTTP
- provider libraries
- Windows UI APIs

## Desktop
Must not:
- parse provider payloads
- inspect provider files directly
- manage tokens directly

## Providers
Own:
- detection
- collection
- parsing
- normalization
- provider-specific errors

## Infrastructure
Owns:
- EF Core/LocalDB
- credential storage
- Windows notifications
- logging infrastructure
- OS integrations where appropriate

Provider failure must be isolated.

---

# 11. Dynamic Quota Rule

Never model the system around fixed quota columns.

Support arbitrary:

- rolling windows
- session limits
- weekly limits
- monthly limits
- AI credits
- token allowances
- model-specific quotas
- custom provider quotas

The UI must adapt to the provider's actual data.

---

# 12. Database Rules

V1 uses Microsoft SQL Server LocalDB.

Rules:

- EF Core migrations
- no secrets
- preserve user history
- do not drop/recreate data as a migration shortcut
- avoid duplicate snapshots when values did not materially change
- handle missing LocalDB gracefully
- upgrades must not destroy history

---

# 13. Security Rules

Never:

- store passwords
- extract browser cookies
- log tokens
- commit tokens
- store provider secrets in LocalDB
- place secrets in appsettings
- commit raw authenticated payloads
- expose sensitive environment variables

Use:

1. official OAuth/device auth
2. Windows Credential Manager
3. DPAPI only if required

Sanitize all diagnostics and test fixtures.

Before completion, inspect changed files for accidental secrets.

---

# 14. Privacy Rules

The monitor needs usage metadata only.

Do not collect/store:

- prompts
- conversations
- provider chat history
- source code
- repository content
- task content

unless a tiny sanitized fixture is required for a parser test.

---

# 15. UI Rules

Visual objective:

**Modern developer command center**

Priorities:

1. remaining quota readability
2. reset readability
3. provider status
4. quick scan
5. visual polish

Use:

- Mica
- dark-first design
- strong typography
- restrained gradients
- rounded cards
- subtle motion
- good spacing
- accessible status text/icons

Avoid:

- neon overload
- huge hero areas
- excessive animation
- marketing-style UI
- color-only meaning
- clutter

Focus HUD must be useful beside an IDE.

---

# 16. Testing Rules

Target tests at high-risk logic:

- quota normalization
- used/remaining conversion
- reset math
- provider parsers
- database persistence
- duplicate snapshot prevention
- secret redaction
- burn-rate math
- recommendation scoring

Do not build thousands of low-value tests.

No live authenticated provider calls in GitHub CI.

Use sanitized fixtures.

Manual WinUI verification is acceptable and must be recorded honestly.

---

# 17. Validation Rules

Before stopping:

- build full solution
- run relevant tests
- perform required manual/provider validation
- review compile warnings caused by session
- review `git diff`
- inspect secrets/debug artifacts
- update `.ai/CURRENT_STATE.md`

Never claim a command/test was run if it was not.

If the environment blocks validation, document exactly what was blocked and why.

---

# 18. Git Safety

Before editing, inspect `git status`.

Never:

- destroy unrelated user changes
- hard reset without explicit instruction
- blindly delete untracked files
- rewrite unrelated history

Keep session changes isolated.

Repository remote should target:

`https://github.com/Hossam1104/AI-Usage-Monitor-Tool.git`

Default branch:

`main`

---

# 19. Dependency Discipline

Before adding a NuGet package:

- prove it is needed
- prefer .NET/Microsoft/Windows App SDK capabilities
- avoid duplicate packages solving the same concern
- prefer maintained packages
- record significant dependency decisions in CURRENT_STATE

Do not replace approved technologies because another framework is personally preferred.

---

# 20. Error Handling

When refresh fails:

- retain last-known valid value
- mark stale/error
- show last successful update
- expose concise diagnostic detail
- do not replace values with zero
- do not crash other providers

---

# 21. Scope Control

Do not add:

- AI chat
- prompt runner
- model auto-switch
- AI-based recommendation calls
- cloud sync
- mobile app
- Angular dashboard
- organization/team billing
- payment workflows
- unrelated productivity features

---

# 22. Executor Completion Report

End every executor session with:

```text
Session:
Status: COMPLETE / PARTIAL / BLOCKED

Implemented:
- ...

Validated:
- ...

Not validated:
- ...

Blockers / limitations:
- ...

Files/areas changed:
- ...

CURRENT_STATE updated: Yes

Next planned session:
- ...
```

---

# 23. Reviewer Rules

Opus 5 review severity:

- BLOCKER — unsafe, fundamentally wrong, or prevents release/progression
- HIGH — major correctness/security/reliability issue
- MEDIUM — important but safely deferrable
- LOW — minor maintainability/polish

Reviewer must inspect actual code/evidence, not trust executor summaries.

A polished UI does not compensate for unreliable quota data.

---

# 24. Core Product Standard

The application must be trustworthy enough that the user can glance at it while working and decide which paid AI subscription has sufficient capacity.

Accuracy and explicit uncertainty always outrank visual symmetry.
