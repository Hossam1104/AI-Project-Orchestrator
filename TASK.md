# TASK — SESSION 04

Task: Session 04 — Provider Feasibility Investigation
Executor: Luna Max
Status: NOT STARTED
Scope: Evidence and feasibility only; do not implement provider adapters.

## Common active-session inheritance

This is the complete executable prompt for the current task. The repository, not prior chat
context, is authoritative.

- Execute only the named session.
- Read `AGENTS.md`, `docs/BRD v1.0.md`, `.ai/CURRENT_STATE.md`, `docs/IMPLEMENTATION_PLAN.md`,
  the Session 04 section of `docs/SESSION_PROMPTS.md`, and this `TASK.md` before acting. Inspect
  `git status` before editing and preserve unrelated changes.
- `docs/SESSION_PROMPTS.md` is the permanent approved session library; `TASK.md` is the current/next
  executable session; `.ai/CURRENT_STATE.md` is the factual execution history and live project state.
- The active architecture is WPF + .NET 10 + MVVM + Modular Clean Architecture.
- Active persistence is `System.Text.Json` with JSON for state/configuration and JSONL for append-oriented
  history/events.
- V1 has no EF Core, SQL Server, LocalDB, ORM, SQLite, Angular, Electron, Node.js, npm, or embedded Chromium.
- Released artifacts must be self-contained; no separately installed .NET runtime, SDK, database, Node, or
  developer tooling may be required.
- No provider CLI is mandatory for the whole application. Non-developer/browser-only users require truthful
  connection or manual fallback paths where supported.
- Preserve remaining-capacity semantics, dynamic quota windows, `DateTimeOffset`, provider truthfulness,
  privacy, and last-known-good behavior.
- Honor the Cross-Windows Compatibility Contract: Windows 10 build 17763 engineering baseline, Windows 10
  1809+/Windows 11 goal, x86/x64/ARM64, x64 primary, and graceful fallback for optional modern effects.
- Do not add providers or unrelated product scope. Do not implement provider production code in this session.
- Use focused evidence and sanitized fixtures; no live authenticated provider calls in CI.
- Review the diff, inspect secrets/debug artifacts, update `CURRENT_STATE`, deliver the assigned session,
  prepare the next executable `TASK.md` only after successful delivery, and stop before the next session/gate.

Completed implementation/remediation work must follow the Git Delivery Contract: use an appropriately named
session branch, commit, push the branch, merge into `main`, push `main`, fetch and verify
`origin/main`, and leave the working tree clean. The permitted final metadata-only synchronization may
touch only `.ai/CURRENT_STATE.md` and `TASK.md`.

## Session 04 — Provider Feasibility Investigation

~~~
EXECUTE SESSION 04 ONLY.
EVIDENCE/FEASIBILITY SESSION — DO NOT IMPLEMENT PROVIDER ADAPTERS.
~~~

Confirm before investigation that Session 03R remains complete, Session 03R-F is complete and merged into
`main`, `origin/main` is verified, and the planner has no unresolved blocker preventing feasibility work.
Gate A follows Session 04 and is not a prerequisite. Stop before Gate A.

Investigate Codex, Claude, Kimi, GitHub Copilot, and Google Antigravity using official documentation, safe
local inspection, and approved account workflows. For each provider determine:

1. installation/application detection
2. account and authentication-state detection
3. official usage API or account usage endpoint
4. OAuth/device/account connection options
5. browser-only workflow if officially supported
6. official CLI/status/usage command, if any
7. safe verified local metadata
8. actual quota windows/credits exposed
9. whether values mean used or remaining
10. absolute used/remaining/limit availability
11. reset timestamp and timezone
12. plan and subscription/renewal information
13. network and rate-limit requirements
14. auth expiry behavior
15. manual/partial/unsupported fallback

Answer explicitly:

- Can a non-developer connect it?
- Can a browser-only user use it?
- Is CLI required, or can it remain optional?
- What is official, verified local, inferred, manual, unavailable, or unsupported?

Use current, primary evidence. Prefer official provider APIs, OAuth/device/account flows, authenticated account
surfaces, and documented endpoints. A CLI is optional, never a whole-application prerequisite. Do not guess
endpoints or schemas, fabricate quotas/plans/reset times, or label inferred data as official. Do not extract
browser cookies/passwords, print tokens, commit raw authenticated payloads, or store secrets.

Record a capability/evidence matrix and safe implementation recommendation in `.ai/CURRENT_STATE.md`.
Do not create provider production code, provider adapters, dashboard work, Session 05 work, or Gate A findings.
Validate the existing solution only as appropriate for an evidence session. Update the factual state, deliver
the assigned evidence session through Git, prepare the next executable task after successful delivery, and stop.

## Stop condition

Session 04 is complete only when its provider evidence and capability matrix are recorded, no provider adapter
has been implemented, Git delivery is verified, and the next task is prepared without executing it.
~~~
