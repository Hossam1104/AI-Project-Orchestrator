# APO-44 - Explainable Quality-First Quota-Aware Routing Decisions

## Active execution contract

- Prompt: `4/5` - GPT-5.6 Sol authorized exact-head implementation; GPT-5.6 Luna xHigh executor
- Jira: `APO-44` (issue id `10872`), parent `APO-9`, priority `High`
- Jira status: `In Progress`
- Jira implementation-start comment: `12180`
- Authorized starting main SHA: `35ee09d8095a01fc7a0221f27b793c758da5e6d9`
- Authorized starting main tree: `c968eac17efdc6883a20d1281e97ac9dacddd60b`
- Feature branch: `feat/APO-44-quality-first-quota-routing`
- Functional commit: `a9e7b0f69478337261bf81bc97fb487367bafa81`
- Functional tree: `805ceded05c251d311e431755a991d3c2bd34263`
- Draft PR: `#16 OPEN / DRAFT / UNMERGED`, base `main`
- APO-43: `COMPLETE / MERGED / DONE` at the authorized baseline
- APO-45: `TO DO / NOT AUTHORIZED`

## Delivered scope

APO-44 adds a bounded, provider-independent routing decision authority:

1. `RoutingTaskClassification` captures scope, risk, blast radius, validation cost, required role,
   exact normalized capabilities, policy tags, capacity requirement, and downstream gate flags.
2. `RoutingPolicySnapshot` is an explicit executable policy snapshot separate from descriptive
   `AgentRolePolicyMetadata`, with ordered preferences, prohibitions, capacity requirements, and
   verification requirements.
3. `RoutingAgentSnapshot` copies trusted effective APO-38 registry truth, including project
   overrides. No default catalog, caller-supplied identity, or competing registry is used.
4. `RoutingCapacityEvidence` is exact agent-bound evidence with truthful Sufficient, Constrained,
   Insufficient, Unknown, Stale, NotMapped, and NotApplicable states. Existing provider usage data
   is not guessed into an agent mapping; no live provider call or refresh is performed.
5. `RoutingDecisionEngine` is pure and deterministic. It evaluates enabled state, exact role and
   capabilities, connection truth, availability, authentication, entitlement, and prohibited
   policy before normalized capacity, explicit preference, and stable AgentId tie-breaking. It
   recommends but never invokes or executes.
6. Owner overrides are auditable and can change only soft ranking among already-eligible,
   non-prohibited candidates. Rejected overrides have no silent fallback. Review, security, and
   owner-approval gates remain recorded and unsatisfied by selection.
7. `RoutingInputAssembler` binds the exact persisted planning-contract revision and current
   project context identity/version/update time. It uses the existing handoff redaction service:
   descriptive values may be redacted; authority, identity, and reference values are rejected if
   secret-shaped.
8. Immutable decisions use dedicated schema V1, create-once GUID/project-isolated persistence at
   `projects/<ProjectId>/routing/decisions/<DecisionId>/decision.json`, deterministic input
   fingerprints, lower-case SHA-256 content-integrity evidence, exact-ID reads, and observational
   tamper/version handling. No migration, overwrite, repair, quarantine, deletion, backup, or
   directory scan is performed.

## Validation contract and evidence

- `dotnet restore AIUsageMonitor.sln`: SUCCESS.
- `dotnet build AIUsageMonitor.sln --no-restore`: SUCCESS; 0 warnings, 0 errors.
- `dotnet test AIUsageMonitor.sln --no-restore`: 633/633 passed; 0 failed; 0 skipped.
- Suite totals: Domain 28; Connection 178; Provider 46; Desktop 82; Infrastructure 299.
- Focused routing tests: 16/16 passed (Connection 11; Infrastructure 5).
- `git diff --check`: clean before metadata-only changes.
- Changed-line credential-shaped review: no real credentials; only intentional redaction fixture and
  security-related identifiers.
- GitHub CI: `NONE / NOT CLAIMED`.

## Governance and handoff boundary

This is an implementation handoff, not acceptance. APO-44 remains `In Progress`; PR #16 remains
`OPEN / DRAFT / UNMERGED`. GPT-5.6 Sol must perform exact-head acceptance. No Claude Opus or Claude
Sonnet was invoked. Do not merge, mark the PR Ready, transition APO-44 to Done, or begin APO-45,
APO-46, routing execution, model invocation, worktree automation, tracker synchronization,
validation/approval engines, or Mission Control UI.

`OPUS-05-03..11 = DEFERRED / NON-BLOCKING`

`JsonFileStore.CurrentSchemaVersion = UNCHANGED` (remains `1`).

Runtime is explicitly not launched per Prompt 4/5:

`APO PROCESS COUNT = 0`

`APPLICATION LEFT RUNNING = NO`

## Next planner boundary

APO-44 Prompt 4/5 implementation complete and awaiting GPT-5.6 Sol exact-head acceptance. Draft PR
remains `OPEN / DRAFT / UNMERGED`. Do not begin APO-45, APO-46, routing execution, model invocation,
worktree automation, tracker synchronization, validation/approval engines, or Mission Control UI.
