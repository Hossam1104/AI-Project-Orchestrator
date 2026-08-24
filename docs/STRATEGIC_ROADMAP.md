# AI Project Orchestrator — Strategic Orchestration Roadmap

## 1. Purpose

This roadmap is the concise bridge between the approved BRD, the implementation plan, Jira
Stories, and the owner experience APO is being built to provide. It records the strategic
direction and sequencing after APO-37; it is not a runtime feature claim and does not authorize a
new Story. Each implementation boundary still requires a Sol-authored `TASK.md` contract.

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

The orchestration runtime, Smart Continue, remote SCM evidence, controlled delivery, and
consolidated Mission Control experience remain planned capabilities in the current repository
state.

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
| GPT-5.6 Luna xHigh | Primary bounded implementation, remediation, repository, documentation, Jira, and validation executor |
| Claude Sonnet 5 | Exceptional difficult alternative only when Luna genuinely needs one or Sol explicitly requests it |
| Claude Opus 5 | Independent reviewer at configured cadence and critical checkpoints |
| GPT-5.6 Terra HIGH | Optional risk-triggered security specialist |
| Gemini 3.7 | Auxiliary executor for suitable bounded, repetitive, validation, documentation, or quota-balancing work |

One assigned Jira work item is the maximum active scope for an executor. A roadmap Story is not
implementation authorization, and completing one Story never automatically starts the next.

## 5. Current shipped foundation

The accepted foundation includes the WPF/.NET/JSON/JSONL desktop architecture, secure credential
reference boundaries, provider-independent capacity contracts, project/orchestration storage,
Projects workspace, normal DI composition, and APO-37 read-only local Git repository verification.
APO-37 provides bounded local branch/HEAD/status/remote evidence for a selected configured path;
it does not call a remote SCM service, read repository file contents, or perform Git writes.

APO-33 remains the existing repository-owned GitHub Actions CI/release Story. Local validation in
this roadmap session is not a GitHub CI result. The strategic orchestration runtime, provider/model
registry runtime, routing, execution, tracker automation, remote SCM evidence, controlled delivery,
review engine, and Mission Control are not shipped by this documentation checkpoint.

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

The approved APO-1 through APO-17 Epic structure is reused. The active strategic product backlog is
APO-38 through APO-63. APO-64 through APO-67 are Done, VOID, `no-project-work`, and
`connector-correction` artifacts retained only as transparent Jira connector history; they have
zero product scope and are excluded from roadmap totals, dependencies, sequencing, BRD claims,
and Mission Control scope. No APO-68 or replacement Story is created by this roadmap continuation.

## 12. Near-term ordering

The dependency order is:

```text
APO-37 accepted local foundation
        ↓
APO-38 → APO-39 → APO-40 → APO-41 → APO-42 → APO-43
        ↓                                  ↘
APO-44 → APO-45 → APO-46 → APO-47       recovery context
                                  + APO-62 read-only remote SCM/CI evidence
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

Read-only remote evidence must precede controlled remote writes because APO cannot safely mutate a
remote target without independently knowing the current repository, ref, review/check, validation,
permission, mergeability, and approval state. APO-38 is the recommended next Story because
capability and connection truth are prerequisites for later routing, handoffs, and execution, but
it is not automatically authorized.

## 13. Current planner boundary

Prompt 1/5 strategic rebaseline continuation is documentation/Jira synchronization only. Draft PR
#8 remains OPEN / DRAFT / UNMERGED against the accepted APO-37 main baseline while GPT-5.6 Sol
reviews the complete rebaseline, Jira APO-38..63, APO-43 Smart Continue ownership, APO-62, APO-63,
and the void APO-64..67 exclusion. No Prompt 2/5 Story is authorized until Sol accepts this
boundary and replaces `TASK.md` with exactly one self-contained implementation contract.
