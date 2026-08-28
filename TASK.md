# AI PROJECT ORCHESTRATOR — APO-68 PROMPT 1/5R SOL EXACT-HEAD RE-REVIEW

## Fresh-context handoff

This handoff records the bounded SOL-68-01 remediation delivered by GPT-5.6 Luna xHigh for
AI Project Orchestrator (APO), Jira APO-68, on the existing feature branch. GPT-5.6 Sol must
perform the next exact-head review. This remediation does not advance the five-prompt review
counter. APO-45 remains `TO DO / NOT AUTHORIZED`.

No Claude Opus, Claude Sonnet, Terra, Gemini, or other model was invoked. No APO runtime was
launched. No product execution runtime, provider, model invocation, tracker application, UI,
cleanup, or schema migration work was started.

## Exact identity and ancestry

- Repository: `Hossam1104/AI-Project-Orchestrator`
- Local root: `D:\AI Tools\Active Projects\AI-Project-Orchestrator`
- Jira: `APO-68`; Sol review comment: `12236`
- Branch: `feat/APO-68-workspace-pre-runtime-hardening`
- Draft PR: `#18`, base `main`, required state `OPEN / DRAFT / UNMERGED`
- Authorized main SHA/tree: `9ebf935244bbb6a16aef0aeeefe55bcdf6c6896f` /
  `83bf373165377df69ef6a304af5ad14f973d7348`
- Prior exact Sol-reviewed head/tree: `91fb7685c4de9863915678d5793f642c66d0a6f5` /
  `d78f6d93896eed865e32570c682b77a2eb35cc87`
- Original APO-68 functional SHA/tree: `407b891175d58fa39374a31a7e4884553f115d87` /
  `a0f30ef43150017c6c108d99b27942e781bb5c4c`
- Remediation functional SHA/tree: `f685069ef12d3ab5caf4460d14642b82103deb1c` /
  `9e68b3eba1373a895a7ac6dfa933d4506116ed24`
- The remediation functional commit is directly descended from the prior reviewed head and
  contains only the production authority fix and its tests.
- The final feature head is the later metadata-only handoff commit containing only this file and
  `.ai/CURRENT_STATE.md`; its exact SHA/tree are recorded in the final executor report and PR #18
  after that commit.

## SOL-68-01 finding and exact fix

The blocking finding was that `JsonWorkspacePreparationApprovalEvidenceRepository.GetForPlanAsync`
returned ordinary `Missing` when a valid deterministic plan-index record pointed to a canonical
approval whose immutable evidence was missing. `FindForPlanAsync` interpreted that `Missing` as an
absent index and searched canonical evidence, allowing an alternate approval to be adopted around
a present broken authority selector.

The production fix is minimal:

- `GetForPlanAsync` continues to return the index read result unchanged when the index record is
  absent, invalid, corrupt, unsupported, migration-required, or unavailable.
- Once a deterministic index record has been successfully read and validated as present, a missing
  indexed canonical approval is converted to `WorkspacePreparationApprovalEvidenceReadState.IntegrityFailure`
  with the deterministic message `Approval evidence plan index points to missing immutable approval evidence.`
- Other indexed canonical failures (`Invalid`, `IntegrityFailure`, `MigrationRequired`,
  `UnsupportedFutureVersion`, and `Unavailable`) continue to propagate fail closed.
- `FindForPlanAsync` can enumerate bounded canonical evidence only when `GetForPlanAsync` reports
  `Missing` for the index record itself. A present broken index cannot be searched around, repaired,
  or replaced, and no latest/timestamp authority is selected.
- `EnsurePlanIndexAsync` remains create-once. A valid missing-index case can create only the missing
  index; a present index whose target is missing, corrupt, invalid, conflicting, or unreadable
  returns a deterministic conflict/unavailable result without deleting or overwriting the index.
- No persisted discriminator or schema field was added.

## Required broken-index evidence

The following real JSON repository tests were added to
`WorkspacePreparationApo68Tests`:

1. `PresentPlanIndexWithMissingCanonical_DoesNotFallbackToAlternateAuthority`
   - creates canonical A and its deterministic index, preserves index bytes, deletes only A,
     creates canonical B with the exact same plan reference without replacing the index, and calls
     both indexed and observational lookup;
   - both results are `IntegrityFailure`, never `Valid` or ordinary `Missing`;
   - B is not selected; index bytes and B bytes are unchanged; no `.bak`/`.tmp` artifacts occur.
2. `PresentPlanIndexWithMissingCanonical_FailsClosedWithoutAlternate`
   - creates A and its index, deletes only A, and calls `GetForPlanAsync` and `FindForPlanAsync`;
   - both results are `IntegrityFailure`, and the original index bytes remain unchanged.
3. `EnsurePlanIndexWithMissingIndexedCanonical_ReturnsConflictWithoutReplacingIndex`
   - leaves the index pointing to missing A, creates B as an immutable canonical record while
     preserving the index, and calls `EnsurePlanIndexAsync(B)`;
   - result is `Conflict`; the present index and B bytes are unchanged; no repair or alternate
     authority is selected.
4. `InspectWithPresentBrokenPlanIndex_FailsClosedWithoutReceiptOrGitMutation`
   - uses a persisted valid plan, exact source repository registration, valid workspace-local
     verification, no receipt, a present index pointing to missing A, and alternate B;
   - `InspectAsync` returns `WorkspaceRecoveryState.IntegrityFailure`, never
     `PreparedWithoutReceipt` or `PreparedAndRecorded`; Git mutation count is zero, no receipt is
     written, the index is not repaired, and B is not adopted.

