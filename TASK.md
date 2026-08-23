# APO-27 REAL CLAUDE OPUS 5 REVIEW HANDOFF

**Story:** APO-27 - Extend Storage Layout and Stores for APO Projects and Orchestration Records
**Status:** READY FOR REAL CLAUDE OPUS 5 INDEPENDENT ARCHITECTURE REVIEW
**Owner:** Hossam
**Planner / acceptance authority:** GPT-5.6 Sol
**Executor:** GPT-5.6 Luna xHigh - bounded storage-remediation executor
**Branch:** feat/APO-27-orchestration-storage
**Draft PR:** #5 - OPEN / DRAFT / UNMERGED
**Jira:** APO-27 - In Progress
**Parent Epic:** APO-3 - In Progress
**Owner approval comment:** 11778

This file is the next executable contract only for the required independent Claude Opus 5 review.
It does not authorize implementation of another Story, merge, PR readiness, main-branch changes,
Jira status transitions, or downstream orchestration work.

## Exact repository and commit identity

| Item | Exact value |
|---|---|
| Required remediation start head | a6ef753143996f31591d300dfc56fbfa9bbb50a4 |
| Exact main base | 4b393b3e3cf732dd1f0e861a734e3c311327e2af |
| Original APO-27 implementation SHA | d38817f96050b0decfd0a8328f8ef2cd33bc5a5e |
| Prior Sol remediation target/start SHA | 5e2caa596ba8e67d14ee359302f210149eadb397 |
| Prior functional remediation SHA | dcfa922b58c0282311ec1e027d1187bee771651b |
| Owner-approved remediation start SHA | a6ef753143996f31591d300dfc56fbfa9bbb50a4 |
| New functional remediation SHA | 0ea0c65ec7dac7ec09d30a6b25156353b714298f |
| Final pushed SHA / exact Opus review target | 0ea0c65ec7dac7ec09d30a6b25156353b714298f |

The final pushed SHA above is the exact functional remediation target. The branch may contain the
separate documentation/handoff commit after that functional commit; the documentation-only
commit must not change the functional review target. Verify the final branch head with
git rev-parse HEAD and git rev-parse origin/feat/APO-27-orchestration-storage before review.

## Full architecture summary

APO-27 is a local persistence foundation on the approved C# / .NET 10 / WPF / MVVM architecture.
Application owns provider-independent orchestration contracts. Infrastructure owns JSON/JSONL
files, monthly partitions, safe writes, path derivation, logging, and OS/file-system details.
The existing %LOCALAPPDATA%\AIUsageMonitor\ root and accepted provider/capacity storage remain
unchanged. V1 still has no database, ORM, cloud backend, embedded browser, Node/npm prerequisite,
provider CLI prerequisite, or LocalAppData migration.

The project layout remains GUID-derived and isolated:

    %LOCALAPPDATA%\AIUsageMonitor\
        projects\
            {project-guid}\
                routing-policy.json
                orchestration\
                    runs\YYYY-MM.jsonl
                    evidence\YYYY-MM.jsonl
                    reviews\YYYY-MM.jsonl
                    activity\YYYY-MM.jsonl

No Projects UI, activity UI, routing engine, execution runtime, task classifier, model selector,
validation engine, review engine, acceptance engine, provider change, tracker/GitHub executor,
cloud backend, database/ORM, migration, installer, signing, updater, or APO-33 CI was added.

## Blocker A remediation - review truthfulness

ReviewMetadata.Findings is authoritative.

- FindingCount is derived from Findings.Count and is no longer independently writable.
- Blocking is derived from finding-level blocking flags. A review with zero findings is
  non-blocking.
- Blank finding IDs, null finding entries, and case-insensitive duplicate finding IDs are rejected.
- Finding evidence IDs/references remain normalized and bounded; finding summaries remain optional.
- Verdict text such as accepted, changes-required, or rejected remains metadata and is not
  interpreted as a workflow engine.

ReviewMetadataRecord retains aggregate FindingCount and Blocking as derived serialized
inspection/compatibility fields. FromApplication always writes both from the detailed finding
collection. ToApplication derives the expected count and blocking state from the persisted
finding collection and rejects any contradictory persisted value before constructing Application
state. Invalid review records are skipped by the project store with a CorruptRecord read issue.

