# AI Project Orchestrator (APO)

## Business Requirements Document

**Document Version:** 1.0  
**Date:** 21 August 2026  
**Product:** AI Project Orchestrator (APO)  
**Previous Product Identity:** AI Usage Monitor  
**Repository:** `https://github.com/Hossam1104/AI-Usage-Monitor-Tool`  
**Local Project Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`  
**Jira Project:** `APO — AI Project Orchestrator`  
**Jira URL:** `https://hossamsqa.atlassian.net/jira/software/c/projects/APO/`  
**Product Owner:** Hossam  
**Planner / Architect / Acceptance Authority:** GPT-5.6 Sol  

---

# 1. Purpose of This Document

This BRD defines the refactored and rebranded product direction for the existing AI Usage Monitor project.

The product is now **AI Project Orchestrator (APO)**: a local-first Windows application that combines AI usage and capacity monitoring with software-project orchestration, model routing, autonomous execution, validation, independent review, human approval gates, and auditable delivery workflows.

This is not a greenfield rewrite. The existing repository, local project folder, validated implementation, Git history, tests, and architecture work SHALL be retained where they remain correct. Existing code SHALL be reused, extended, or refactored to fit the new product architecture. Existing implementation SHALL NOT be discarded merely because the product scope and name have changed.

The previous usage-monitor-only product scope is superseded by this BRD. Historical documentation and completed implementation remain evidence of work already performed, but they must be mapped to the new requirements and architecture before being treated as fully accepted APO functionality.

This BRD is intentionally structured so it can be decomposed into Jira **Epics, Stories, Tasks, Sub-tasks, and Bugs**. Work that already exists in the repository SHALL also be represented in Jira for complete traceability.

---

# 2. Executive Summary

AI Project Orchestrator is a personal AI engineering control center for managing software projects and the AI models used to plan, implement, validate, review, and accept project work.

The product addresses two connected problems:

1. AI users need to know which AI services currently have sufficient quota, credits, subscription capacity, and availability.
2. Software project owners using multiple AI models currently spend significant time manually transferring prompts, implementation results, review findings, and remediation instructions between models and tools.

APO combines these capabilities into one local application.

The application SHALL provide a unified view of:

- AI subscriptions, quotas, usage windows, remaining capacity, reset times, and availability;
- registered software projects and their local repositories;
- Git/GitHub state;
- Jira and Azure DevOps work items where configured;
- AI agent/model roles and capabilities;
- current and historical orchestration runs;
- planning contracts;
- implementation progress;
- build and test evidence;
- reviewer findings;
- remediation loops;
- acceptance decisions;
- human approval gates;
- execution history and audit evidence.

The core product objective is:

> Allow the owner to supervise AI-assisted software delivery instead of manually acting as the clipboard between planner, executor, reviewer, source control, and work-tracking systems.

The product SHALL support long-running local orchestration through approved command-line interfaces, APIs, SDKs, and connected services where technically and contractually supported. It SHALL NOT depend on keeping an interactive ChatGPT, Claude, Gemini, or browser conversation open.

---

# 3. Product Identity and Continuity

## 3.1 Product Name

The product-facing name is:

> **AI Project Orchestrator (APO)**

The existing GitHub repository remains:

`Hossam1104/AI-Usage-Monitor-Tool`

The existing local project folder remains:

`D:\AI Tools\Hossam\AI Usage Monitor Tool`

Repository and local-folder renaming are explicitly out of scope for the initial rebaseline. They may be reconsidered later if the benefit outweighs migration risk.

## 3.2 Rebranding Requirement

User-facing application identity SHALL migrate from **AI Usage Monitor** to **AI Project Orchestrator**.

Technical solution, project, assembly, namespace, persistence-path, and package identifiers that currently contain `AIUsageMonitor` MAY be migrated incrementally rather than through one risky big-bang rename. The implementation plan SHALL define a controlled migration strategy.

No refactor may silently break existing persisted data, tests, build profiles, release targets, Git history, or validated runtime behavior.

## 3.3 Legacy Data Compatibility

If local runtime storage is eventually moved from a legacy path such as `%LOCALAPPDATA%\AIUsageMonitor`, APO SHALL provide a deliberate migration or compatibility path. Existing valid user data SHALL NOT be silently lost, overwritten, or abandoned.

---

# 4. Business Problem

The owner currently uses several AI systems with different strengths, quotas, subscriptions, interfaces, and reliability characteristics.

Typical project delivery currently requires repeated manual steps:

1. inspect project state;
2. ask the planner for the next implementation contract;
3. copy the prompt into the selected implementation model;
4. wait for implementation;
5. copy the result back to the planner;
6. request independent review;
7. copy review findings to a fixing model;
8. repeat validation and review;
9. return evidence to the planner for acceptance;
10. manually track GitHub/Jira state.

This workflow creates avoidable overhead and introduces risks including:

- context loss;
- prompt drift;
- incorrect model selection;
- unnecessary quota consumption;
- unverified executor claims;
- inconsistent review quality;
- missed project-state updates;
- cross-project contamination;
- duplicated manual administration;
- excessive owner involvement in low-risk intermediate steps.

APO SHALL automate the mechanical parts of this workflow while preserving explicit human authority over high-risk actions and product decisions.

---

# 5. Product Vision

APO should allow the owner to open one application and answer, within seconds:

