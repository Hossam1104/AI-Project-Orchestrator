# APO-27 SOL DELTA ACCEPTANCE HANDOFF

**Story:** APO-27 - Extend Storage Layout and Stores for APO Projects and Orchestration Records
**Status:** READY FOR GPT-5.6 SOL DELTA ACCEPTANCE
**Assigned executor:** GPT-5.6 Luna xHigh - bounded storage-correctness remediation executor
**Branch:** `feat/APO-27-orchestration-storage`
**Draft PR:** [#5](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/5) - OPEN / DRAFT / UNMERGED
**Jira:** APO-27
**Sol acceptance verdict:** CHANGES REQUIRED - Jira Sol comment `11776`

## Original APO-27 Head and Base

- Original APO-27 implementation/remediation target head: `5e2caa596ba8e67d14ee359302f210149eadb397`
- Exact base `main`: `4b393b3e3cf732dd1f0e861a734e3c311327e2af`
- Previous accepted test total: 162 / 162
- No rebase, merge-main, reset, force-push, destructive clean, replacement branch, or replacement PR was used.

## Remediation Commits and Final Functional SHA

- Remediation commit: `dcfa922b58c0282311ec1e027d1187bee771651b`
- Exact final functional pushed SHA: `dcfa922b58c0282311ec1e027d1187bee771651b`
- The final branch may receive this handoff metadata commit after the functional remediation; the
  functional SHA above is the exact Sol delta-acceptance target.
- PR #5 remains OPEN, DRAFT, and UNMERGED against `main`.

## R-01 Execution History

### Record Identity

Each `ExecutionRun` checkpoint now carries a non-empty unique `RecordId` in addition to its
lifecycle `RunId`. `RecordId` identifies one durable appended checkpoint; `RunId` correlates the
same lifecycle across checkpoints. `ProjectId`, status, start/completion metadata, and the existing
work-item/task/agent/model/outcome/stop/contract references remain persisted.

### RecordedAt Semantics

`RecordedAt` is the append/history timestamp. It is validated as not earlier than `StartedAt` where
the lifecycle relationship is meaningful. `StartedAt` remains business lifecycle metadata and is
not used as the history timestamp.

### Monthly Partitioning

Run append paths select the UTC monthly JSONL partition from `RecordedAt`. Run range filtering and
chronological ordering also use `RecordedAt`, with `RecordId` as the deterministic tie-breaker.

### Cross-Month Lifecycle Regression

The focused suite proves one `RunId` with an August 31 start checkpoint and a September 1 review
checkpoint persists to August and September partitions respectively. A September-only range query
returns the review checkpoint, and both distinct record IDs remain independently reconstructable.

## R-02 Review Finding Traceability

### Finding Identity

`ReviewMetadata` now persists a normalized read-only collection of bounded
`ReviewFindingMetadata` records. Each finding has a stable nonblank string `FindingId` such as
`OPUS-01`, `SOL-02`, or `SEC-04`. Duplicate finding IDs within a review are rejected.

### Affected References

Each finding persists a nonblank `AffectedReference` for a BRD requirement, acceptance criterion,
file/area, or other non-secret traceability identifier.

### Disposition

Each finding persists a separate nonblank `Disposition` such as `Open`, `Accepted`, `Rejected`,
`Remediated`, or `Deferred`; it is not inferred from or collapsed into the aggregate review
`Verdict`.

### Blocking

Each finding preserves its independent `Blocking` flag. Aggregate review verdict, severity,
blocking, finding count, evidence reference, and summary fields remain for compatibility.

### Evidence References

Each finding may preserve typed `EvidenceIds` and bounded non-secret `EvidenceReferences`, plus an
optional bounded summary. No review workflow behavior, reviewer execution, source code, prompts,
raw diffs, conversations, or credentials were added or persisted.

## R-03 Activity / Evidence Traceability

### Task Reference

`ActivityAuditRecord` now carries optional `TaskReference` while preserving project, activity, run,
actor, action, time, outcome, and summary fields.

### Multiple Evidence IDs

Activity records now carry a normalized immutable/read-only `EvidenceIds` collection. Empty IDs are
rejected and duplicates are removed. The prior singular `EvidenceId` remains readable for existing
records and is folded into the collection for compatibility; activity is no longer limited to one
evidence item.

### Requirement References

`EvidenceMetadata` now carries a normalized read-only `RelatedRequirementReferences` list of
nonblank metadata-only strings, including values such as `FR-PROJ-003`, `FR-REV-003`, and
`FR-AUD-001`. Evidence remains metadata/reference storage and is not an arbitrary blob store.

## R-04 Enum Validation

The application model boundaries now fail closed with `ArgumentException`-compatible validation
for undefined persisted values of:

- `ProjectStatus`;
- `AgentConnectionMode`;
- `AgentAvailability`; and
- `ExecutionRunStatus`.

Manually authored invalid numeric project/agent JSON and execution-run JSONL records are skipped by
the existing repository/store mapping boundaries. Valid sibling records remain readable, and no
undefined enum reaches Application consumers.

## R-05 DefaultBranch Semantics

### Repository-Backed Project

A meaningful repository provider, URL, ID, or repository metadata configuration requires a nonblank
`DefaultBranch`. Repository-backed `main` remains valid; a repository-backed project with a blank
branch is rejected.

### Non-Repository Project

`DefaultBranch` is now nullable/optional when no repository is configured. Null and blank values are
normalized to null, are accepted, and round-trip through `projects.json`.

## R-06 Storage Layout

### Legacy Root

The application storage identity remains `ApplicationDirectoryName = "AIUsageMonitor"`, and the
legacy `%LOCALAPPDATA%\AIUsageMonitor\` root and existing non-APO-27 paths are unchanged.

### Project Orchestration Directory

`ApplicationDataPaths.GetProjectOrchestrationDirectory(Guid projectId)` and
`ProjectDataPaths.OrchestrationDirectory` are the explicit path seam. The final project layout is:

```text
%LOCALAPPDATA%\AIUsageMonitor\
    projects\
        {project-guid}\
            routing-policy.json
            orchestration\
                runs\
                    YYYY-MM.jsonl
                evidence\
                    YYYY-MM.jsonl
                reviews\
                    YYYY-MM.jsonl
                activity\
                    YYYY-MM.jsonl
```

### Runs

`orchestration\runs\YYYY-MM.jsonl` stores execution checkpoints partitioned and queried by
`RecordedAt`.

### Evidence

`orchestration\evidence\YYYY-MM.jsonl` stores bounded evidence metadata, requirement references,
validator/artifact/content-hash references, and summaries only.

### Reviews

`orchestration\reviews\YYYY-MM.jsonl` stores project/run review metadata and finding-level
traceability only.

### Activity

`orchestration\activity\YYYY-MM.jsonl` stores project/run/task/activity/evidence audit metadata.
Routing policy remains directly under the GUID project directory outside `orchestration`.

## R-07 Regression Expansion

### Project Registry

Covered same-ID replacement without duplicates, Archived round-trip, non-repository nullable branch,
repository missing-branch rejection, concurrent distinct upserts, invalid `ProjectStatus`, corrupt
document quarantine/recovery, and unsupported-schema quarantine/recovery.

### Agent Registry

Covered invalid `AgentConnectionMode` and `AgentAvailability` values failing closed while valid
siblings remain readable, plus the existing round-trip.

### Routing

Covered global/project override persistence and project A/project B isolation.

### Execution History

Covered unique record identity, same-RunId cross-month checkpoints, `RecordedAt` partition/range
semantics, invalid status isolation, and parallel checkpoint appends.

### Evidence

Covered related requirement reference round-trip/deduplication and representative serialized
metadata containing no raw-output fixture or secret payload.

### Reviews

Covered two findings in one review with IDs, affected references, severity, disposition, blocking,
evidence IDs/references, and project isolation.

### Activity

Covered task reference, multiple evidence IDs, chronological range ordering, and malformed/
unsupported JSONL record isolation.

### Recovery

Preserved and exercised unsupported-schema JSON quarantine, malformed JSONL isolation, unterminated
tail recovery, valid partition preservation, atomic writes, per-path synchronization, and temporary
file cleanup.

## Focused Storage Tests

Command:

```text
dotnet test tests\AIUsageMonitor.Infrastructure.Tests\AIUsageMonitor.Infrastructure.Tests.csproj --no-restore --filter FullyQualifiedName~ProjectOrchestrationStorageTests
```

Exact result: **22 executed, 22 passed, 0 failed, 0 skipped**.

## Full Test Suite

Command:

```text
dotnet test AIUsageMonitor.sln --no-restore
```

Exact result: **176 executed, 176 passed, 0 failed, 0 skipped**.

| Test project | Passed |
|---|---:|
| Domain | 28 |
| Provider | 46 |
| Infrastructure | 72 |
| Connection | 10 |
| Desktop | 20 |

## Release Build

`dotnet restore AIUsageMonitor.sln` succeeded. `dotnet build AIUsageMonitor.sln --no-restore`
succeeded with **0 warnings, 0 errors**.

## Publish

### win-x64

The existing `win-x64` self-contained single-file publish profile succeeded:

```text
dotnet publish src\AIUsageMonitor.Desktop\AIUsageMonitor.Desktop.csproj -c Release -p:PublishProfile=win-x64 --no-restore
```

The published `AIUsageMonitor.Desktop.exe` was produced successfully.

### win-x86

Not rerun. Previous accepted compile/publish evidence is retained because no project, package, or
publish configuration changed.

### win-arm64

Not rerun. Previous accepted compile/publish evidence is retained because no project, package, or
publish configuration changed.

## Runtime Smoke

The published win-x64 executable launched on the Windows x64 development machine, remained running
after a five-second startup interval, and closed cleanly. No screenshot was required because this
remediation has no visible UI change. No live authenticated provider interaction was attempted.

## git diff --check

Passed with no whitespace errors. Git emitted only normal Windows LF/CRLF normalization notices.

## Secret Scan

The targeted added-line scan covered 1,004 added lines and found **0 high-confidence credential
literals**. No raw credentials, tokens, cookies, prompts, conversations, source code, or
authenticated payloads were added to persistence contracts or fixtures.

## Working Tree and PR

The functional remediation is committed on the existing feature branch. Draft PR #5 is reused and
remains OPEN / DRAFT / UNMERGED. No Jira writes, Opus invocation, merge, rebase, force-push, reset,
destructive clean, replacement branch, or replacement PR was performed.

## Documentation

`.ai/CURRENT_STATE.md` records the Sol CHANGES REQUIRED verdict/comment, R-01 through R-07
remediation, exact storage layout, checkpoint identity/time semantics, finding-level review
contract, activity/evidence traceability, enum fail-closed behavior, DefaultBranch semantics,
validation counts, publish/smoke evidence, limitations, and the pending Opus review boundary.

## Known Limitations

- x86 and ARM64 are retained compile/publish evidence from the x64 development machine; no
  corresponding hardware runtime is claimed.
- This is a storage foundation only. It does not implement project UI, routing, classification,
  executor/autonomous runtime, validation/review engines, activity UI, provider changes,
  tracker/GitHub execution, cloud sync, database/ORM, LocalAppData migration, installer/updater,
  signing, or APO-33 CI.
- Storage contracts persist bounded metadata and references only; future callers must keep free-form
  summaries and references non-secret.

## Deferred Scope

Projects UI, routing engine, classifier, model selection, executor runtime, autonomous loops,
GitHub execution, Jira/Azure execution, validation engine, review execution, Activity UI, provider
changes, cloud backend, database/ORM, LocalAppData migration, installer/updater/signing, APO-33 CI,
merge, Jira writes, Opus invocation, and downstream Story work remain deferred.

## Next Planner Boundary

GPT-5.6 Sol must perform delta acceptance against the exact final functional remediation SHA
`dcfa922b58c0282311ec1e027d1187bee771651b`. If Sol accepts the remediated head, the required
independent Claude Opus 5 architecture review remains next. The Draft PR stays open and unmerged.

No Claude Opus review prompt is included in this file.
