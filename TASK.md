# APO-27 OPUS-01 SOL DELTA ACCEPTANCE HANDOFF

**Story:** APO-27 - Extend Storage Layout and Stores for APO Projects and Orchestration Records
**Status:** READY FOR GPT-5.6 SOL DELTA ACCEPTANCE
**Owner:** Hossam
**Planner / architect / acceptance authority:** GPT-5.6 Sol
**Executor:** GPT-5.6 Luna xHigh - bounded persistence-correctness remediation executor
**Branch:** `feat/APO-27-orchestration-storage`
**Draft PR:** #5 - OPEN / DRAFT / UNMERGED
**Jira:** APO-27 - In Progress
**Parent Epic:** APO-3 - In Progress

This is the next gate for the completed OPUS-01 remediation. It authorizes Sol to inspect and
accept the exact functional remediation evidence below. It does not authorize a merge, a PR-ready
transition, a main-branch change, a Jira status transition, another Story, or downstream
orchestration work. A bounded real Claude Opus 5 re-review is the following gate after Sol delta
acceptance; its prompt is not prepared in this file.

## Exact repository and review identity

| Item | Exact value |
|---|---|
| Exact main base | `4b393b3e3cf732dd1f0e861a734e3c311327e2af` |
| Original real Opus reviewed target | `0ea0c65ec7dac7ec09d30a6b25156353b714298f` |
| Pre-remediation branch head | `66bd4505861bbdc831a4909d6c0da9082c433a55` |
| Real Opus verdict | `CHANGES REQUIRED` |
| Accepted blocker | `OPUS-01 - P2` |
| Sol adjudication comment | `11790` |
| Current-review state comment | `11791` |
| Functional remediation SHA | `9350ae53edd4ed75d0158d8da78e4a8cc81ad291` |
| Documentation/handoff SHA | Recorded by final Git verification after this handoff commit |
| Final branch SHA | Recorded by final Git verification and delivery report |

The functional remediation SHA is the reviewable code/test target. Any later documentation-only
commit must not change that functional target.

## OPUS-01 root cause and decision

`VersionedJsonCollectionStore<TRecord>.ReadAsync` intentionally retains the existing read-only
behavior of collapsing every non-usable `FileReadResult` to an empty collection. Both
`UpdateAsync` overloads previously reused that read method. Consequently, an `IoFailure` or
`PermissionFailure` during an update could be interpreted as a new empty registry, invoke the
caller delegate, and atomically replace a healthy authoritative registry with only the incoming
record.

Sol accepted OPUS-01 as a blocking data-loss defect. The remediation is deliberately limited to
the shared Infrastructure collection-store update path and deterministic Infrastructure tests.
OPUS-02 through OPUS-07 remain deferred as documented below.

## VersionedJsonCollectionStore fix

Both overloads now share the private `ReadForUpdateAsync` helper:

```text
UpdateAsync(string path, Func<List<TRecord>, List<TRecord>> update, ...)
UpdateAsync<TResult>(
    string path,
    Func<List<TRecord>, (List<TRecord> Items, TResult Result)> update,
    ...)
```

The helper preserves the accepted recovery semantics for `Missing`, `Empty`, `Corrupt`, and
`UnsupportedSchema`, and uses valid persisted items for `Valid`. It fails closed for transient
read failures before the update delegate is called or `WriteCoreAsync` can run.

### Failure semantics

- `IoFailure` throws a bounded safe `IOException`.
- `PermissionFailure` throws a bounded safe `UnauthorizedAccessException`.
- The raw file path, raw exception text, registry contents, credentials, and authenticated data
  are not included in the surfaced message.
- Cancellation continues to propagate normally.
- Transient read failures do not quarantine, move, delete, truncate, or replace the authoritative
  file.
- Corrupt and unsupported-schema reads retain the existing quarantine/recovery behavior; a later
  write may recreate the authoritative destination.

The only deterministic fault seam is an Infrastructure-internal read-failure injector on
`JsonFileStore`. It is unset in production and is not exposed to Application or Domain.

## Regression evidence

### Project registry

- Persisted two valid projects before each injected-failure attempt.
- `IoFailure` caused `JsonProjectRepository.UpsertAsync` to fail with `IOException`.
- `PermissionFailure` caused `JsonProjectRepository.UpsertAsync` to fail with
  `UnauthorizedAccessException`.
- Before/after `projects.json` byte arrays are asserted identical for both failures.
- The incoming project is absent and both original project IDs remain readable after the injected
  fault is removed.

### Agent registry

- Persisted two valid agents before an injected `IoFailure`.
- `JsonAgentRepository.UpsertAsync` failed with `IOException`.
- Before/after `agents.json` byte arrays are asserted identical.
- The incoming agent is absent and both original agent IDs remain readable after recovery.

### Shared result-bearing update overload

- `VersionedJsonCollectionStore.UpdateAsync<TResult>` was exercised directly.
- An injected `IoFailure` throws before the update delegate runs.
- The delegate-side-effect flag remains false.
- The authoritative collection file bytes remain identical and the original item remains readable.