- What projects are active?
- What work is currently running?
- Which AI models are available?
- Which models have sufficient remaining capacity?
- Which model is best suited to the current task?
- What is blocked?
- What changed in the repository?
- Did builds and tests actually pass?
- What did the independent reviewer find?
- What requires my approval?
- What happened while I was away from the computer?

The long-term interaction should become closer to:

> **Continue MESP.**

or:

> **Continue RMS Support Hub.**

APO then determines the current project state, work item, repository condition, execution contract, suitable model, available quota, validation requirements, review path, and human gates.

---

# 6. Product Principles

## 6.1 Local First

APO SHALL remain primarily a local Windows application.

No APO-owned cloud backend is required for V1.

Project configuration, orchestration state, audit history, routing decisions, and cached usage information SHALL be stored locally unless an external integration requires remote persistence in its own system.

## 6.2 Human Authority Over High-Risk Actions

Autonomy SHALL reduce repetitive work, not remove ownership.

High-risk actions require explicit human approval as defined in this BRD.

## 6.3 Evidence Over Self-Reported Success

Executor output SHALL never be accepted solely because the executor says a task is complete.

Where technically possible, APO SHALL independently inspect Git state, changed files, test results, build results, CI status, review findings, and acceptance evidence.

## 6.4 Quality Before Quota Savings

Quota availability influences routing but SHALL NOT override capability, risk, or quality requirements.

A lower-cost or higher-capacity model must not automatically receive a task that requires a stronger model.

## 6.5 Provider Truthfulness

APO SHALL distinguish:

- official provider data;
- verified local data;
- inferred data;
- manually entered data;
- stale data;
- authentication-required data;
- unavailable data;
- unsupported capability.

It SHALL NOT fabricate quotas, reset times, limits, subscription fields, programmatic access, or agent capabilities.

## 6.6 Project Isolation

Project context, source code, prompts, credentials, Jira data, execution state, and model conversations SHALL be isolated per project.

APO SHALL NOT accidentally include data from one registered project in another project's execution context.

## 6.7 Safe Autonomy

Autonomous loops SHALL be bounded by configurable limits, stop conditions, budgets, and approval gates.

## 6.8 Official Integration Paths First

APO SHALL prefer official APIs, SDKs, CLIs, authenticated integrations, OAuth/device flows, and supported connector mechanisms.

Browser UI scraping or browser automation SHALL NOT be the normal orchestration mechanism for AI models or project tools.

## 6.9 Recoverable Execution

A long-running orchestration job SHALL persist sufficient non-secret state to recover or clearly report its last safe checkpoint after application restart or process interruption.

## 6.10 No Hidden Work

Every meaningful orchestration decision and action SHALL be visible in activity history with its actor, project, time, outcome, and evidence where applicable.

---

# 7. Target Users

## 7.1 Primary User

The primary V1 user is an individual software project owner or technical lead who:

- manages multiple software projects;
- works with multiple AI coding/reasoning systems;
- uses Git/GitHub;
- may use Jira and/or Azure DevOps;
- wants to conserve AI quota without sacrificing quality;
- wants strong review and acceptance discipline;
- wants local control over credentials and project data;
- wants AI agents to continue bounded work without requiring manual prompt transfer at every step.

## 7.2 Secondary Future Users

Possible future users include individual developers, QA leads, architects, engineering managers, and advanced AI power users.

Multi-user team collaboration is not a V1 requirement.

---

# 8. AI Operating Model

The following model strategy is the default APO operating model unless the owner explicitly overrides it for a project or task.

| Priority | Model | Default Role |
|---|---|---|
| 1 | GPT-5.6 Sol | Planner / Architect / Acceptance Authority |
| 2 | GPT-5.6 Luna Max | Substantial implementation / cross-cutting fixes |
| 3 | Claude Sonnet 5 | Bounded implementation / bug fixing |
| 4 | Claude Opus 5 | Independent reviewer |
| 5 | GPT-5.6 Terra HIGH | Optional specialist security audit — not default reviewer |
| Auxiliary | Gemini 3.7 | Cost/quota load-balancing executor for suitable bounded work |

## 8.1 Role Rules

### GPT-5.6 Sol

Owns:

- business-requirement interpretation;
- architecture;
- task classification;
- implementation contracts;
- model-routing approval rules;
- acceptance criteria;
- final acceptance decisions;
- escalation when requirements are ambiguous or architecture would materially change.

### GPT-5.6 Luna Max

Preferred for:

- substantial implementations;
- cross-cutting changes;
- complex refactors;
- high-blast-radius fixes;
- implementation requiring strong repository-wide context.

### Claude Sonnet 5

Preferred for:

- bounded implementation;
- targeted bug fixes;
- focused test additions;
- localized remediation following review findings.

### Claude Opus 5

Acts as independent reviewer.

It SHALL review implementation evidence independently and SHALL NOT be treated as the default executor.

### GPT-5.6 Terra HIGH

Used only when a specialist security audit is justified by risk or explicitly requested.

### Gemini 3.7

Used as an auxiliary executor for suitable bounded, repetitive, validation, documentation, or quota-balancing work when capability and risk permit.

## 8.2 Routing Is Policy, Not Hard-Coding

The model table above is the default policy. APO SHALL represent model roles, capabilities, availability, and project-specific overrides as configuration/domain data rather than scattering model-name conditionals through workflows.

