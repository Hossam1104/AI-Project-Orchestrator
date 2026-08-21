# AI Project Orchestrator - Implementation Plan

**Version:** 1.1
**Date:** 21 August 2026
**Product:** AI Project Orchestrator (APO)
**Primary Requirements:** `docs/BRD.md`
**Jira Project:** `APO`
**Repository:** `https://github.com/Hossam1104/AI-Usage-Monitor-Tool`
**Default Branch:** `main`
**Planner / Architect / Acceptance Authority:** GPT-5.6 Sol
**Substantial Executor:** GPT-5.6 Luna Max
**Bounded Executor:** Claude Sonnet 5
**Independent Reviewer:** Claude Opus 5
**Optional Security Specialist:** GPT-5.6 Terra HIGH
**Auxiliary Executor:** Gemini 3.7

---

## 1. Purpose and Authority

This plan explains how APO will be delivered from the existing repository. It is subordinate to
`docs/BRD.md` and does not create product scope by itself. Jira `APO` is the authoritative
work-tracking system; repository documentation and evidence remain the architecture/governance
source of truth.

Authority order for execution is:

1. `docs/BRD.md`;
2. `AGENTS.md`;
3. planner-approved architecture decisions and `.ai/CURRENT_STATE.md`;
4. assigned Jira Story/Task acceptance scope;
5. this plan;
6. root `TASK.md`; and
7. executor preference.

The active execution flow is:

```text
Jira Epic -> Jira Story/Task -> Sol planning/architecture
-> TASK.md execution contract -> assigned executor -> validation
-> independent review where required -> Sol acceptance -> Jira/Git synchronization
```

Only one bounded assigned work item is active at a time. Detailed Stories/Tasks are progressively
decomposed by Sol after repository evidence and acceptance dependencies are understood. This plan
does not invent a future Story list.

---

## 2. APO Epic Capability Structure

The approved initial Jira capability map is:

| Epic | Capability | Primary dependency |
|---|---|---|
| APO-1 | APO Product Rebrand & Governance Rebaseline | None; current checkpoint |
| APO-2 | Windows Platform & Application Foundation | APO-1 |
| APO-3 | Local Persistence, Resilience & Security Foundation | APO-1, APO-2 |
| APO-4 | AI Usage, Subscription & Capacity Monitoring | APO-3 |
| APO-5 | Project Registry & Workspace Management | APO-2, APO-3 |
| APO-6 | Git & GitHub Integration | APO-2, APO-5 |
| APO-7 | Jira & Azure DevOps Work-Item Integration | APO-5, APO-6 |
| APO-8 | AI Agent / Model Registry & Connectivity | APO-3, APO-4 |
| APO-9 | Intelligent Model Routing & Quota-Aware Decisioning | APO-4, APO-8 |
| APO-10 | Planning & Execution Contracts | APO-5, APO-8, APO-9 |
| APO-11 | Autonomous Execution Runtime | APO-6, APO-7, APO-8, APO-10 |
| APO-12 | Validation & Evidence Engine | APO-6, APO-10, APO-11 |
| APO-13 | Independent Review & Remediation Engine | APO-8, APO-11, APO-12 |
| APO-14 | Acceptance & Human Approval Gates | APO-7, APO-12, APO-13 |
| APO-15 | Command Center & Project UX | APO-4, APO-5, APO-11, APO-14 |
| APO-16 | Activity, Audit, History & Notifications | APO-3, APO-4, APO-11, APO-14 |
| APO-17 | Packaging, Compatibility, CI & Release Quality | Cross-cutting; evidence from all Epics |

The Epic list is approved. Sol may refine names, dependencies, and Story ordering only within the
BRD and owner-approved scope. APO-18 is the first Story under APO-1 and is the current governance
boundary; it does not authorize APO-2 or any later Epic.

---

## 3. Legacy Backfill and Reuse Classification

The repository contains completed work from the former AI Usage Monitor scope. That work is not
discarded and is not automatically accepted as APO-complete. Sol will backfill meaningful work into
Jira with links to code, commits, tests, and historical validation, then classify each significant
area as:

- **Reuse As-Is**;
- **Reuse With Extension**;
- **Refactor**;
- **Superseded**; or
- **Remove**.

Backfilled work may be marked Done only after the requirement mapping, code mapping, architecture
check, and relevant validation evidence are explicit. Historical work must retain its original
truth even when the architecture later changed.

Known historical implementation and initial interpretation:

| Historical implementation | Current interpretation | Initial treatment |
|---|---|---|
| Repository/solution foundation | APO-2 platform foundation | Reuse / verify |
| Domain/Application foundation | APO-2/APO-3 contracts | Reuse With Extension |
| Domain integrity remediation | APO-3 correctness | Reuse / verify |
| EF Core + SQL Server LocalDB | Earlier persistence baseline | Superseded; history only |
| WinUI / Windows App SDK shell | Earlier desktop baseline | Superseded; history only |
| Portable WPF migration | APO-2 desktop foundation | Reuse / revalidate |
| JSON/JSONL stores | APO-3 and APO-16 persistence | Reuse With Extension |
| Atomic writes/corruption recovery | APO-3 resilience | Reuse / revalidate |
| Startup/storage resilience | APO-2/APO-3 reliability | Reuse / revalidate |
| Latest/range JSONL optimization | APO-16 history | Reuse / revalidate |
| Interrupted-tail handling | APO-3/APO-16 resilience | Reuse / revalidate |
| x86/x64/ARM64 solution targets | APO-2/APO-17 compatibility | Reuse / revalidate |
| Self-contained publish profiles | APO-17 release foundation | Reuse / revalidate |
| Provider-independent quota concepts | APO-4 capacity domain | Reuse With Extension |
| Provider-only feasibility sequence | APO-4 discovery | Superseded; replan |

The current source is a reusable foundation, not a fully compliant APO product. Source-code mapping
and refactoring have not started under APO-18.

---

## 4. Existing Foundation and Active Architecture

The active target is WPF + .NET 10 + MVVM + modular clean architecture with JSON/JSONL persistence,
secure external credentials, and self-contained Windows artifacts. V1 has no database engine or
ORM. The intended dependency direction is:

```text
Desktop/WPF -> Application -> Domain
Infrastructure -> Application/Domain contracts
Providers/integrations -> Application/Domain contracts
```

The current source already contains a WPF desktop foundation, provider-independent Domain and
Application contracts, JSON/JSONL Infrastructure stores, resilience behavior, focused tests, target
configuration, and publish profiles. Existing `AIUsageMonitor` project/namespace names and the
minimal shell are retained until a future approved mapping/refactor item.

The historical EF/LocalDB and WinUI/Windows App SDK implementations are preserved in Git history
and current-state evidence only. They are not active APO runtime dependencies and must not be
revived without an explicit architecture decision.

---

## 5. Delivery Approach and Dependencies

Delivery proceeds in capability families, but actual work is always driven by one assigned Jira
Story/Task and its Sol-authored `TASK.md` contract.

### Phase A - Governance and evidence boundary

Complete APO-1 governance, consolidate the BRD, stop the old Session 04 prompt, and establish Jira
traceability. Then Sol performs repository-to-Jira legacy mapping and defines the next Story. No
source refactoring occurs before this checkpoint.

### Phase B - Foundation and local safety

Validate and extend APO-2 Windows/application foundation and APO-3 local persistence, resilience,
and credential security. Preserve cross-Windows behavior, schema/version contracts, safe writes,
corruption isolation, and zero-prerequisite consumer deployment.

### Phase C - Inputs and capacity

Build APO-4 monitoring/subscriptions/capacity, APO-5 project registry, APO-6 Git/GitHub, and APO-7
Jira/Azure DevOps integration. Each integration requires truthful capabilities, least privilege,
project isolation, explicit failures, and evidence-backed synchronization.

### Phase D - Planning and execution

Build APO-8 agent/model registry/connectivity, APO-9 routing, APO-10 contracts, and APO-11 bounded
execution. Routing is quality/risk first; execution is cancellable, bounded, auditable, and never
silently crosses project or approval boundaries.

### Phase E - Evidence, review, and acceptance

