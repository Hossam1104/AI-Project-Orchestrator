# AI Project Orchestrator — Strategic Orchestration Roadmap

## 1. Purpose

This roadmap is the concise bridge between the approved BRD, the implementation plan, Jira
Stories, and the owner experience APO is being built to provide. It records the strategic
direction and sequencing after the accepted APO foundation; it is not a runtime feature claim and
does not authorize a new Story. Each implementation boundary still requires a Sol-authored
`TASK.md` contract.

## Current status (29 August 2026)

APO-38 through APO-46 and APO-68 are implemented and marked Done in Jira. APO-47 through APO-63
remain the bounded strategic backlog, with APO-48 still To Do and not started. APO-69 is the active
repository-wide rebaseline and cleanup handoff on a Draft PR awaiting GPT-5.6 Sol exact-head review.
The live branch, commit, tree, validation, and planner boundary are maintained in
`.ai/CURRENT_STATE.md` and `TASK.md`.

## 2. Product experience we are building

APO is intended to let an owner supervise AI-assisted software delivery from one local-first
command center without replacing the IDE, GitHub, Azure Repos, Jira, Azure Boards, build tools, or
the owner's authority. The owner should not need to reconstruct project state by copying prompts
between models or by trusting an executor's completion message.

The target workflow is:

```text
Open/select project
    ↓
Smart Continue resolves canonical project checkpoint
    ↓
Sol plans and produces versioned execution contract
    ↓
Quality-first / quota-aware routing selects executor
    ↓
Luna xHigh normally performs the bounded implementation
    ↓
APO maintains project-isolated repository/worktree evidence
    ↓
Jira / Azure Boards and GitHub / Azure Repos evidence refreshes
    ↓
Independent build/test/CI/runtime validation
    ↓
Opus review only when cadence/risk requires
    ↓
Sol adjudicates findings
    ↓
Bounded remediation + revalidation
    ↓
Sol acceptance
    ↓
Owner gate for protected/high-risk actions
    ↓
Controlled remote delivery
    ↓
Final independent evidence verification
    ↓
Decision/audit history + next safe checkpoint
```

The end-to-end orchestration runtime, provider execution, remote SCM evidence, controlled delivery,
and consolidated Mission Control experience remain planned capabilities. APO-43 has delivered the
durable Smart Continue/recovery contract and state boundary; its complete owner-facing experience
remains planned.

## 3. Architectural principles

- Local-first Windows desktop operation remains the V1 foundation: C#/.NET 10, WPF, MVVM, clean
  dependency direction, JSON/JSONL persistence, secure external credentials, and self-contained
  Windows artifacts.
- Quality and risk take precedence over quota savings. Capacity can inform routing but cannot
  override capability, policy, risk, or required review.
- Project identity, context, credentials, repository/worktree state, tracker state, and run state
  remain isolated. No chat transcript is the canonical project record.
- Evidence is independently captured, timestamped, source-labeled, freshness-aware, and explicit
  about limitations. Missing evidence is not success.
- Read-only remote evidence and remote writes are separate capabilities. Controlled delivery must
  fail closed when exact targets, validation, approval, or permissions are stale or changed.
- Official APIs, account surfaces, OAuth/device flows, supported SDKs, and verified local evidence
  are preferred. Browser scraping, cookie extraction, secret persistence, and model CLI claims as
  source-of-truth are prohibited.

## 4. Model operating policy

| Model | Default role |
|---|---|
| GPT-5.6 Sol | Planner, architect, Jira decomposition owner, and acceptance authority |
| Claude Haiku 4.5 | Deterministic low-risk reconnaissance and mechanical work |
| Claude Sonnet 5 Medium | Primary routine bounded implementation executor |
| Claude Sonnet 5 High | Difficult bounded debugging and larger bounded implementation |
| GPT-5.6 Luna xHigh | Architecture-sensitive, cross-cutting, or high-blast-radius execution |
| GPT-5.6 Luna Max | Exceptional implementation escalation only |
| Claude Opus 5 | Independent reviewer at configured cadence and critical checkpoints |
| GPT-5.6 Terra Medium/High | Optional risk-triggered security, concurrency, and data-integrity assurance |

One assigned Jira work item is the maximum active scope for an executor. A roadmap Story is not
implementation authorization, and completing one Story never automatically starts the next.

## 5. Current shipped foundation