---

# 9. Scope

## 9.1 In Scope for V1

V1 includes the following capability families:

1. application rebrand and architecture rebaseline;
2. existing-code assessment, reuse, and controlled refactoring;
3. AI provider usage/capacity monitoring;
4. project registry and project isolation;
5. local repository/workspace awareness;
6. Git and GitHub integration;
7. Jira integration;
8. Azure DevOps work-item integration where required for registered projects;
9. AI model/agent registry;
10. agent connectivity through supported CLI/API/integration mechanisms;
11. intelligent task classification and model routing;
12. planner-generated execution contracts;
13. autonomous implementation execution;
14. build/test/validation execution and evidence capture;
15. independent review orchestration;
16. bounded remediation loops;
17. final acceptance orchestration;
18. human approval gates;
19. activity history and audit trail;
20. command-center dashboard;
21. notifications and attention queue;
22. local persistence, resilience, security, and recovery;
23. self-contained Windows packaging;
24. Jira traceability for both new work and previously completed work.

## 9.2 Out of Scope for V1

V1 does not require:

- a commercial SaaS offering;
- a mandatory APO cloud account;
- multi-user collaboration or RBAC for teams;
- autonomous production deployment without owner approval;
- autonomous destructive database/data operations;
- automatic purchasing of subscriptions or API credits;
- browser-cookie extraction;
- password scraping;
- browser automation as the primary AI-agent transport;
- replacing GitHub, Jira, Azure DevOps, or the user's IDE;
- unlimited autonomous self-modification;
- guaranteed access to provider data that the provider does not expose;
- guaranteed use of a consumer subscription through an API when the provider requires separate API/CLI entitlement or billing.

---

# 10. Existing Implementation Reuse and Refactoring Policy

The repository already contains validated implementation from the previous AI Usage Monitor scope. That work is an asset, not disposable prototype code.

## 10.1 Existing Capability Areas to Assess for Reuse

The rebaseline SHALL explicitly assess and map existing implementation including:

- .NET solution and project foundation;
- WPF desktop shell;
- MVVM/composition-root structure;
- Domain and Application layers;
- provider-independent quota/domain concepts;
- dependency injection;
- logging;
- JSON document persistence;
- monthly JSONL history/event persistence;
- atomic writes;
- corruption handling/quarantine;
- duplicate suppression;
- storage initialization resilience;
- cross-Windows target settings;
- self-contained publish profiles;
- x86/x64/ARM64 build configuration;
- xUnit tests;
- GitHub repository governance/history.

## 10.2 Reuse Classification

Every significant existing component SHALL be classified as one of:

- **Reuse As-Is** — already fits the APO architecture and requirements;
- **Reuse With Extension** — valid foundation requiring additional capability;
- **Refactor** — behavior is useful but boundaries/naming/design no longer fit;
- **Superseded** — intentionally replaced architecture retained only as history;
- **Remove** — no longer valid and unsafe or unnecessary to retain.

## 10.3 No False Completion

Existing code SHALL NOT automatically be marked complete against new APO requirements merely because it was previously implemented.

A backfilled Jira item may be marked Done only after:

1. the relevant code is identified;
2. the requirement mapping is explicit;
3. architecture compatibility is checked;
4. relevant validation evidence exists or is rerun;
5. any required refactor is completed.

## 10.4 Historical Superseded Work

Historical EF Core / SQL Server LocalDB work remains valid project history but is architecturally superseded by the existing JSON/JSONL portable architecture. It SHALL NOT be revived unless future evidence and explicit planner approval justify a storage-architecture change.

---

# 11. Functional Requirements

## 11.1 Project Registry

**FR-PROJ-001** — APO SHALL allow the owner to register multiple software projects.

**FR-PROJ-002** — A project SHALL support at minimum: name, local path, repository, default branch, tracker type, tracker project identifier, status, governance files, model-routing policy, and safety policy.

**FR-PROJ-003** — APO SHALL verify configured local paths and source-control state before starting execution.

**FR-PROJ-004** — Project context SHALL remain isolated from every other project.

**FR-PROJ-005** — A project SHALL support Active, Paused, Blocked, Archived, and equivalent lifecycle states.

**FR-PROJ-006** — APO SHALL maintain a concise current project state/checkpoint that can be reconstructed from repository/tracker evidence when stale.

## 11.2 Work Intake and Tracking

**FR-TRACK-001** — APO SHALL integrate with Jira for project/work-item discovery, reading, creation, update, and status synchronization where permissions allow.

**FR-TRACK-002** — APO SHALL support Azure DevOps work-item integration for registered projects that use Azure DevOps.

**FR-TRACK-003** — Work-item adapters SHALL be provider-independent at the orchestration layer.

**FR-TRACK-004** — APO SHALL preserve tracker IDs/keys and link execution evidence to the originating work item.

**FR-TRACK-005** — APO SHALL be able to distinguish Epic, Story/User Story, Task, Bug, and Sub-task semantics when the tracker supports them.

**FR-TRACK-006** — Tracker updates performed autonomously SHALL be auditable.

## 11.3 Source Control and Repository Awareness

**FR-GIT-001** — APO SHALL inspect repository status, current branch, HEAD SHA, remote relationship, and owner changes before modifying code.

**FR-GIT-002** — Unrelated owner changes SHALL be preserved.

