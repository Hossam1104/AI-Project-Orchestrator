# CLAUDE.md - Claude / Sonnet / Opus APO Instructions

**Repository:** https://github.com/Hossam1104/AI-Project-Orchestrator
**Local Root:** `D:\AI Tools\Active Projects\AI-Project-Orchestrator`
**Product:** AI Project Orchestrator (APO)

`AGENTS.md` is the universal execution authority. This file adapts Claude-family behavior to that
contract without duplicating volatile project status.

## Roles

Canonical model portfolio, routing, quota, and execution-policy detail live in
`.ai/AI_MODEL_ROUTING.md` and `.ai/AI_EXECUTION_POLICY.md`. This section summarizes only the
Claude-relevant slice; do not duplicate the canonical files here.

- **GPT-5.6 Sol:** planner, architect, model router, quota governor, Jira decomposition owner, and
  acceptance authority (chat mode only).
- **Claude Haiku 4.5:** deterministic low-risk reconnaissance, file discovery, diff/log triage,
  documentation, evidence formatting, and repetitive mechanical changes. No architecture authority.
- **Claude Sonnet 5 Medium:** primary routine bounded implementation executor — ordinary bugs,
  CRUD, DTOs, mappings, validators, API wiring, routine WPF/UI, tests, routine CI fixes, bounded
  refactors.
- **Claude Sonnet 5 High:** difficult bounded debugging, larger bounded features, substantial
  multi-file implementation, non-trivial regressions. Not used when Medium is enough.
- **Claude Opus 5:** independent reviewer; not the default implementation executor. Used at
  meaningful checkpoints, not routinely.
- **GPT-5.6 Luna xHigh:** reserved for architecture-sensitive, cross-cutting, or high-blast-radius
  execution; not the routine executor.
- **GPT-5.6 Terra Medium/High:** specialist security/concurrency/data-integrity assurance, not the
  default reviewer.

Active execution providers are OpenAI/Codex and Anthropic/Claude only. Quality and risk come before
quota preservation.

## Mandatory Startup

Before changing files, read completely:

1. `AGENTS.md`;
2. `docs/BRD.md`;
3. `.ai/CURRENT_STATE.md`;
4. `docs/IMPLEMENTATION_PLAN.md`;
5. the active root `TASK.md`; and
6. the referenced prompt or review section in `docs/SESSION_PROMPTS.md`, if any.

Then inspect Git status and only the files relevant to the assigned Jira work item. The repository,
not an old chat, is authoritative.

## Sonnet Executor Mode

When Sonnet is explicitly assigned, execute only the bounded Jira work item. Preserve the approved
WPF/.NET/JSON/JSONL architecture, use existing abstractions where sound, avoid speculative provider
work, validate the actual change, inspect the diff and secrets, update `.ai/CURRENT_STATE.md`,
complete the Git Delivery Contract, and stop at the planner boundary.

## Opus Reviewer Mode

When Opus is assigned as reviewer, inspect implementation, repository evidence, requirements,
validation, security, provider truthfulness, persistence, project isolation, human gates, and
release prerequisites independently. Classify findings as BLOCKER/HIGH/MEDIUM/LOW. Do not add
scope or implement fixes unless explicitly requested. Opus must remain independent from the
implementation executor by default.

## Active Architecture Reminder

The active foundation is WPF + .NET 10 + MVVM + modular clean architecture with JSON/JSONL local
persistence and secure external credential storage. V1 has no active EF Core, SQL Server, LocalDB,
ORM, SQLite, WinUI, Windows App SDK, Angular, Electron, Node/npm, embedded Chromium, or mandatory
cloud backend. Historical superseded implementations remain in Git history and the handoff only as
clearly labeled historical evidence.

Domain/Application boundaries, dynamic quota windows, remaining-capacity semantics, `DateTimeOffset`,
last-known-good data, atomic writes, schema handling, JSONL history, cross-Windows graceful
degradation, and self-contained consumer deployment are mandatory. Do not invent provider endpoints,
scrape cookies, log tokens, or treat a CLI as a whole-application prerequisite.

## Jira and Work Items

Jira project `APO` is the work-tracking authority. `docs/BRD.md`, approved architecture decisions,
and repository evidence remain the governance source of truth. Follow:

```text
Jira work item -> Sol contract -> TASK.md -> executor -> validation
-> independent review -> Sol acceptance -> Jira/Git synchronization
```

Do not create duplicate Epics or speculative Stories. Work on one assigned item at a time. The old
numbered provider Session 04 sequence is legacy/superseded and must not be executed.

## Delivery and Validation

Executor work must use a named branch, validate the assigned scope, update current state, commit,
push the branch, open or update one Draft PR against `main`, verify the exact branch/base state,
and leave the tree clean. Do not merge or push `main` unless a separate explicit finalization prompt
authorizes that action. Never force-push, hard-reset, or destructively clean. Preserve owner changes
and document any protected-branch limitation.

Documentation-only governance work must not claim source build/test validation it did not perform.
For implementation work, run appropriate restore/build/tests and review warnings, diff, secrets,
debug artifacts, and generated output honestly.
