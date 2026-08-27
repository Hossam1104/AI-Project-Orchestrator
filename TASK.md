# APO-43 - Canonical Context, Smart Continue & Recovery Checkpoints

## Active execution contract

- Prompt: `3/5` - GPT-5.6 Sol authorized implementation; GPT-5.6 Luna xHigh executor
- Jira: `APO-43` (issue id `10871`), parent `APO-3`, priority `High`
- Jira status: `In Progress`
- Jira implementation-start comment: `12176`
- Authorized starting main SHA: `fcbb3e82460f9ed689b446eef16b6c2904d643c6`
- Authorized starting main tree: `f7920d9a06d3acd4443937b77f5eed45c6210740`
- Feature branch: `feat/APO-43-smart-continue-recovery-checkpoints`
- Functional commit: `ad466d52ff2a66af098337aa2fd4df3988e4a339`
- Functional tree: `82b15b4311025098d8ca534caa9b2e28e1c6b078`
- Draft PR: `#15 OPEN / DRAFT / UNMERGED`, base `main`

## Delivered scope

APO-43 provides three composed, provider-independent authorities:

1. An immutable, create-once `RecoveryCheckpoint` with schema V1, GUID-derived project/checkpoint
   storage, deterministic lower-case SHA-256 content-integrity evidence, bounded reference-only
   metadata, lifecycle state, exact context/contract binding, optional exact WorkGraph/node and
   HandoffPackage binding, shallow predecessor lineage, selected agent-role references, evidence,
   gate snapshots, typed blockers, and a typed next safe action.
2. A project-scoped two-slot `ContinuationHead` with atomic generations alternating A/B (`1 -> A`,
   `2 -> B`, `3 -> A`), latest and last-known-safe references, observational reads, and corrupt
   newest-generation fallback without repair or directory scanning.
3. A read-only `SmartContinueResolver` that evaluates durable authority only and returns explicit
   lifecycle, stale-evidence, context, integrity, version, and infrastructure outcomes. It never
   executes the returned action, routes models, refreshes external evidence, or writes state.

LastSafe is updated for `Ready`, `Waiting`, `Blocked`, and `ApprovalRequired`. `Interrupted`,
`Failed`, and `Cancelled` preserve the previous safe reference; `Completed` is terminal. Mutable
repository, tracker, routing, validation, and approval evidence is freshness-checked where present.
Descriptive text uses the existing `IHandoffRedactionService`; authority/reference text is rejected
if the same detector would transform it.

No chat/transcript/prompt, repository content, credential, model invocation, provider call, routing,
execution runtime, tracker synchronization, validation/approval engine, UI, automatic worktree, or
APO-44+ scope is authorized by this task.

## Validation contract and evidence

- `dotnet restore AIUsageMonitor.sln`
- `dotnet build AIUsageMonitor.sln --no-restore`
- `dotnet test AIUsageMonitor.sln --no-restore`
- Required final totals: Domain 28; Connection 167; Provider 46; Desktop 82; Infrastructure 275;
  Total 598/598; Failed 0; Skipped 0; warnings 0; errors 0.
- Focused APO-43 recovery tests: 32/32 passed.
- `git diff --check`: clean.
- GitHub CI: `NONE / NOT CLAIMED`.

## Governance and handoff boundary

APO-42 is `COMPLETE / MERGED / DONE` at main SHA
`fcbb3e82460f9ed689b446eef16b6c2904d643c6`, tree
`f7920d9a06d3acd4443937b77f5eed45c6210740`; PR #14 merged, SOL-42-01 closed, accepted validation
566/566. APO-43 is implemented but not accepted or Done. Return the exact final feature head to
GPT-5.6 Sol for acceptance. Do not merge PR #15, mark it Ready, transition APO-43 to Done, or
begin APO-44, APO-45, APO-46, model invocation, routing, execution runtime, tracker
synchronization, validation/approval engines, or Mission Control UI.

`OPUS-05-03..11 = DEFERRED / NON-BLOCKING`

`JsonFileStore.CurrentSchemaVersion = UNCHANGED` (remains `1`).

Runtime is explicitly not launched:

`APO PROCESS COUNT = 0`

`APPLICATION LEFT RUNNING = NO`

## Next planner boundary

APO-43 Prompt 3/5 implementation complete and awaiting GPT-5.6 Sol exact-head acceptance. Draft PR
#15 remains `OPEN / DRAFT / UNMERGED`.