**FR-GIT-003** — APO SHALL support safe creation and use of task branches where repository policy allows.

**FR-GIT-004** — APO SHALL inspect changed files and diffs independently from executor self-report.

**FR-GIT-005** — APO SHALL support draft pull-request creation where configured and permitted.

**FR-GIT-006** — Merge to protected/default branches SHALL respect the Human Approval Gate policy.

**FR-GIT-007** — Force push, destructive reset, and destructive clean operations SHALL be prohibited by default.

## 11.4 AI Agent and Model Registry

**FR-AGENT-001** — APO SHALL maintain a registry of available AI models/agents and their roles.

**FR-AGENT-002** — Each agent profile SHALL support capabilities, limitations, connection mechanism, current availability, routing role, cost/quota metadata, and project-specific overrides.

**FR-AGENT-003** — Agent executors SHALL be accessed through a provider-independent execution abstraction.

**FR-AGENT-004** — APO SHALL distinguish interactive-only access from programmatically invokable CLI/API/SDK access.

**FR-AGENT-005** — APO SHALL NOT assume that a consumer subscription automatically grants API access or that API billing is included in the subscription.

**FR-AGENT-006** — Unsupported or unavailable programmatic execution SHALL be represented truthfully and SHALL fall back to a manual handoff path when appropriate.

## 11.5 AI Usage and Capacity Monitoring

The original usage-monitoring capability remains a first-class part of APO.

**FR-CAP-001** — APO SHALL represent provider subscriptions/plans where data is available.

**FR-CAP-002** — APO SHALL support arbitrary quota windows rather than fixed five-hour/weekly columns.

**FR-CAP-003** — APO SHALL represent used, remaining, limit, reset time, window type, source, confidence, and freshness independently.

**FR-CAP-004** — APO SHALL preserve `DateTimeOffset` semantics for provider timestamps and display relevant times in the user's Windows timezone.

**FR-CAP-005** — APO SHALL support manual fallback data where automatic provider collection is unavailable.

**FR-CAP-006** — APO SHALL maintain historical usage/capacity snapshots without unnecessary duplicate records.

**FR-CAP-007** — APO SHALL surface provider health, stale data, authentication requirements, and unsupported fields clearly.

**FR-CAP-008** — Capacity data SHALL be available to the routing engine but SHALL never be its only decision input.

## 11.6 Task Classification and Intelligent Routing

**FR-ROUTE-001** — APO SHALL classify work by scope, complexity, blast radius, risk, required expertise, expected validation cost, and suitable agent role.

**FR-ROUTE-002** — Routing SHALL consider project policy, model capability, model availability, capacity/quota state, previous failures, and owner overrides.

**FR-ROUTE-003** — Quality/risk constraints SHALL take precedence over quota conservation.

**FR-ROUTE-004** — Every routing decision SHALL record an explanation and the evidence used.

**FR-ROUTE-005** — The owner SHALL be able to override a routing decision before execution where appropriate.

**FR-ROUTE-006** — Routing policies SHALL be configurable without rewriting orchestration workflows.

## 11.7 Planning and Execution Contracts

**FR-PLAN-001** — Substantial work SHALL be executed from an explicit implementation contract or task specification.

**FR-PLAN-002** — The contract SHALL contain scope, repository/project state, requirements, constraints, forbidden scope, expected deliverables, validation, acceptance criteria, and stop conditions.

**FR-PLAN-003** — The planner role SHALL remain separate from the implementation role unless explicitly overridden.

**FR-PLAN-004** — APO SHALL preserve the exact contract and its version as execution evidence.

**FR-PLAN-005** — Material ambiguity affecting requirements or architecture SHALL stop autonomous implementation and require planner/owner resolution.

## 11.8 Autonomous Execution Runtime

**FR-EXEC-001** — APO SHALL execute supported AI agents through official or approved CLI/API/SDK mechanisms.

**FR-EXEC-002** — APO SHALL be able to run long-lived local executions without requiring the owner to manually copy/paste intermediate prompts.

**FR-EXEC-003** — Execution SHALL be cancellable.

**FR-EXEC-004** — Execution SHALL expose status, active step, elapsed time, project, agent, task, and recent activity.

**FR-EXEC-005** — APO SHALL capture structured execution results rather than relying solely on free-form completion messages.

**FR-EXEC-006** — Failed execution SHALL distinguish recoverable failure, authentication failure, quota/capacity failure, tool failure, validation failure, policy stop, and unknown failure.

**FR-EXEC-007** — A process interruption SHALL not falsely mark a task as complete.

**FR-EXEC-008** — APO SHALL persist sufficient run state to report or resume from a safe boundary after restart when the underlying tool supports safe resumption.

## 11.9 Validation and Evidence

**FR-VAL-001** — APO SHALL support project-specific validation commands for builds, automated tests, linting, formatting, static analysis, security checks, and other approved validators.

**FR-VAL-002** — Validation output SHALL be captured with command/result metadata and timestamps while avoiding secret leakage.

**FR-VAL-003** — APO SHALL distinguish targeted validation from full regression validation.

**FR-VAL-004** — Executor-reported test counts SHALL be independently verifiable where practical.

**FR-VAL-005** — Validation failures SHALL block acceptance unless explicitly waived by authorized policy with recorded rationale.

