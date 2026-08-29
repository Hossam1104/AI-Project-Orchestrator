# AI PROJECT ORCHESTRATOR — APO-47 PROMPT 3/5 SOL EXACT-HEAD REVIEW

This is the fresh-context review contract for GPT-5.6 Sol. It replaces the stale APO-69 planner
boundary. It is a review handoff only; it does not authorize merge, Jira completion, another model,
or another product Story.

## Identity and exact Git boundary

- Repository: `Hossam1104/AI-Project-Orchestrator`
- Local root: `D:\AI Tools\Active Projects\AI-Project-Orchestrator`
- Jira work item: `APO-47 — Implement Tracker-Agnostic Work Item and Dependency Synchronization`
- Parent Epic: `APO-7`
- Sol authorization comment: `12268`
- Prompt: `3/5`
- Assigned model: `GPT-5.6 Luna xHigh`
- Provider pool: `OpenAI / Codex`
- Risk tier: `Tier 3 — difficult/substantial bounded execution`
- Quota state: `UNKNOWN` (no invented percentage)
- Authorized starting `main` SHA: `346bb8faa13db551af06a24154e0da6eaebb60cb`
- Authorized starting `main` tree: `394355fbddf4eb70a6731401922763cf3fb3cab1`
- Feature branch: `feat/APO-47-tracker-agnostic-work-item-sync`
- Functional SHA: `a043234fa1fb640d5b857dc6ff4c64c97a46f4f8`
- Functional tree: `98937c63560b099fb9f9df8e97b73112db83e143`
- Final feature SHA/tree: the exact metadata-only handoff commit values are authoritative in
  the external Git/PR handoff and completion report. A commit cannot self-embed its own SHA.
- PR: #22, base `main`, must remain `OPEN / DRAFT / UNMERGED`.

## Delivered architecture

Application-owned provider-independent tracker contracts are in
`AIUsageMonitor.Application.Trackers`. They cover provider/project identity, work-item identity
and snapshots, status, parent hierarchy, dependency links, bounded comments, evidence states,
bounded reads, synchronization planning, exact mutation authority, mutation results/receipts,
and adapter resolution.

Existing `Project.TrackerType`, `Project.TrackerId`, and `Project.TrackerMetadata` are reused.
Documented metadata keys are `baseUri`, `projectKey`, `authRef`, and `freshnessSeconds`.
Secrets are never project metadata. Exact provider resolution returns `NotConfigured`,
`Unsupported`, `Resolved`, or `ConfigurationConflict`; it never guesses or cross-falls back.

Jira is the first production adapter. It uses the official Jira Cloud REST v3 surfaces:

- `GET /rest/api/3/issue/{issueIdOrKey}` for bounded exact issue reads;
- `POST /rest/api/3/search/jql` for bounded discovery and pagination;
- `GET /rest/api/3/issue/{issueIdOrKey}/transitions` and
  `POST /rest/api/3/issue/{issueIdOrKey}/transitions` for validated status transitions;
- `POST /rest/api/3/issue/{issueIdOrKey}/comment` with minimal plain-text ADF; and
- `POST /rest/api/3/issueLink` for explicitly requested dependency-link creation.

Reads normalize core identity, project, issue type, summary, status, updated time, parent,
inward/outward links, conservative known dependency semantics, bounded comments, and safe
remote references. Search, pages, response bodies, strings, comments, links, diagnostics, and
plan operations are bounded. Jira response bodies are never persisted or logged raw.

Azure DevOps exists as a provider-independent identity/configuration boundary and is resolved
exactly. No live Azure transport, browser scraping, invented endpoint, or Azure project is part
of APO-47; an unavailable Azure adapter is reported truthfully as unsupported/not configured.

## Synchronization and mutation safety

The planner is deterministic and non-autonomous. It compares fresh current evidence with one
bounded desired state and produces no-op/required-operation/conflict/unsupported/blocker results.
Jira-owned current work-item state is authoritative. Stale, partial, unavailable, or conflicting
evidence cannot produce an executable overwrite plan. No arbitrary Jira field patch surface exists.

Every mutation requires an immutable execution-level `TrackerMutationAuthority` binding the
authority, ProjectId, exact tracker identity, exact target/link, mutation kind, expected state
fingerprint, actor, correlation/run identity, issue time, expiry, and content identity where
applicable. Authority is checked before any remote call and cannot widen at runtime. Project and
target isolation are enforced.

`APO-49 HUMAN APPROVAL IMPLEMENTED = NO`.