The accepted foundation includes the WPF/.NET/JSON/JSONL desktop architecture, secure credential
reference boundaries, provider-independent capacity contracts, project/orchestration storage,
Projects workspace, agent/model registry, progressive onboarding, versioned contracts, dependency
graphs, structured handoffs, durable recovery state, explainable routing, isolated workspace
preparation, bounded cancellable execution, workspace-preparation hardening, and APO-37 read-only
local Git repository verification. APO-37 provides bounded local branch/HEAD/status/remote evidence
for a selected configured path; it does not call a remote SCM service, read repository file contents,
or perform Git writes.

APO-33 remains the existing repository-owned GitHub Actions CI/release Story. Local validation in
this roadmap session is not a GitHub CI result. Provider execution, tracker automation, remote SCM
evidence, controlled delivery, independent review/acceptance engines, and Mission Control are not
shipped by this documentation checkpoint.

## 6. Strategic Jira roadmap

The active strategic roadmap is APO-38 through APO-63 under the approved APO-1 through APO-17
Epics. APO-33 remains a complementary existing CI/release Story.

### P0 control plane — APO-38..43

- APO-38 — Provider-independent agent/model capability and connection truth.
- APO-39 — Progressive project onboarding and canonical context resolution.
- APO-40 — Versioned planning and execution contracts.
- APO-41 — Dependency DAG and safe scheduling.
- APO-42 — Structured planner/executor/reviewer handoffs.
- APO-43 — Persist canonical context, Smart Continue, and recovery checkpoints.

### P0 bounded execution — APO-44..47

- APO-44 — Quality-first, quota-aware routing.
- APO-45 — Bounded execution.
- APO-46 — Isolated worktrees/workspaces.
- APO-47 — Jira/Azure Boards tracker integration.

### P0 source-control/tracker/evidence — APO-62, APO-48, APO-49, APO-63

- APO-62 — Provider-independent, read-only GitHub/Azure Repos remote SCM and CI evidence.
- APO-48 — Independent QA and evidence gates.
- APO-49 — Human approval policy.
- APO-63 — Controlled remote source-control delivery operations.

### P0 Mission Control — APO-50

APO-50 consolidates active work, roles, blockers, approvals, repository/tracker state, evidence,
and owner attention into an evidence-backed command-center read model and surface.

### P1 acceleration — APO-51..56

Review Inbox, skills/workflow library, explainable project health, Decision Ledger/activity
history, runtime evidence, and context budgets/compression.

### P2 controlled expansion — APO-57..58

Bounded background automation/housekeeping and an optional remote/mobile approval security design.

### P3 accepted hardening — APO-59..61

APO-37 local evidence/output bounds, verification deadline/path truthfulness, and real-Git
availability evidence semantics.

### Canonical hard dependency DAG

Jira `Blocks` records only real architectural prerequisites. The repaired strategic graph contains
exactly 18 hard dependencies:

```text
APO-38 -> APO-39                 APO-38 -> APO-44
APO-40 -> APO-41                 APO-40 -> APO-42
APO-40 -> APO-43                 APO-40 -> APO-45
APO-39 -> APO-43
APO-41 -> APO-45                 APO-42 -> APO-45
APO-43 -> APO-45                 APO-44 -> APO-45
APO-46 -> APO-45
APO-45 -> APO-48
APO-48 -> APO-63                 APO-49 -> APO-63
APO-62 -> APO-63
APO-45 -> APO-57                 APO-49 -> APO-58
```

The arrows mean the left Story blocks the right Story. APO-46 intentionally precedes APO-45 because
project-isolated repository/worktree safety must be established before autonomous implementation.
The accepted APO-37 traceability links to APO-59, APO-60, and APO-61 are `Relates`, not hard
dependencies. Other useful ordering relationships are planner guidance and are not Jira `Blocks`
links unless a future contract proves a real prerequisite.

## 7. Integration boundaries

### Local Git — APO-37

APO-37 is the local, read-only repository verification slice. It owns local path/repository
inspection, bounded worktree evidence, cancellation, project identity checks, and safe unavailable
states. It does not imply remote reachability, pull-request state, CI success, or permission to
write.

### Tracker awareness — APO-47

Jira and Azure Boards are provider-independent work-item inputs where configured. Tracker identity,
keys, status, and evidence links remain auditable and project-isolated. A model or tracker CLI is
not itself proof of repository or CI state.

### Remote SCM / CI evidence — APO-62

APO-62 is planned as a read-only provider-independent boundary for configured GitHub and Azure
Repos projects, using official supported integration paths. It may capture repository identity,
branch/commit relationships, pull-request identity/state, reviews, checks/status, CI/workflow
evidence, source/provider, freshness, and immutable target identifiers where exposed. It must
distinguish Not Configured, Authentication Required, Permission Denied, Unsupported, Unavailable,
Stale, Partial, and Available.

