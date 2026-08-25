# APO-38 Prompt 2/5 — Active Implementation Contract

## Story

APO-38 — Establish Provider-Independent Agent and Model Registry Truth

Parent Epic: APO-8 — AI Agent / Model Registry & Connectivity
Executor: GPT-5.6 Luna xHigh
Planner / acceptance authority: GPT-5.6 Sol

## Authorized base and branch

- Starting `main` SHA: `ffb449e5396fa56a6ccb3f39134807166cc5ea40`
- Branch: `feat/APO-38-agent-model-registry-truth`
- Keep the branch open as one Draft PR; do not merge.
- APO-38 remains In Progress until Sol acceptance.

## Objective

Evolve the existing `AIUsageMonitor.Application.Agents` foundation and `agents.json` persistence
to represent trustworthy provider-independent agent/model identity, multi-role capability,
multi-mode access, availability, authentication, entitlement, limitations, project-isolated
overrides, connection-test truth, and centralized owner-approved role metadata.

The implementation is configuration and evidence truth only. It must not route, score, select,
invoke, retry, execute, create worktrees, send prompts, or infer entitlement from subscriptions,
quota, or provider-capacity connections.

## Required implementation boundary

- Preserve the existing GUID identity and legacy `Role` and `ConnectionMode` meaning.
- Add structured provider/model identity metadata without storing credentials.
- Add the approved roles: Planner, Architect, Acceptance Authority, Executor, Reviewer, Security
  Specialist, and Auxiliary Executor.
- Keep invocation modes separate from availability: InteractiveOnly, CLI, API, SDK, Manual, and
  Unsupported.
- Keep authentication separate from entitlement, with Unknown/Not Required/Authenticated/
  Authentication Required and Unknown/Verified Available/Verified Unavailable/Not Applicable.
- Add bounded provider-independent connection-result evidence with timestamp, evaluated mode,
  availability, authentication, entitlement, evidence source, limitation/error code, safe message,
  and optionally reported supported modes.
- Provide one centralized data-driven catalog for Sol, Luna xHigh, Sonnet 5, Opus 5, Terra HIGH,
  and Gemini 3.7. Defaults may describe identity, roles, and owner-approved usage metadata only;
  they must remain Unknown/unverified for live access truth.
- Persist small project overrides below the existing GUID-derived project boundary. Overrides may
  change only enabled state, permitted roles/modes, and bounded policy/metadata.
- Provide an application resolver for global definition + optional project override. Missing agents
  must return explicit not-found semantics. This resolver is not routing.
- Register new repositories/services in the existing DI composition root without requiring auth,
  providers, or a new UI.
- Reuse versioned JSON, atomic writes, cancellation, existing invalid-file handling, and no SQL,
  ORM, database, cloud backend, or parallel registry.

## Compatibility and security

Old `agents.json` records lacking APO-38 fields must load, preserve legacy identity/role/mode/
availability, receive Unknown/unverified defaults, and round-trip without semantic loss. Do not
persist passwords, tokens, OAuth secrets, cookies, headers, sessions, prompts, conversations,
provider payloads, source code, repository content, or raw credentials. Metadata must remain bounded
and secret-key validated.

## Required validation

Run focused contract/catalog/connection tests, old-record compatibility tests, project A/B isolation
tests, and effective-registry tests. Then run:

```powershell
dotnet restore AIUsageMonitor.sln
dotnet build AIUsageMonitor.sln --no-restore
dotnet test AIUsageMonitor.sln --no-restore
git diff --check
```

Also perform changed-line secret scanning, base-to-head diff/file review, DI/startup composition
validation, self-contained `win-x64` publish and runtime smoke verification. Report actual test
totals by project, warnings/errors, CI evidence only if observed, and all limitations truthfully.

## Explicit exclusions

Do not implement APO-39 onboarding/context resolution; APO-44 routing or quota selection; APO-45
process execution/checkpoints/retries; APO-46 worktree automation; APO-47 tracker integration;
APO-62 remote SCM evidence; provider adapters/live probes/scraping/CLI orchestration; prompt/chat
transport; cloud sync/backend; or configuration-heavy UI.

## Delivery and planner boundary

Update `.ai/CURRENT_STATE.md` with factual implementation and validation evidence. Add one final
Jira comment, keep APO-38 In Progress, create/push one Draft PR, and do not merge. Replace this file
at completion with a complete GPT-5.6 Sol acceptance handoff containing the final SHA, PR, changed
files, architecture decisions, test totals, validation, Jira/CI/runtime truth, limitations, and
next boundary. Leave the current published APO process alive after runtime verification.

Required next boundary:

> Prompt 2/5 APO-38 implementation complete. Draft PR remains OPEN / DRAFT / UNMERGED. Next action is GPT-5.6 Sol exact-head acceptance review. APO-39 and APO-44 remain NOT AUTHORIZED.