Required truthfulness coverage is present:

- two findings produce FindingCount == 2;
- one blocking plus one non-blocking finding produces aggregate Blocking == true;
- zero findings produce count zero and non-blocking state;
- duplicate IDs are rejected case-insensitively;
- blank and null findings are rejected;
- contradictory persisted count and blocking fields are rejected fail-closed;
- a valid derived aggregate round-trips without contradiction.

## Blocker B remediation - history read truthfulness

The Application contract now returns HistoryReadResult<T> from all four orchestration read methods:

    IProjectOrchestrationStore
        ReadExecutionRunsAsync -> HistoryReadResult<ExecutionRun>
        ReadEvidenceAsync      -> HistoryReadResult<EvidenceMetadata>
        ReadReviewsAsync       -> HistoryReadResult<ReviewMetadata>
        ReadActivityAsync      -> HistoryReadResult<ActivityAuditRecord>

Each result contains Records, HistoryReadStatus Status, and HistoryReadIssue Issues.

### Status semantics

- Success: requested partitions were read without storage or record-integrity failure. Missing
  history directories, missing monthly files, and empty partitions are normal absence and return
  Success with empty records.
- Partial: readable records are preserved but one or more malformed/unsupported records or
  permission/I/O failures may make the requested history incomplete.
- Unavailable: permission/I/O failure prevented every requested partition from being reliably
  read. No empty result is allowed to hide that failure.

### Issue semantics and privacy

HistoryReadIssue contains only Kind, a partition filename such as 2026-08.jsonl, and a bounded
NonSecretMessage. The kinds are CorruptRecord, UnsupportedSchema, PermissionFailure, and IoFailure.
Exception objects, exception text, absolute paths, raw JSONL lines, prompts, source code,
authenticated payloads, and credentials never cross the Application boundary. Logging may retain
safe diagnostics according to the existing policy.

JsonlEventStore.ReadRangeWithStatusAsync is additive. Existing accepted usage-history
ReadRangeAsync remains available with its original signature and behavior. Orchestration storage
uses the richer API, preserves valid records beside malformed/unsupported siblings, reports
unterminated-tail corruption as degraded, and maps Infrastructure failures into provider-neutral
issues. A small Infrastructure-internal partition-reader seam makes permission/I/O tests
deterministic without ACL mutation or a generic filesystem abstraction.

## Compatibility and project isolation

- Legacy %LOCALAPPDATA%\AIUsageMonitor\ identity is preserved.
- Existing provider/capacity files and schemas are untouched.
- APO-27's unmerged review record schema keeps its serialized derived aggregate fields but rejects
  contradictions before Application construction.
- Paths are projects\{guid}\; no names, local paths, repository URLs, or tracker identifiers
  select a storage directory.
- Records are checked against both the derived project directory and their persisted ProjectId.
- Foreign ProjectId records remain filtered even when the read result is Partial.
- Valid records from a readable month are retained when another requested month fails.

## Focused and regression tests

The focused command is:

    dotnet test tests\AIUsageMonitor.Infrastructure.Tests\AIUsageMonitor.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ProjectOrchestrationStorageTests

Result: 32 executed, 32 passed, 0 failed, 0 skipped.

The 32-test class covers the original 22 storage regressions plus:

- derived review count/blocking truth;
- blank, null, and duplicate finding rejection;
- persisted review aggregate round-trip;
- persisted count/blocking mismatch rejection;
- missing project history directory as Success + empty;
- existing empty partition as Success + empty;
- malformed and unsupported sibling preservation with Partial issues;
- unterminated JSONL tail recovery with valid records preserved;
- one valid month plus I/O failure as Partial;
- permission failure with no readable partition as Unavailable; and
- project isolation under a degraded/partial read.

## Full solution validation

    dotnet restore AIUsageMonitor.sln
    dotnet build AIUsageMonitor.sln --no-restore
    dotnet test AIUsageMonitor.sln --no-restore

All succeeded. Build result: 0 warnings, 0 errors. Full test result: 186 executed, 186
passed, 0 failed, 0 skipped.

| Test project | Passed |
|---|---:|
| Domain | 28 |
| Provider | 46 |
| Infrastructure | 82 |
| Connection | 10 |
| Desktop | 20 |