Immediately before mutation, Jira state is freshly re-read and compared with the authority.
Only bounded comment addition, validated status transition, and dependency-link addition are
implemented. No state-changing tracker request is automatically retried:

`AUTOMATIC STATE-CHANGING RETRY COUNT = 0`

Timeout/transport ambiguity after a request may have been sent, audit persistence failure after
remote success, or failed post-mutation verification returns reconciliation-required/indeterminate
truth. A successful mutation is independently re-read and verified before synchronized success is
reported. Dependency-link removal remains unsupported/deferred because it expands destructive scope.

## Evidence, credentials, and persistence

Evidence states include `Available`, `NotConfigured`, `AuthenticationRequired`, `PermissionDenied`,
`NotFound`, `Unsupported`, `Unavailable`, `RateLimited`, `Partial`, `Stale`, `InvalidResponse`,
and `Cancelled`. Every read carries capture time, source/provider, project, target, and limitations.
Last-known evidence remains explicitly stale and never replaces a current auth/permission/failure
state. Auxiliary failures preserve verified core data as partial evidence.

Jira credentials are resolved only at request time through `ISecureCredentialStore` using an
opaque reference. No token, password, cookie, bearer/basic header, raw response, or comment body
is written to Project, JSON/JSONL, logs, exceptions, tests, or audit evidence. Comment audit data
uses body hash and bounded length only. Tracker mutation audit records use the existing project-
isolated JSONL pattern under the tracker audit area, with semantic schema version `1`.

`JsonFileStore.CurrentSchemaVersion = 1` remains unchanged, and projects without tracker
configuration remain backward-compatible.

## Validation evidence

Validation was run at the frozen functional commit before the metadata handoff:

- Restore: passed.
- Build: passed with 0 warnings and 0 errors.
- Full tests: Domain `28`, Connection `208`, Provider `60`, Desktop `83`, Infrastructure `550`;
  total `929` passed, `0` failed, `0` skipped. The final discovered count is greater than the
  inherited `896` baseline.
- Focused tracker contract/resolution/planning/authority tests: `15`.
- Focused Jira HTTP adapter tests: `14`.
- Focused tracker audit persistence tests: `3`.
- Production composition boundary tests: `10` total in the existing composition class, including
  the tracker boundary registration assertion.
- `git diff --check`: passed; changed-line secret and unsafe-surface review: no real secrets,
  raw Jira persistence, browser scraping, arbitrary patch API, or mutation retry.
- No live-owner Jira mutation test or credential-dependent test was used.

## Sol exact-head checklist

Review the exact final feature head for:

1. provider independence;
2. Jira vs Azure exact resolution;
3. ProjectId isolation;
4. bounded search/pagination;
5. Jira endpoint correctness;
6. credential handling;
7. remote response bounds;
8. stale/partial semantics;
9. deterministic synchronization planning;
10. Jira-authoritative conflict behavior;
11. explicit mutation authority;
12. authority is not human approval;
13. fresh pre-mutation validation;
14. zero automatic mutation retries;
15. ambiguous-outcome handling;
16. post-mutation verification;
17. audit evidence;
18. comment-content secret minimization;
19. no arbitrary field patch;
20. no browser scraping;
21. no live-owner mutation tests;
22. Azure boundary truthfulness;
23. schema V1;
24. full test count greater than 896;
25. CI truth;
26. application stopped; and
27. PR Draft/unmerged.

## Handoff truth and limitations

- GitHub CI: `GitHub CI = NONE / NOT CLAIMED` if no checks/statuses exist for the exact head.
- APO-47 remains `In Progress`; it must not be transitioned to Done by this handoff.
- APO-62 remains `To Do`; APO-48 remains `To Do`.
- The APO WPF application was not launched. Required final runtime truth is:
  `APO PROCESS COUNT = 0` and `APPLICATION LEFT RUNNING = NO`.
- Azure DevOps live transport is not implemented.
- Dependency-link removal is not implemented.
- APO-49 human approval, APO-48 QA gates, APO-62 SCM evidence, APO-63 remote SCM writes,
  background synchronization, polling, autonomous loops, bulk edits, arbitrary field updates,
  Mission Control, and other product Stories are out of scope.

## Required decision boundary

GPT-5.6 Sol must independently review the exact final head. Do not invoke Claude Opus during this
Prompt 3/5 handoff. If Sol finds blocking issues, remediation remains Prompt `3/5R`. If Sol accepts
the exact head, Sol may authorize controlled merge finalization.
