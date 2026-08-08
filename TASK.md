# TASK - SESSION 04 - Provider Feasibility Investigation

This is the complete current executable copy of the approved Session 04 assignment. The permanent
source is `docs/SESSION_PROMPTS.md`; this file is prepared for direct execution in a fresh context.

# COMMON ACTIVE SESSION INHERITANCE

Every active prompt below inherits these rules from `AGENTS.md`:

- execute only the named session or gate
- read `AGENTS.md`, `docs/BRD v1.0.md`, `.ai/CURRENT_STATE.md`, `docs/IMPLEMENTATION_PLAN.md`, and this assigned prompt before acting
- inspect `git status` before editing and preserve unrelated user changes
- active target architecture is WPF + .NET 10 + MVVM + Modular Clean Architecture
- active persistence is `System.Text.Json` with JSON for state/configuration and JSONL for append-oriented history/events
- V1 has no EF Core, SQL Server, LocalDB, ORM, SQLite, Angular, Electron, Node.js, npm, or embedded Chromium
- released artifacts must be self-contained; no separately installed .NET runtime, SDK, database, Node, or developer tooling may be required
- no provider CLI is mandatory for the whole application
- non-developer/browser-only users must be represented by safe connection or manual fallback paths
- preserve remaining-capacity semantics, dynamic quota windows, `DateTimeOffset`, provider truthfulness, privacy, and last-known-good behavior
- honor the Cross-Windows Compatibility Contract: Windows 10 1809/build 17763 baseline, Windows 10 1809+/Windows 11 goal, x86/x64/ARM64, x64 primary, graceful fallback for optional modern effects
- do not add new providers or unrelated product scope
- use focused tests and sanitized fixtures; no live authenticated provider calls in CI
- review the diff, inspect secrets/debug artifacts, update `CURRENT_STATE`, and stop before the next session

Completed implementation sessions must also follow the Git Delivery Contract: commit, push the session branch, merge into `main`, push `main`, fetch/verify `origin/main`, and leave the tree clean. Reviewer-only gates do not modify or deliver code unless explicitly instructed.

The active root `TASK.md` is the current or next executable session assignment. Read it before
acting, execute only its named session, and stop after a successful delivery has prepared the next
task without executing it. If the session is partial or blocked, keep a remediation task.

The task-file relationship is:

~~~
docs/SESSION_PROMPTS.md
    = permanent approved session library

TASK.md
    = current/next executable session

.ai/CURRENT_STATE.md
    = factual execution history and live project state
~~~

---

# SESSION 04 - Provider Feasibility Investigation

```text
EXECUTE SESSION 04 ONLY.
EVIDENCE/FEASIBILITY SESSION - DO NOT IMPLEMENT PROVIDER ADAPTERS.
```

Inherit the active WPF/JSONL/zero-prerequisite/Cross-Windows/Git rules above. Confirm Session
03R remains complete, Session 03R-F is complete and merged into `main`, `origin/main` is verified,
and no unresolved planner blocker prevents feasibility work. Gate A follows Session 04 and is not a
prerequisite. Stop before Gate A.

Investigate Codex, Claude, Kimi, GitHub Copilot, and Google Antigravity using official documentation, safe local inspection, and approved account workflows. For each provider determine:

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

Do not extract browser cookies/passwords, print tokens, commit raw payloads, or assume an online document proves an account endpoint exists. Record a capability/evidence matrix and safe implementation recommendation in `CURRENT_STATE`. Do not create provider production code. Validate the existing solution only as appropriate, update state, deliver the assigned evidence session, and stop before Gate A/Session 05.

---