The existing `ApprovalEvidenceRepository_MissingIndexFindsExactCanonicalAndRepairsIndexOnly` test
was strengthened to assert `GetForPlanAsync` is genuinely `Missing` before fallback. It remains the
legitimate crash-window regression: canonical approval exists, only the deterministic index is
missing, exact recovery succeeds, and explicit retry creates only that index.

## Preserved findings and scope

- OPUS-46R-01 remains unchanged: independent read-only/worktree-mutation/drain bounds, sole
  `worktree add -b` Git mutation, managed workspace-local verification, and fail-closed partial or
  timeout behavior without cleanup or rollback.
- OPUS-46R-03 remains unchanged: normalized repository identity is used for path comparison and
  file-lock hashing, with actual equivalent-identity serialization coverage.
- OPUS-46R-04 remains unchanged: inherited Git repository-redirection variables are removed while
  PATH/unrelated environment values and deterministic prompt/lock/locale settings are preserved.
- No forbidden Git command or network path was added. Only product `worktree add -b` may mutate Git.
- No R-01/R-03/R-04 redesign, APO-45 work, source refactor, provider, runtime, UI, cleanup, or
  speculative Jira item was added.

## Validation at remediation functional head

Functional validation was run at
`f685069ef12d3ab5caf4460d14642b82103deb1c` / `9e68b3eba1373a895a7ac6dfa933d4506116ed24`:

- `dotnet restore AIUsageMonitor.sln`: SUCCESS; all projects up to date.
- `dotnet build AIUsageMonitor.sln --no-restore`: SUCCESS; 0 warnings; 0 errors.
- `dotnet test AIUsageMonitor.sln --no-restore`: 806 passed; 0 failed; 0 skipped.
- Suite totals: Domain 28; Connection 193; Provider 46; Desktop 82; Infrastructure 457.
- Focused `WorkspacePreparationTests`: 9/9 passed.
- Focused `WorkspacePreparationRemediationTests`: 22/22 passed.
- Focused `WorkspacePreparationAcceptanceTests`: 100/100 passed.
- Focused `SystemGitCommandRunnerTests`: 4/4 passed.
- Focused `WorkspacePreparationApo68Tests`: 16/16 passed.
- Combined focused workspace/APO-68 total: 151/151 passed.
- `git diff --check`: clean before functional commit and after exact functional validation.
- Changed-line credential-shaped review found no real credentials; only sanitized fixture values and
  security-related identifiers are present.
- No tracked generated, corruption, quarantine, `.bak`, or `.tmp` artifacts were found.

## Schema, CI, Jira, and runtime truth

- `JsonFileStore.CurrentSchemaVersion = 1`.
- Workspace preparation plan schema = V1; receipt schema = V1; approval evidence schema = V1.
- `OPUS-05-03..11 = DEFERRED / NON-BLOCKING`; no schema migration was performed.
- GitHub CI must be reported from the final feature head. If statuses and workflow runs remain zero,
  report exactly `GitHub CI = NONE / NOT CLAIMED`; do not create CI.
- APO-68 remains `IN PROGRESS`; do not transition it to Done. APO-45 remains `TO DO / NOT
  AUTHORIZED`; do not change it.
- At completion verify and report `APO PROCESS COUNT = 0` and `APPLICATION LEFT RUNNING = NO`.
- Do not launch APO under this remediation.

## Sol exact-head re-review checklist

Sol must independently verify at the final feature head:

1. exact ancestry from authorized main through the prior reviewed head, remediation functional
   commit, and metadata-only handoff;
2. the final commit changes only `TASK.md` and `.ai/CURRENT_STATE.md`;
3. `GetForPlanAsync` no longer returns ordinary `Missing` when a present index targets missing
   canonical evidence;
4. `FindForPlanAsync` does not search around a present broken index;
5. alternate canonical evidence cannot be adopted;
6. `EnsurePlanIndexAsync` cannot replace or repair a present broken index;
7. genuinely missing-index crash recovery still finds exact canonical evidence and repairs only the
   missing index;
8. service `InspectAsync` fails closed with zero Git mutation and no receipt/index write;
9. R-01, R-03, and R-04 remain unchanged and green;
10. all plan/receipt/approval schemas and `JsonFileStore.CurrentSchemaVersion` remain V1;
11. CI truth is `NONE / NOT CLAIMED` unless independent evidence shows otherwise;
12. APO application is stopped and APO-45 remains unauthorized.

## Delivery boundary

PR #18 must remain `OPEN / DRAFT / UNMERGED` against `main`; do not mark Ready and do not merge.
Do not invoke Claude Opus or Claude Sonnet. Do not begin APO-45. The next gate is GPT-5.6 Sol
exact-head re-review of this remediation.

> APO-68 Prompt 1/5R remediation complete and awaiting GPT-5.6 Sol exact-head re-review. PR #18
> remains OPEN / DRAFT / UNMERGED. Do not begin APO-45. Do not invoke Claude Opus. If Sol accepts
> the exact remediated head, Sol may authorize APO-68 merge finalization because this remains
> Prompt 1/5 of the current cycle.