**FR-VAL-006** — Baseline failures that existed before the task SHALL be distinguished from regressions introduced by the task where evidence allows.

## 11.10 Independent Review

**FR-REV-001** — Configured high-value implementation SHALL pass independent review before final acceptance.

**FR-REV-002** — The default independent reviewer is Claude Opus 5.

**FR-REV-003** — The reviewer SHALL receive repository evidence, task requirements, validation evidence, and relevant diff/context rather than relying only on the executor summary.

**FR-REV-004** — Review findings SHALL include severity, evidence, affected requirement, recommended disposition, and blocking status.

**FR-REV-005** — Review findings SHALL be persisted and traceable through remediation.

**FR-REV-006** — Specialist Terra security review SHALL be optional and risk-triggered, not the default review path.

## 11.11 Remediation Loop

**FR-FIX-001** — APO SHALL classify accepted review findings and route them to an appropriate fixing agent.

**FR-FIX-002** — Bounded findings SHOULD prefer a bounded executor such as Sonnet when capability and risk allow.

**FR-FIX-003** — Cross-cutting findings SHOULD return to a substantial executor such as Luna when appropriate.

**FR-FIX-004** — Fixes SHALL be revalidated before re-review.

**FR-FIX-005** — Autonomous review/fix cycles SHALL have a configurable maximum. The initial default target is three cycles unless the implementation plan defines a safer value.

**FR-FIX-006** — Exceeding the configured retry/review limit SHALL transition the run to `Human Review Required` rather than continuing indefinitely.

## 11.12 Final Acceptance

**FR-ACC-001** — GPT-5.6 Sol is the default final acceptance authority for implementation quality and architectural compliance.

**FR-ACC-002** — Final acceptance SHALL evaluate original requirements, final diff/state, validation evidence, review outcome, unresolved findings, and project policy.

**FR-ACC-003** — Acceptance SHALL return Accepted, Rejected, Blocked, or Requires Human Decision with reasons.

**FR-ACC-004** — Acceptance does not automatically authorize a Human Approval Gate action such as merge or production deployment.

## 11.13 Human Approval Gates

Explicit owner approval SHALL be required before:

**FR-GATE-001** — merging into `main` or another protected/default integration branch, unless the owner later configures a clearly bounded repository-specific exception;

**FR-GATE-002** — production deployment;

**FR-GATE-003** — destructive database/schema/data changes or destructive local-data migration;

**FR-GATE-004** — material architecture-baseline changes;

**FR-GATE-005** — material approved-business-requirement changes;

**FR-GATE-006** — actions involving secrets, credential rotation, billing, purchasing, or subscription changes;

**FR-GATE-007** — high-risk security changes identified by policy;

**FR-GATE-008** — any action explicitly configured by the owner as approval-required.

APO MAY prepare branches, commits, draft PRs, evidence, tracker updates, and recommendations before a gate.

## 11.14 Activity, Audit, and History

**FR-AUD-001** — APO SHALL maintain append-oriented activity history for orchestration actions.

**FR-AUD-002** — Audit records SHALL include project, run/task, actor/agent, action, timestamp, outcome, and relevant non-secret evidence identifiers.

**FR-AUD-003** — The owner SHALL be able to inspect a chronological run timeline.

**FR-AUD-004** — Routing decisions, approvals, rejections, review findings, fixes, validations, and gate decisions SHALL be auditable.

**FR-AUD-005** — Audit history SHALL avoid storing secrets and unnecessary raw authenticated payloads.

## 11.15 Command Center UI

**FR-UI-001** — APO SHALL provide a modern primary dashboard summarizing AI capacity, project state, active executions, blockers, and owner-attention items.

**FR-UI-002** — The dashboard SHALL surface the most important current question: what can safely continue without owner action and what requires attention now?

**FR-UI-003** — APO SHALL provide project-specific views.

**FR-UI-004** — APO SHALL provide an execution-center view with live progress and recent actions.

**FR-UI-005** — APO SHALL provide an AI capacity/provider view.

**FR-UI-006** — APO SHALL provide an agent/model configuration view.

**FR-UI-007** — APO SHALL provide an activity/audit view.

**FR-UI-008** — APO SHALL clearly distinguish Running, Waiting, Blocked, Failed, Review, Accepted, and Human Approval Required states.

## 11.16 Notifications and Attention Queue

**FR-NOTIFY-001** — APO SHALL notify the owner when an autonomous run reaches a required human gate.

**FR-NOTIFY-002** — APO SHALL notify the owner when execution becomes blocked and cannot safely self-remediate.

**FR-NOTIFY-003** — APO SHALL support AI quota/reset notifications where useful.

**FR-NOTIFY-004** — Notifications SHALL be configurable and SHALL avoid excessive noise.

## 11.17 Settings and Policy

**FR-SET-001** — Global and per-project model-routing policies SHALL be configurable.

**FR-SET-002** — Autonomous loop counts, execution budgets, allowed tools/actions, validation requirements, and human gates SHALL be configurable within safe minimum constraints.

**FR-SET-003** — Owner overrides SHALL be recorded where they change normal routing or safety behavior.

---

# 12. Orchestration Lifecycle

The default delivery lifecycle is:

```text
Work Item / Owner Intent
        |
        v
Project State Verification
        |
        v
Task Classification
        |
        v
Planner / Execution Contract
        |
        v
Model Routing Decision
        |
        v
Implementation Executor
        |
        v
Independent Evidence Collection
        |
        v
Build / Test / Validation
        |
        v
Independent Review
        |
        +---- findings ----> Remediation Executor
        |                         |
        |                         v
        |                    Revalidation
        |                         |
        +-------------------------+
        |
        v
Sol Final Acceptance
        |
        v
Human Approval Gate (when required)
        |
        v
Delivery / Tracker Synchronization
```

The workflow SHALL stop safely rather than guessing when required state, requirements, credentials, repository integrity, or approval is missing.

---

# 13. Autonomous Execution Controls

APO SHALL never implement an unbounded recursive agent loop.

The orchestration engine SHALL support controls including:

- maximum remediation/review cycles;
- maximum executor retries;
- configurable quota/cost budget where usage data is available;
- configurable execution-time budget;
- changed-file/blast-radius safeguards;
- cancellation;
- project-level concurrency policy;
- global concurrency policy;
- approval-required actions;
- explicit stop reasons.

Default stop conditions SHALL include at least:

- unexpected repository divergence;
- unresolvable merge conflict;
- secret detection;
- destructive operation outside approved scope;
- architecture change outside approved scope;
- ambiguous requirement affecting correctness;
- unavailable required credentials;
- exhausted/insufficient agent capacity with no safe fallback;
- repeated failed remediation beyond policy limits;
- inability to verify mandatory validation evidence.

---

# 14. Security and Privacy Requirements

## 14.1 Credential Safety

APO SHALL NOT:

- store raw passwords in JSON/JSONL;
- scrape browser passwords;
- extract browser cookies as an integration strategy;
- log access tokens;
- commit secrets;
- expose credentials in task prompts or audit records.

Secrets SHALL use Windows Credential Manager, DPAPI, or another planner-approved secure Windows mechanism where local secure storage is required.

## 14.2 Necessary AI Context Transmission

Unlike the original usage-monitor-only product, APO must provide task context and relevant source/code to user-configured AI executors when required to perform software work.

This transmission SHALL follow these rules:

- only the context reasonably needed for the task is sent;
- cross-project context leakage is prohibited;
- configured secret/sensitive-file exclusions are honored;
- secret scanning/redaction is applied where technically appropriate;
- the destination provider/agent is visible to the owner;
- APO-owned telemetry SHALL NOT silently upload project code;
- provider-directed execution is treated separately from APO analytics/telemetry.

## 14.3 Least Privilege

External integrations SHALL request/use only permissions needed for their configured capability.

## 14.4 Command Execution Safety

Shell/process execution SHALL be policy-controlled. Dangerous commands SHALL be blocked or gated.

---

# 15. Architecture and Technology Constraints

The validated portable foundation remains the default technical baseline unless a later architecture decision explicitly changes it.

## 15.1 Core Stack

- C#
- .NET 10
- WPF
- MVVM
- Modular Clean Architecture
- `System.Text.Json`
- JSON for small mutable state/configuration
- JSONL for append-oriented events/history
- `HttpClient`
- `Microsoft.Extensions.Http.Resilience` where justified
- `Microsoft.Extensions.DependencyInjection`
- Serilog
- Windows secure credential storage
- Git/GitHub
- Jira and Azure DevOps adapters
- focused automated tests
- self-contained Windows release artifacts

## 15.2 Persistence

V1 SHALL remain database-free by default.

No ORM/database SHALL be introduced unless evidence demonstrates that the JSON/JSONL architecture cannot safely satisfy orchestration requirements and the planner explicitly approves the change.

Likely local data categories include:

- settings;
- registered projects;
- agent/model configuration;
- provider state;
- subscriptions/capacity snapshots;
- orchestration runs;
- routing decisions;
- approvals;
- activity/audit events;
- review findings;
- validation summaries;
- local non-secret integration metadata.

## 15.3 Logical Architecture

The target architecture SHALL preserve clear dependency direction and add orchestration as a first-class domain rather than embedding logic in the UI.

Conceptually:

```text
Desktop / WPF
      |
      v
Application Use Cases
      |
      v
Domain

Infrastructure -----------------> Application / Domain contracts
AI Providers / Agents ----------> Application / Domain contracts
Project Integrations -----------> Application / Domain contracts
Source Control Integrations ----> Application / Domain contracts
```

Expected extensibility seams include concepts equivalent to:

- AI usage/capacity provider adapter;
- agent executor adapter;
- project tracker adapter;
- source-control adapter;
- validation runner;
- secure credential store;
- execution-state persistence;
- notification adapter.

Exact interface names are an architecture decision, not a BRD mandate.

---

# 16. Windows Compatibility

The existing compatibility objective remains:

- Windows 10 version 1809 / build 17763 baseline goal;
- Windows 10 1809+ and Windows 11 product goal;
- x86, x64, and ARM64 release targets;
- x64 primary validation target;
- graceful degradation for optional modern UI effects;
- no mandatory modern GPU, NPU, TPM, or AI accelerator;
- low background resource usage.

Exact framework/OS support claims SHALL continue to be verified against current Microsoft support documentation before release claims are made.

---

# 17. Non-Functional Requirements

## 17.1 Reliability

**NFR-REL-001** — One provider or integration failure SHALL NOT crash the whole application.