### Existing recovery and normal updates

- Existing corrupt-document and unsupported-schema quarantine tests remain green, including future
  write recovery.
- Existing normal project upsert coverage remains green: same IDs are replaced, distinct IDs are
  added, and concurrent distinct upserts preserve all siblings.

## Validation evidence

### Focused persistence/storage tests

Command:

```powershell
dotnet test tests\AIUsageMonitor.Infrastructure.Tests\AIUsageMonitor.Infrastructure.Tests.csproj `
  --no-restore --filter FullyQualifiedName~ProjectOrchestrationStorageTests
```

| Executed | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| 36 | 36 | 0 | 0 |

### Full solution tests

Command:

```powershell
dotnet test AIUsageMonitor.sln --no-restore
```

| Executed | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| 190 | 190 | 0 | 0 |

Breakdown: Domain 28, Provider 46, Infrastructure 86, Connection 10, Desktop 20.

### Restore and build

- `dotnet restore AIUsageMonitor.sln` - SUCCESS; all projects up to date.
- `dotnet build AIUsageMonitor.sln --no-restore` - SUCCESS; 0 warnings, 0 errors.

### Publish and runtime smoke

- `win-x64` self-contained single-file publish - SUCCESS using the unchanged
  `win-x64.pubxml` profile.
- x64 startup smoke - SUCCESS; published executable remained alive for five seconds and was
  stopped cleanly.
- `win-x86` and `win-arm64` publish evidence is retained from the accepted baseline because no
  project, package, or publish configuration changed. No x86/ARM64 hardware runtime is claimed.
- No screenshot was required; no visible UI change was expected.

### Diff, secret, and artifact checks

- `git diff --check` - SUCCESS; no whitespace errors, with only normal Windows LF/CRLF notices.
- Targeted added-line secret scan - SUCCESS; no real secrets, tokens, cookies, prompts,
  conversations, source code, authenticated payloads, or generated artifacts were added.
- Build and publish outputs remain ignored and are not part of the commits.

## Markdown, BRD, and governance synchronization

All tracked Markdown files were enumerated and searched for APO-27 lifecycle references. The
current factual state is synchronized in `.ai/CURRENT_STATE.md`, `TASK.md`, `README.md`, and the
current-planning boundary of `docs/IMPLEMENTATION_PLAN.md`.

`AGENTS.md`, `CLAUDE.md`, and `docs/BRD.md` remain unchanged. BRD requirement changes: NONE.
The permanent execution contract, product requirements, historical/provider evidence, and
stable mapping documents were reviewed and did not require rewriting.

Required current lifecycle:

```text
APO-27 implementation
  -> Sol findings
  -> R-01..R-07 remediation
  -> Sol delta acceptance
  -> owner-approved storage truthfulness remediation
  -> real Claude Opus 5 review
  -> Opus verdict CHANGES REQUIRED
  -> Sol accepted OPUS-01 as blocking
  -> this OPUS-01 remediation
  -> Sol delta acceptance pending
  -> bounded Opus re-review pending
  -> Sol final acceptance pending
```

## Jira and Draft PR #5 evidence

- APO-27 remains `In Progress`.
- APO-3 remains `In Progress`.
- No Jira status transition was made.
- One concise APO-27 completion comment records the functional remediation SHA, final branch SHA,
  fail-closed semantics, byte-preservation evidence, project/agent/result-bearing tests, full
  validation, publish/smoke evidence, Markdown synchronization, and the next gate.
- PR #5 remains OPEN, DRAFT, UNMERGED, and based on `main`.
- The PR body records the real Opus verdict, OPUS-01 non-deferral, remediation design, exact
  functional SHA, final branch SHA, no-truncation tests, validation, P3 deferrals, and next gate.

If the configured Jira or GitHub connector cannot complete a write, the final delivery report
must state the specific synchronization item as pending rather than claiming it was performed.

## Deferred Opus findings

- **OPUS-02:** Summary/reference length hardening - DEFER.
- **OPUS-03:** Generic metadata-value secret detection - DEFER / CALLER BOUNDARY.
- **OPUS-04:** FilePathLocks lifetime - DEFER.
- **OPUS-05:** RecordId idempotency - DEFER TO EXECUTION RUNTIME.
- **OPUS-06:** Dead JSONL `lineNumber` - DEFER.
- **OPUS-07:** Repository metadata / `DefaultBranch` semantic refinement - DEFER.

## Exact next gate

GPT-5.6 Sol delta acceptance of functional remediation
`9350ae53edd4ed75d0158d8da78e4a8cc81ad291` is the next gate. Sol must inspect the actual diff,
tests, persistence semantics, architecture boundary, and evidence. After Sol delta acceptance,
the bounded real Claude Opus 5 re-review is required; after that, Sol remains the final acceptance
authority. No Opus re-review, final Sol acceptance, merge, main-branch change, or downstream Story
work was performed in this remediation.
