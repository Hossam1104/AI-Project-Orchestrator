# APO-42 - Structured Planner-to-Executor-to-Reviewer Handoff Packages

## Active execution contract

- Prompt: `2/5R` - SOL-42-01 redaction and authority-identity remediation
- Jira: `APO-42` (issue id `10870`), parent `APO-10`, priority `High`
- Planner / acceptance authority: GPT-5.6 Sol
- Assigned executor: GPT-5.6 Luna xHigh
- Jira status: `In Progress`
- Jira implementation-start comment: `12137`
- Feature branch: `feat/APO-42-structured-handoff-packages`
- Authorized starting main SHA: `86f53c549c19ab698a8257b3f3c57ee8b5449ffa`
- Authorized starting main tree: `4b05cd85c8f6434ba3750c444e0c66da671bd18a`
- Original APO-42 functional commit: `93e759646c0098974eb089c72b50d8f3cecf24f1`
- SOL-42-01 remediation starting handoff: `90497896222063bf796d7720458242c952d14f75`
- SOL-42-01 functional remediation commit: `8cdd7d20d5e5e875d638312c2f3bbf44d9082d07`
- Draft PR: `#14 OPEN / DRAFT / UNMERGED`, base `main`

## Delivered scope

APO-42 is implemented and SOL-42-01 remediation is complete; the final metadata handoff awaits Sol
exact-head acceptance. The implementation provides a
provider-independent immutable structured handoff authority, an application creation service, and
project-isolated create-once JSON persistence. It resolves the exact requested APO-40 planning
contract revision, derives work-item/repository/scope authority from that contract, optionally
validates an exact APO-41 graph/node binding, validates shallow same-project predecessor lineage,
applies role-specific inclusion policy, redacts bounded descriptive text, rejects secret-shaped
authority/identity/reference values before persistence, enforces a 128 KiB canonical
payload budget, and calculates deterministic SHA-256 content-integrity evidence.

Supported transitions are exactly:

1. `PlannerToExecutor`
2. `ExecutorToReviewer`
3. `ReviewerToRemediation`
4. `RemediationToReviewer`
5. `ReviewerToAcceptance`
6. `AcceptanceToPlanner`

The package carries typed work-item, exact contract, point-in-time context, planner-defined
repository target, optional graph/node, predecessor, role-relevant scope, evidence/finding and
changed-artifact references, bounded outcome/limitations/next action, redaction metadata, size
metadata, and `ContentHash`. It contains no raw prompts, transcripts, chat, source, repository
contents, diffs, full logs, provider-specific format, model selection, or execution behavior.

Persistence uses `projects/<ProjectId>/handoffs/<PackageId>/package.json`, exact GUID-derived
identity, `CreateNewAsync`, and `ReadPreservingAsync`. Malformed, unsupported, and tampered reads
are observational and never quarantine, rewrite, repair, rename, move, delete, or create a
backup. `JsonFileStore.CurrentSchemaVersion` remains unchanged at `1`.

Redaction recognizes password assignments, API-key assignments, bearer/authorization values,
common PAT/token prefixes, and connection-string passwords. Descriptive values retain only the
stable marker, bounded count, and categories; authority, identity, and reference values are kept
exactly when safe and otherwise fail closed as `RedactionRejected` without persistence. Original
values and secret hashes are not retained. This is conservative defense-in-depth, not a universal
secret-detection guarantee. Quoted password and API-key assignments containing spaces are covered.

## Validation evidence

- `dotnet restore AIUsageMonitor.sln`: SUCCESS.
- `dotnet build AIUsageMonitor.sln --no-restore`: SUCCESS; 0 warnings, 0 errors.
- `dotnet test AIUsageMonitor.sln --no-restore`: 566/566 passed; 0 failed; 0 skipped.
- Suite totals: Domain 28; Connection 167; Provider 46; Desktop 82; Infrastructure 243.
- Focused remediation tests: HandoffRedactionTests 10/10; HandoffPackageServiceTests 26/26;
  HandoffPackagePersistenceTests 17/17.
- `git diff --check`: clean.
- Changed-line credential-shaped scan: only intentional redaction patterns/fixtures; no real
  credentials detected.
- GitHub CI: `NONE / NOT CLAIMED`.

## Governance and boundary

APO-41 is fully merged/closed and Done at the authorized baseline SHA and tree above. SOL-42-01 is
remediated and APO-42 is not accepted; it must remain In Progress until Sol reviews this exact
final branch head. Do not
merge PR #14, mark it Ready, transition APO-42 to Done, or begin APO-43, APO-44, APO-45, or any
other downstream Story. Do not invoke Opus or Sonnet.

No APO runtime launch is permitted for this work item:

`APO PROCESS COUNT = 0`

`APPLICATION LEFT RUNNING = NO`

## Metadata handoff

This file and `.ai/CURRENT_STATE.md` are the only files authorized for the post-functional
metadata-only commit. The final branch head is the SHA of that metadata-only commit and is captured
in the executor completion report and Jira handoff comment. The next action is GPT-5.6 Sol exact-head
acceptance of SOL-42-01/APO-42.