Build APO-12 validation/evidence, APO-13 independent review/remediation, and APO-14 final
acceptance/human gates. Review remains independent from implementation, and high-risk actions stop
for owner approval.

### Phase F - UX, history, and release

Build APO-15 command-center UX and APO-16 audit/history/notifications while preserving dynamic
capacity display, stale/partial/error states, lightweight WPF behavior, and readable accessibility.
Complete APO-17 packaging, compatibility, CI, and release quality only with evidence-backed claims.

No family is considered complete merely because code exists. Acceptance requires the Jira scope,
BRD requirements, validation, review, and Sol decision to align.

---

## 6. Technical Guardrails

### Persistence and security

Use per-user LocalAppData; JSON for small documents; monthly JSONL for append-oriented records;
schema/record metadata; atomic temporary-file replacement; synchronized writes; streaming/range
history; material-change duplicate suppression; explicit missing/empty/corrupt/unsupported/I/O/
permission handling; safe quarantine; and last-known-good retention. Never write secrets to files
or logs; use opaque references to secure storage.

### Provider truth

Prefer official APIs, account surfaces, OAuth/device auth, optional official CLIs, safe verified
local metadata, and manual fallback in that order. Prove used versus remaining semantics, preserve
`DateTimeOffset`, support arbitrary quota windows, and never fabricate plan, reset, subscription,
or credit information.

### Compatibility and consumer release

Target Windows 10 1809/build 17763 and Windows 11 with x86/x64/ARM64 consideration, x64 primary,
guarded modern APIs, graceful WPF fallback, no modern hardware prerequisite, low idle resource use,
and self-contained release artifacts. Do not claim a clean-machine or architecture validation not
actually performed.

### Testing

Prioritize quota normalization, reset/timezone math, provider parsing, subscription semantics,
JSON/schema round trips, JSONL ordering/range/duplicates, atomic writes, corrupt-file recovery,
credential-reference safety, routing/recommendation, gate/state behavior, WPF view-model behavior,
and self-contained publish smoke checks. CI uses sanitized fixtures and no live authenticated calls.

---

## 7. Review, Security, and Release Gates

Every applicable implementation Story defines its validation and review requirements in Jira and
`TASK.md`. At minimum:

1. Sol confirms scope, dependencies, and acceptance criteria before execution.
2. The executor validates the change and records evidence.
3. Opus independently reviews high-value or high-risk implementation.
4. Findings are classified BLOCKER/HIGH/MEDIUM/LOW, fixed in a bounded loop, and revalidated.
5. Sol accepts or rejects the result against the BRD, Jira scope, diff, validation, and review.
6. Human approval is required for protected-branch merges and other high-risk actions unless the
   explicit owner-approved execution contract provides the applicable authorization.
7. Git and Jira are synchronized only after the required gate.

Release review must explicitly check zero-prerequisite deployment, active WPF/JSON/JSONL
architecture, no hidden database/runtime prerequisites, credential safety, project isolation,
provider truthfulness, remaining-capacity semantics, file integrity, Windows compatibility, and
actual packaging evidence.

---

## 8. Definition of Done

A Story/Task is complete only when:

- its assigned scope is implemented without future scope;
- relevant build/tests/manual checks actually ran;
- warnings/errors are explained;
- secrets/debug artifacts are absent;
- the diff and changed-file list are reviewed;
- `.ai/CURRENT_STATE.md` is updated;
- Jira status/evidence is synchronized by the authorized workflow;
- the branch is committed and pushed;
- the work is integrated into `main` under repository policy;
- `origin/main` is fetched and verified;
- the tree is clean; and
- limitations, blockers, and the next planner boundary are explicit.

For documentation-only APO-18, validation is document consistency, stale-reference review,
source-scope confirmation, and baseline restore/build/test checks requested by the Story. No source
code is changed or claimed as APO-refactored.

---

## 9. Current Planning Boundary

APO-18 is the active rebaseline Story under APO-1. On completion, Sol must review the consolidated
BRD and current source, backfill/match legacy implementation to APO Epics, classify reuse, define
dependencies, and prepare the next approved Jira Story and self-contained execution contract.

The next implementation Story must not be guessed here and must not be executed automatically.
