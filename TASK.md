# APO-27 SOL ACCEPTANCE HANDOFF

**Story:** APO-27 - Extend Storage Layout and Stores for APO Projects and Orchestration Records
**Status:** READY FOR GPT-5.6 SOL ACCEPTANCE
**Assigned executor:** GPT-5.6 Luna Max - substantial bounded implementation executor
**Base:** `main` at `4b393b3e3cf732dd1f0e861a734e3c311327e2af`
**Branch:** `feat/APO-27-orchestration-storage`
**Implementation SHA:** `d38817f96050b0decfd0a8328f8ef2cd33bc5a5e`
**Final functional branch SHA:** `d38817f96050b0decfd0a8328f8ef2cd33bc5a5e` before this handoff metadata synchronization
**Draft PR:** [#5](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/5) - OPEN / DRAFT / UNMERGED

## Result

APO-27 is implemented as a local persistence foundation. The accepted APO-34 merge is treated as
complete, merged, and done. The old local solution-only owner change was proven to be fully
subsumed by APO-34, removed safely, and replaced by the exact merged `main` state before the
feature branch was created.

## Corrected Local Main Reconciliation

### Old Local Main

- Required old local `main` base: `a585ed40ea0e8652c50e4627ee66f7109c67d591`.
- Verified branch: `main`.
- Verified `HEAD` before reconciliation: `a585ed40ea0e8652c50e4627ee66f7109c67d591`.

### origin/main

- `git fetch origin --prune` completed.
- Verified `origin/main`: `4b393b3e3cf732dd1f0e861a734e3c311327e2af`.
- This is the authoritative APO-34 merge SHA.

### Local Working Delta

`git diff --unified=0 HEAD -- AIUsageMonitor.sln` contained exactly four replacements for the
existing Desktop project GUID `{AA1BB444-5C7F-4F1E-B376-BFE9AE13D8AC}`:

```text
Debug|Any CPU.ActiveCfg: x86 -> x64
Debug|Any CPU.Build.0:  x86 -> x64
Release|Any CPU.ActiveCfg: x86 -> x64
Release|Any CPU.Build.0:  x86 -> x64
```

No project entries, test-project changes, solution-folder changes, other configuration changes,
or unrelated content were present.

### Incoming APO-34 Delta

`git diff --unified=0 HEAD..origin/main -- AIUsageMonitor.sln` contained the same four Desktop
mapping replacements plus the accepted `AIUsageMonitor.Desktop.Tests` and
`AIUsageMonitor.Connection.Tests` project entries, configurations, and test-folder nesting.
`git show origin/main:AIUsageMonitor.sln` independently confirmed the four Desktop Any CPU
mappings resolve to `x64`.

### Local Delta Fully Subsumed

Every local changed line and target value was present in the committed APO-34 incoming delta.
The local solution change was therefore redundant overlap, not an unrelated owner change.

### Redundant Local Change Removed

`git restore --source=HEAD -- AIUsageMonitor.sln` was run only after the corrected guards passed.
`git status --short` was empty before the fast-forward.

### Fast-Forward Result

`git pull --ff-only origin main` completed successfully. Local `main` and `origin/main` both became
`4b393b3e3cf732dd1f0e861a734e3c311327e2af`; the solution diff against `origin/main` was empty.

### Final Local Main

`main` was verified at `4b393b3e3cf732dd1f0e861a734e3c311327e2af`, with both APO-34 test projects
present and the Desktop Any CPU mappings still targeting `x64`. APO-34 received no follow-up
commit.

### Final Working Tree

The working tree was clean immediately before the APO-27 implementation branch was created and
again after the implementation commit. The final handoff metadata commit is the only subsequent
change on the feature branch.

## Branch and Delivery

- Starting SHA: `4b393b3e3cf732dd1f0e861a734e3c311327e2af`.
- Implementation SHA: `d38817f96050b0decfd0a8328f8ef2cd33bc5a5e`.
- Functional final branch SHA before handoff metadata: `d38817f96050b0decfd0a8328f8ef2cd33bc5a5e`.
- Draft PR: [#5](https://github.com/Hossam1104/AI-Project-Orchestrator/pull/5),
  `feat/APO-27-orchestration-storage` -> `main`, OPEN / DRAFT / UNMERGED.
- No Jira writes, merge, force push, rebase, destructive reset, destructive clean, or stash was
  performed.

## Storage Layout

### Legacy Root Compatibility

The existing per-user root remains `%LOCALAPPDATA%\AIUsageMonitor\`. Existing `settings.json`,
provider, connection, subscription, quota, alert, history, sync, and log paths remain unchanged.
`ApplicationDataPaths.CreateDefault()` continues to resolve the Windows
`Environment.SpecialFolder.LocalApplicationData` location and the `AIUsageMonitor` directory.

### Global Documents

```text
%LOCALAPPDATA%\AIUsageMonitor\projects.json
%LOCALAPPDATA%\AIUsageMonitor\agents.json
%LOCALAPPDATA%\AIUsageMonitor\routing-policy.json
```

All three documents use the existing `JsonFileStore` schema-versioned envelope. Project and agent
documents use the existing synchronized JSON collection store; the global routing policy uses a
versioned JSON document.

### Project-Scoped Layout

```text
%LOCALAPPDATA%\AIUsageMonitor\projects\{project-guid}\
    routing-policy.json
    runs\
        yyyy-MM.jsonl
    evidence\
        yyyy-MM.jsonl
    reviews\
        yyyy-MM.jsonl
    activity\
        yyyy-MM.jsonl
```

Every project directory is derived from a non-empty GUID in canonical `D` format. No project
record can supply a filesystem path for another project's streams.

### JSON Documents

- `projects.json`: project identity, local workspace path, repository metadata/default branch,
  tracker metadata, lifecycle status, governance references, routing/safety references, and
  created/updated timestamps.
- `agents.json`: agent/model name, role, provider label, capabilities, limitations, connection
  mode, availability, cost/quota metadata, enabled state, and timestamps.
- `routing-policy.json`: global routing/safety policy values and non-secret rules.
- `projects/{guid}/routing-policy.json`: optional project-specific override values/rules.

No document stores passwords, raw tokens, refresh tokens, cookies, authenticated payloads, prompts,
conversations, source code, or unrelated credentials.

### JSONL Streams

- `runs`: execution-run status/checkpoint metadata.
- `evidence`: validator/evidence metadata and references, not raw output.
- `reviews`: reviewer/verdict/severity/finding-count/blocking metadata and references.
- `activity`: actor/action/outcome/timestamp/run/evidence audit metadata.

Each record includes `schemaVersion`, `recordType`, and `projectId`. Partitions use the UTC month
of the event timestamp and are read through the existing range-aware JSONL event store.

## Project Registry

`IProjectRepository` and `JsonProjectRepository` persist and round-trip:

- `Id`, `Name`, `LocalPath`, and `ProjectStatus` (`Active`, `Paused`, `Blocked`, `Archived`);
- repository provider, URL, identifier, metadata, and `DefaultBranch`;
- tracker type, identifier, and metadata;
- governance references;
- routing-policy and safety-policy references; and
- `CreatedAt` / `UpdatedAt` timestamps with invariant validation.

Metadata dictionaries reject sensitive-looking keys such as token, secret, password, cookie,
credential, authorization, API-key, prompt, conversation, source-code, or payload.

## Agent Registry Persistence

`IAgentRepository` and `JsonAgentRepository` persist small global registry state for agent/model
identity, role, provider label, capability and limitation labels, `Api`/`Cli`/`Sdk`/manual/
interactive/unsupported connection mode, truthful availability, enabled state, cost/quota
metadata, and timestamps. No secure-store material is loaded or written by these stores.

## Routing Policy Persistence

`IRoutingPolicyRepository` and `JsonRoutingPolicyStore` persist one global policy and optional
project-specific overrides. Policy values are nullable so a future routing engine can resolve an
override against global policy without APO-27 inventing execution behavior. Global and project
documents are separate atomic files, and an override for project A cannot be returned for project B.

## Execution Run History

`IProjectOrchestrationStore.AppendExecutionRunAsync` appends non-secret run identity, project,
status, start/completion, work-item reference, task title, agent/model reference, outcome,
stop-reason, and contract-reference metadata to the project's monthly `runs` stream.

## Evidence Metadata

`AppendEvidenceAsync` persists project/run linkage, evidence id, capture time, kind, outcome,
validator/artifact references, content hash, and a bounded summary. Raw command output, source
code, prompts, credentials, and authenticated payloads are not modelled or stored.

## Review Metadata

`AppendReviewAsync` persists project/run linkage, reviewer reference, verdict, severity, blocking
state, finding count, occurrence time, evidence reference, and bounded summary to monthly `reviews`
partitions. It does not implement review parsing or remediation.

## Activity / Audit

`AppendActivityAsync` persists actor, action, outcome, project/run/evidence identifiers, timestamp,
and bounded summary to monthly `activity` partitions. Reads return chronological records for the
requested project and range only.

## Project Isolation

Isolation is enforced in two independent ways:

1. Every append/read path is derived from the requested non-empty project GUID; callers cannot
   select a project directory by passing arbitrary path text.
2. Every JSONL record carries `ProjectId`, and the orchestration store filters records whose ID does
   not equal the directory/request project before mapping them.

Focused tests append project A and B records, manually place a foreign record in project A's
partition, and verify that project A reads never return project B data. Policy override reads also
verify project B remains empty when only project A has an override.

## Schema Versioning

Small JSON documents use the existing explicit `schemaVersion` envelope and JSONL records use the
existing `schemaVersion` plus `recordType` metadata. Unsupported JSON documents are classified and
quarantined; unsupported JSONL records are skipped without blocking valid records.

## Atomicity / Concurrency

JSON writes reuse temporary-file serialization, flush, and replacement. JSON collection update
sequences and policy writes use the existing per-file `SemaphoreSlim` synchronization. JSONL appends
use per-partition exclusive synchronization and flush the stream. The focused suite verifies no
temporary files remain after concurrent writes and that 24 concurrent run appends remain complete.

## Corruption / Recovery

Corrupt or unsupported JSON documents retain the established quarantine behavior. JSONL readers skip
malformed/unsupported records and continue with valid records. An unterminated JSONL tail is
isolated by inserting a newline before the next append, preserving the partial bytes for safe
diagnosis and allowing subsequent records to be recovered. Focused tests cover unsupported policy
recovery and an interrupted run tail.

## Secret Safety

The new contracts contain no raw credential/token/password/cookie/prompt/source/payload fields.
Registry metadata rejects sensitive-looking keys before any write. The staged added-line scan found
no high-confidence secret literals. Existing secure credential behavior and provider boundaries
were not changed.

## Focused Storage Tests

`dotnet test tests\AIUsageMonitor.Infrastructure.Tests\AIUsageMonitor.Infrastructure.Tests.csproj
--no-restore --filter FullyQualifiedName~ProjectOrchestrationStorageTests`

Result: **8 executed, 8 passed, 0 failed, 0 skipped**.

## Full Test Suite

`dotnet test AIUsageMonitor.sln --no-restore`

Result: **162 executed, 162 passed, 0 failed, 0 skipped**.

| Test project | Passed |
|---|---:|
| Domain | 28 |
| Provider | 46 |
| Infrastructure | 58 |
| Connection | 10 |
| Desktop | 20 |

## Release Build

`dotnet restore AIUsageMonitor.sln` succeeded.
`dotnet build AIUsageMonitor.sln --no-restore` succeeded with **0 warnings, 0 errors**.

## Publish

### win-x64

`dotnet publish src\AIUsageMonitor.Desktop\AIUsageMonitor.Desktop.csproj -c Release
-p:PublishProfile=win-x64 --no-restore` succeeded. The self-contained single-file executable was
produced under the `win-x64\publish` directory.

### win-x86

Not rerun for APO-27. Previous accepted evidence is retained because no project, package, or
publish configuration changed.

### win-arm64

Not rerun for APO-27. Previous accepted evidence is retained because no project, package, or
publish configuration changed.

## Runtime Smoke

The published `win-x64` executable launched successfully on the Windows x64 development machine,
stayed running through the smoke interval, and was stopped cleanly. No authenticated provider
interaction was attempted and no new screenshot was required because APO-27 has no visible UI
changes.

## git diff --check

Passed. Git reported only normal LF/CRLF normalization notices while staging Windows files; no
whitespace errors were present.

## Secret Scan

Passed targeted added-line scan for high-confidence provider/API secret patterns, bearer tokens,
private-key markers, and assigned password/token/secret/API-key literals. No real credentials were
found. The test suite uses only non-secret sanitized values.

## Working Tree

The implementation commit and the documentation handoff are limited to the APO-27 branch scope.
Final verification must leave `git status --short` empty after the handoff metadata commit. `main`
was not modified after the corrected fast-forward.

## Documentation

`.ai/CURRENT_STATE.md` records APO-34 as COMPLETE / MERGED / DONE at
`4b393b3e3cf732dd1f0e861a734e3c311327e2af`, the corrected reconciliation, APO-27 architecture,
validation evidence, limitations, and the post-acceptance Opus 5 architecture-review requirement.

## TASK.md

This file is the complete APO-27 Sol acceptance handoff. It does not contain an Opus prompt.

## PR Description

Draft PR #5 is exactly:

```text
feat/APO-27-orchestration-storage -> main
Title: APO-27: extend project and orchestration storage foundation
State: OPEN / DRAFT / UNMERGED
```

## Deferred Scope

Projects UI, routing engine, task classifier, executor runtime, autonomous loops, GitHub execution,
Jira/Azure execution, validation engine, review engine, activity UI, provider changes, cloud
backend, database/ORM, LocalAppData migration, APO-33 CI, merge, Jira writes, Opus invocation, and
downstream Stories are deferred.

## Known Limitations

- x86 and ARM64 publish evidence is retained from the accepted baseline and is compile/publish
  evidence from the x64 development machine; no corresponding hardware runtime is claimed.
- The storage foundation persists metadata/references only; future callers must keep free-form
  summaries and references non-secret.
- No live authenticated provider calls, tracker calls, GitHub execution, or orchestration runtime
  behavior was implemented or tested.
- The self-contained x64 launch smoke proves startup on this Windows x64 machine only; clean-machine
  release qualification, signing, installer/updater, and full product release qualification remain
  outside APO-27.

## Next Planner Boundary

GPT-5.6 Sol must inspect and accept the exact final pushed feature-branch state. After Sol
acceptance, one independent Claude Opus 5 architecture review is required. The Draft PR remains
open and unmerged. No Jira writes, Opus invocation, merge, or downstream Story work were performed.

APO-27 implementation is complete on the feature branch.

The Draft PR remains open and unmerged.

No Jira writes, Opus review, merge, or downstream Story work were performed.

The next step is GPT-5.6 Sol acceptance of the exact final pushed branch SHA.