## Build, publish, and runtime

The existing project/package/publish configuration did not change.

### win-x64

    dotnet publish src\AIUsageMonitor.Desktop\AIUsageMonitor.Desktop.csproj -c Release -p:PublishProfile=win-x64 --no-restore

Succeeded and produced the self-contained single-file AIUsageMonitor.Desktop.exe. Startup smoke
on the Windows x64 development machine confirmed the published executable remained alive after a
five-second interval and closed cleanly. No visible UI changed, so no screenshot was required.

### win-x86 and win-arm64

Not rerun. Prior accepted compile/publish evidence remains valid because no project, package, or
publish configuration changed. No x86 or ARM64 hardware runtime is claimed.

## Diff, secret, and artifact checks

- git diff --check: passed; no whitespace errors, only normal Windows LF/CRLF notices.
- Added-line secret scan: 668 tracked added lines scanned with 0 high-confidence credential
  literals; the four new Application contract files were separately inspected with the same
  result. No real credentials, tokens, cookies, prompts, conversations, source code, or
  authenticated payloads were added.
- Generated build/publish output is ignored and no generated artifacts are part of the commit.

## Markdown synchronization

All tracked Markdown files were enumerated and reviewed:

- .ai/CURRENT_STATE.md
- AGENTS.md
- CLAUDE.md
- README.md
- TASK.md
- docs/APO-31_PROVIDER_EVIDENCE.md
- docs/BRD.md
- docs/IMPLEMENTATION_PLAN.md
- docs/LEGACY_IMPLEMENTATION_MAP.md
- docs/SESSION_PROMPTS.md

### Changed

- .ai/CURRENT_STATE.md: current APO-27 lifecycle, exact SHAs, two fixed blockers, three deferred
  P3 observations, validation, and Opus boundary synchronized.
- TASK.md: replaced the prior Sol-delta handoff with this full real Claude Opus 5 review contract.
- README.md: current status now records APO-27 storage as under review and updates the branch
  validation count without advertising future orchestration capability.
- docs/IMPLEMENTATION_PLAN.md: current planning boundary now points to the Opus handoff.

### Intentionally unchanged

AGENTS.md, CLAUDE.md, docs/BRD.md, docs/APO-31_PROVIDER_EVIDENCE.md,
docs/LEGACY_IMPLEMENTATION_MAP.md, and docs/SESSION_PROMPTS.md contain permanent governance,
requirements, prior-provider evidence, historical mapping, or no stale APO-27 current-state claim.
They were reviewed and not rewritten. BRD review result: no requirement changes required.

Historical APO-27 SHAs remain where needed as factual history. No stale current APO-27 execution
state remains, including the obsolete “awaiting GPT-5.6 Sol delta acceptance” wording.

## Jira synchronization

Authoritative Jira state remains:

- APO-27: In Progress;
- APO-3: In Progress; and
- owner approval comment: 11778.

No duplicate Jira status transition was made and neither issue was marked Done. Completion comment
`11787` was added through the configured Jira connector and records this functional SHA, final
branch SHA, blocker resolutions, 32/32 focused tests, 186/186 full tests, publish/smoke evidence,
Markdown synchronization, deferred P3 observations, and the next gate.

## Deferred non-blocking P3 observations

- Metadata value secret hardening remains deferred to future explicit metadata contracts/allowlists
  and secret scanning/redaction in integration adapters.
- FilePathLocks keyed semaphore lifetime remains deferred; no ref-counted lock redesign was
  authorized.
- Explicitly reused RecordId global deduplication remains deferred to the future execution runtime;
  storage append does not own idempotency/replay behavior.

## Exact next gate

The real Claude Opus 5 independent architecture review must target:

    0ea0c65ec7dac7ec09d30a6b25156353b714298f

The reviewer must independently inspect the actual code, diff, evidence, zero-prerequisite
consumer contract, cross-Windows behavior, provider-neutral truthfulness, file persistence,
credential/privacy boundaries, project isolation, and the human-approval/release gates. Opus has
not reviewed this remediation yet. After Opus, GPT-5.6 Sol remains the acceptance authority.

No merge, PR-ready transition, main-branch modification, Jira status transition, or downstream
Story work was performed.