### Controlled delivery — APO-63

APO-63 is a separate provider-independent write boundary. Any PR metadata transition, review
coordination, bounded delivery comment, or merge operation must bind to the project, work item,
execution-contract version, repository, base/head refs, exact head SHA, current validation,
approval policy, actor, and audit identity. It fails closed on moved refs, stale approval, missing
evidence, failed validation, changed permissions, changed mergeability, or changed project identity.

## 8. Smart Continue and recovery

APO-43 owns the canonical `Continue project` behavior. Its durable checkpoint includes project/run
context, work-item and dependency state, execution-contract identity/version, selected roles,
routing evidence, repository/tracker references, validation, review, approval, blockers, next safe
action, and checkpoint lifecycle.

Smart Continue must distinguish resumable, blocked, stale/needs fresh evidence, completed, approval
required, and context-insufficient states. It must recover after restart or chat/context loss from
persisted project-isolated state, never from old conversation text treated as current Git, tracker,
CI, or approval evidence.

## 9. Mission Control

APO-50 is the planned command-center read model for the owner. It should make active projects,
current work, model roles, dependency blockers, execution state, validation, review findings,
approval gates, repository/tracker evidence, attention items, and next safe actions visible without
inventing health or completion from missing evidence.

## 10. Evidence, review and delivery safety

The progression chain is: planner contract → bounded execution → independent local/remote
repository and tracker evidence → build/test/CI/runtime validation → configured independent review
→ Sol adjudication → bounded remediation and revalidation → Sol acceptance → owner approval where
required → controlled delivery → final independent verification.

Protected/default-branch merges, production actions, destructive changes, credential/billing
actions, material architecture changes, and other owner-defined high-risk actions remain behind
explicit human gates. No remote write may silently bypass the evidence or approval policy.

## 11. Jira hygiene / roadmap identity

The approved APO-1 through APO-17 Epic structure is reused. The remaining strategic product backlog
is APO-47 through APO-63; APO-38 through APO-46 are delivered. APO-64 through APO-67 are Done, VOID,
`no-project-work`, and `connector-correction` artifacts retained only as transparent Jira connector
history; they have zero product scope and are excluded from roadmap totals, dependencies, sequencing,
BRD claims, and Mission Control scope. APO-68 is delivered workspace-preparation hardening. APO-69
is the current repository rebaseline/cleanup Story and is not a product-runtime Story.

## 12. Near-term ordering

The following compact map is retained as a historical visual only; it is not the Jira DAG or the
authoritative implementation sequence. Use the canonical DAG above and the explicit sequence below.

```text
Accepted APO-38..46 and APO-68 foundation
        ↓
APO-38 → APO-39 → APO-40 → APO-41 → APO-42 → APO-43
        ↓                                  ↘
APO-44 → APO-46 → APO-45                 recovery context
                                  APO-47 + APO-62 evidence integrations
                                         ↓
                              APO-48 QA/evidence gates
                                         ↓
                              APO-49 owner approval policy
                                         ↓
                              APO-63 controlled remote delivery
                                         ↓
                              APO-50 Mission Control
                                         ↓
                              APO-51..56 → APO-57..58
```

Authoritative planner sequence for the remaining backlog:

```text
Accepted APO-38..46 and APO-68 foundation
        v
APO-47 + APO-62 evidence integrations
        v
APO-48 QA/evidence gates
        v
APO-49 owner approval policy
        v
APO-63 controlled remote delivery
        v
APO-50 Mission Control
        v
APO-51..56
        v
APO-57..58
        v
APO-59..61 accepted hardening
```

Read-only remote evidence must precede controlled remote writes because APO cannot safely mutate a
remote target without independently knowing the current repository, ref, review/check, validation,
permission, mergeability, and approval state. The next Story is not selected by this roadmap;
GPT-5.6 Sol must issue a fresh contract after accepting the APO-69 handoff. This is planner
sequencing, not a claim that every adjacent pair is a Jira hard dependency.

## 13. Current planner boundary

The APO-69 repository-wide rebaseline and cleanup is delivered on a Draft PR against the current
`main` baseline and awaits GPT-5.6 Sol exact-head review. The review package covers BRD/plan/roadmap
synchronization, current implementation truth, Jira inventory, architecture/dead-code evidence,
safe deletion proof, validation, and the absence of Azure DevOps APO tracking. No APO-48 or other
new Story is authorized until Sol accepts this boundary and replaces `TASK.md` with exactly one
self-contained implementation contract.