**NFR-REL-002** — Critical state writes SHALL be resilient to interruption and corruption.

**NFR-REL-003** — A run SHALL never be reported as successfully completed without a terminal outcome and required evidence.

**NFR-REL-004** — Restart recovery SHALL preserve the last known safe run state.

## 17.2 Performance

**NFR-PERF-001** — Idle monitoring SHALL remain lightweight and SHALL NOT materially degrade development tools.

**NFR-PERF-002** — History loading SHALL be partitioned/streamed and SHALL NOT load lifetime data unnecessarily.

**NFR-PERF-003** — Long-running agent processes SHALL use asynchronous I/O and shall not freeze the WPF UI.

## 17.3 Maintainability

**NFR-MAINT-001** — Provider/model/tracker-specific behavior SHALL remain behind adapters or modular boundaries.

**NFR-MAINT-002** — Model names and routing rules SHALL not be duplicated throughout application code.

**NFR-MAINT-003** — Existing automated tests SHALL be preserved or deliberately replaced with equivalent/better coverage during refactor.

## 17.4 Testability

**NFR-TEST-001** — Core routing, state transitions, safety gates, quota normalization, validation interpretation, and review-loop behavior SHALL be testable without live paid AI calls.

**NFR-TEST-002** — CI SHALL use sanitized fixtures/mocks/fakes rather than live authenticated provider calls unless explicitly approved for a separate secure integration environment.

## 17.5 Auditability

**NFR-AUD-001** — Important autonomous decisions SHALL be explainable after the fact.

**NFR-AUD-002** — The owner SHALL be able to identify which model performed each significant action.

## 17.6 Usability

**NFR-UX-001** — The owner SHALL be able to identify active work, blockers, quota pressure, and approval requests from the main dashboard without opening multiple external tools.

**NFR-UX-002** — Destructive/high-risk actions SHALL use explicit confirmation language.

---

# 18. Jira Backlog and Traceability Requirements

The Jira project `APO` is the authoritative work-tracking space for the refactored product.

## 18.1 Complete Historical Backfill

All meaningful existing implementation SHALL be represented in Jira, including work completed before the APO Jira project existed.

Historical Jira items SHALL be created for traceability and mapped to actual repository evidence.

They SHALL NOT be marked Done solely based on old session labels; status must reflect the new requirement mapping and refactor need.

## 18.2 Required Jira Traceability

Each implementation Story/Task SHOULD include:

- BRD requirement IDs;
- Epic relationship;
- current/new/refactor classification;
- scope;
- acceptance criteria;
- selected executor role where useful;
- validation requirements;
- reviewer requirement where applicable;
- Git branch/PR/commit evidence;
- final acceptance result.

## 18.3 Candidate Epic Structure

The final Jira backlog may refine names and ordering, but the initial capability map is:

1. **APO Product Rebrand & Governance Rebaseline**
2. **Windows Platform & Application Foundation**
3. **Local Persistence, Resilience & Security Foundation**
4. **AI Usage, Subscription & Capacity Monitoring**
5. **Project Registry & Workspace Management**
6. **Git & GitHub Integration**
7. **Jira & Azure DevOps Work-Item Integration**
8. **AI Agent / Model Registry & Connectivity**
9. **Intelligent Model Routing & Quota-Aware Decisioning**
10. **Planning & Execution Contracts**
11. **Autonomous Execution Runtime**
12. **Validation & Evidence Engine**
13. **Independent Review & Remediation Engine**
14. **Acceptance & Human Approval Gates**
15. **Command Center & Project UX**
16. **Activity, Audit, History & Notifications**
17. **Packaging, Compatibility, CI & Release Quality**

Some of these Epics will contain backfilled completed Stories/Tasks as well as new/refactor work.

---

# 19. Initial Legacy-to-APO Mapping

The following historical implementation areas are expected to map into the new backlog and SHALL be validated during backlog creation.

| Historical Area | Initial APO Interpretation | Expected Treatment |
|---|---|---|
| Repository/solution foundation | Platform foundation | Reuse / verify / rebrand |
| Domain & Application architecture | Core architecture | Reuse with extension |
| Domain integrity remediation | Core correctness | Reuse / verify |
| Historical EF/LocalDB persistence | Superseded architecture | Historical only |
| Portable WPF migration | Windows application foundation | Reuse / rebrand |
| JSON/JSONL persistence | Local orchestration persistence foundation | Reuse with extension |
| Atomic writes / corruption handling | Reliability foundation | Reuse |
| Startup resilience | Reliability foundation | Reuse |
| JSONL latest-history optimization | History foundation | Reuse |
| Interrupted-tail handling | History resilience | Reuse |
| Cross-Windows solution targets | Packaging/compatibility foundation | Reuse / revalidate |
| Self-contained publish profiles | Release foundation | Reuse / revalidate |
| Existing provider-independent quota concepts | AI capacity domain | Reuse with extension |
| Existing usage-provider feasibility plan | AI capacity discovery | Replan under APO scope |

This table is an initial business mapping only. Detailed code-level mapping belongs in architecture/backlog work.

---

# 20. Success Criteria

V1 APO is successful when the following outcomes are demonstrated.

## 20.1 Project Orchestration

At least the owner's primary active projects can be registered with isolated configuration and repository/tracker state.

## 20.2 Reduced Manual Handoffs

For a suitable bounded software task, APO can progress from approved execution contract through implementation, validation, independent review, bounded remediation, and final acceptance without the owner manually copying each intermediate prompt/result between models.

## 20.3 Human Control

The workflow stops at configured human gates and never silently performs prohibited high-risk actions.

## 20.4 Evidence-Based Acceptance

Completion decisions are based on repository and validation evidence, not only model claims.

## 20.5 Capacity-Aware Routing

The routing engine uses real/available AI-capacity data where possible while preserving the quality-first model policy.

## 20.6 Auditability

The owner can inspect what happened during a run, which models were used, why routing occurred, what changed, what tests ran, what review findings existed, and why the work was accepted or rejected.

## 20.7 Resilience

The application remains usable when one AI provider, tracker, source-control service, or quota source fails.

## 20.8 Existing Investment Preserved

Valid existing WPF, domain, persistence, resilience, build, test, and packaging implementation is reused or safely refactored rather than unnecessarily rewritten.

---

# 21. Release Acceptance Principles

Before an APO V1 release is considered ready:

- no known critical safety-gate bypass remains;
- project isolation has automated coverage;
- credentials/secrets are not persisted in plain local files;
- provider/model capability claims are evidence-based;
- routing decisions are explainable;
- autonomous loops are bounded;
- cancellation and blocked-state handling are reliable;
- required validation evidence is captured;
- independent review workflow is operational;
- human-gate behavior is verified;
- existing local data migration/compatibility is validated where relevant;
- x64 release is fully validated;
- x86 and ARM64 release targets are validated to the level defined by the implementation plan;
- self-contained release behavior is verified;
- repository and Jira traceability are complete enough to reconstruct delivered scope.

---

# 22. Decisions Frozen by This BRD

The following decisions are approved and should be treated as baseline unless the owner explicitly changes them:

1. The product is rebranded to **AI Project Orchestrator (APO)**.
2. The existing GitHub repository is retained.
3. The existing local project folder is retained.
4. The new Jira project is `APO`.
5. Existing validated code is reused/refactored rather than discarded.
6. Existing historical work is backfilled into Jira for traceability.
7. AI usage/capacity monitoring remains part of the product.
8. Project orchestration becomes a primary product capability.
9. GPT-5.6 Sol remains Planner / Architect / Acceptance Authority.
10. GPT-5.6 Luna Max remains the substantial implementation/cross-cutting executor.
11. Claude Sonnet 5 remains the bounded implementation/bug-fix executor.
12. Claude Opus 5 remains the independent reviewer.
13. GPT-5.6 Terra HIGH remains an optional specialist security reviewer, not the default reviewer.
14. Gemini 3.7 remains the auxiliary quota/cost-balancing executor for suitable bounded work.
15. Model routing is quality/risk first and quota-aware second.
16. High-risk actions remain behind explicit human approval gates.
17. The product remains local-first and Windows desktop focused.
18. The validated WPF/.NET/JSON/JSONL portable architecture remains the default foundation pending detailed architecture rebaseline.
19. Autonomous execution uses supported APIs/CLIs/SDKs/integrations rather than fragile browser UI automation.
20. Consumer subscriptions and programmatic API/CLI entitlement are treated as separate facts and must not be assumed equivalent.

---

# 23. Next Planning Step

After this BRD is accepted, the planner SHALL use it to build the APO Jira backlog.

The recommended sequence is:

1. create the Epic structure;
2. inspect existing repository implementation against each Epic;
3. create backfilled Stories/Tasks for already-delivered work;
4. classify each item as Reuse As-Is, Reuse With Extension, Refactor, Superseded, or Remove;
5. create new Stories/Tasks for missing APO capabilities;
6. define acceptance criteria and dependencies;
7. establish the new implementation sequence;
8. update repository governance (`AGENTS.md`, implementation plan, session/task strategy, current state, and related documentation) to make this BRD authoritative;
9. stop the old Session 04 provider-only task from being executed under the superseded plan;
10. begin implementation only after the new architecture/backlog baseline is accepted.

---

# 24. Glossary

**APO** — AI Project Orchestrator.

**Agent** — A programmatically invokable AI execution/review/planning capability, such as an approved CLI/API/SDK-backed coding agent.

**Capacity** — Available quota, credits, limit headroom, subscription allowance, or other provider-specific availability data.

**Execution Contract** — The planner-approved task specification that constrains implementation scope and required validation.

**Human Approval Gate** — A point at which APO must stop and request explicit owner approval before proceeding.

**Independent Review** — Review performed by a model/agent separate from the implementation executor.

**Planner** — The role responsible for requirements interpretation, architecture, implementation contracts, routing policy, and final acceptance.

**Project Isolation** — The rule that one project's code, state, credentials, prompts, and work items must not leak into another project.

**Routing Decision** — The evidence-backed selection of the most appropriate model/agent for a task.

**Run** — One persisted orchestration execution instance from intake through a terminal state or human gate.

**Terminal State** — A final or paused outcome such as Accepted, Rejected, Failed, Blocked, Cancelled, or Human Approval Required.

---

# 25. Approval Status

**Status:** BASELINE CREATED — awaiting backlog decomposition and governance rebaseline.

This BRD defines the approved product direction requested by the owner. Detailed architecture, implementation sequencing, Jira decomposition, and code refactoring shall follow in subsequent planner-controlled steps.
