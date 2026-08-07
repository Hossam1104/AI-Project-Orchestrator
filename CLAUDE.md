# CLAUDE.md — Claude / Sonnet / Opus Repository Instructions

**Repository:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**Local Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`

`AGENTS.md` is the universal authority for AI execution behavior.

This file adapts Claude Code, Sonnet, and Opus to that contract without duplicating live project status.

---

# 1. Mandatory Startup

Before any work:

1. Read `AGENTS.md`.
2. Read `docs/BRD v1.0.md`.
3. Read `.ai/CURRENT_STATE.md`.
4. Read `docs/IMPLEMENTATION_PLAN.md`.
5. Read the exact assigned prompt/review gate from `docs/SESSION_PROMPTS.md`.
6. Inspect Git status.
7. Inspect only task-relevant source/config files.

Do not rely on old Claude chat context.

Do not assume previous work is complete because a prior conversation said so.

Repository state wins.

---

# 2. Sonnet Executor Mode

When Sonnet is used as executor:

- execute only the assigned session
- implement code, not just plan it
- preserve approved architecture
- use existing abstractions where sound
- validate actual changes
- review own diff
- update `.ai/CURRENT_STATE.md`
- stop

Do not automatically execute the next session.

Do not rewrite large areas merely for stylistic preference.

---

# 3. Opus 5 Reviewer Mode

When Opus is used as reviewer:

- inspect the real implementation
- challenge claims
- verify provider evidence
- classify findings
- issue the exact requested gate verdict

Do not add new product scope.

Do not fix code in reviewer mode unless explicitly instructed after the review.

Prioritize:

1. provider correctness
2. security
3. used/remaining semantics
4. reset/timezone correctness
5. provider isolation
6. database/history integrity
7. UI responsiveness
8. stale/error behavior
9. packaging/release safety

---

# 3A. Cross-Windows Compatibility (Claude-Specific)

`AGENTS.md` §3A "Cross-Windows Compatibility Contract" is the primary source and applies in full. This is a reminder, not a duplicate:

- Sonnet executor mode must implement every change so it obeys the contract's minimum OS (Windows 10 1809 / build 17763) and architecture (x86/x64/ARM64) baseline, guarding any Windows 11-only API/feature behind capability detection with a working fallback.
- Opus reviewer mode must treat a violation of the minimum OS/architecture contract — including a hard dependency that silently raises it — as a classified review finding, not a stylistic note.
- Optional modern UI features (Mica, Acrylic, newer backdrops/effects) require a graceful, functionally-equivalent fallback; visual effects must never gate functionality.
- Claude models must not silently raise the minimum supported OS by adopting a dependency that requires it — escalate to the planner instead, per `AGENTS.md` §3A.

---

# 3B. Git Delivery (Claude-Specific)

`AGENTS.md` §6A "Git Delivery Contract" is the primary source and applies in full. This is a reminder, not a duplicate:

- Sonnet executor mode: implementation completion includes commit/push/merge/verify. Never stop after local validation with uncommitted changes. Never ask whether completed, validated session work should be committed or pushed — the Git Delivery Contract already answers that permanently. Obey `AGENTS.md` §6A end to end (branch → commit → push → merge → push main → verify `origin/main` → clean working tree) before reporting a session `COMPLETE`.
- Opus reviewer mode: review work itself does not need code changes or commits unless explicitly requested. If a remediation executor is used to fix findings, that remediation must obey the Git Delivery Contract in `AGENTS.md` §6A the same as any other implementation session.

---

# 4. Provider Discipline

For Codex, Claude, Kimi, Copilot, and Antigravity:

- use Session 04 evidence
- re-verify if current behavior differs
- never invent endpoint URLs
- never invent response fields
- never infer billing data as fact
- never extract browser cookies
- never expose raw tokens
- never commit unsanitized account payloads

A provider field that cannot be obtained safely must remain unavailable or use an approved manual fallback.

---

# 5. Context Efficiency

After mandatory governance files, read only what is relevant.

Do not recursively consume:

- `.git`
- `bin`
- `obj`
- package caches
- generated binaries
- unrelated logs
- unrelated source folders

Do not create long redundant progress documents.

Use `.ai/CURRENT_STATE.md` as the handoff.

---

# 6. Editing Discipline

Preserve existing working behavior outside assigned scope.

Prefer coherent, localized changes.

Before replacing an existing abstraction:

- determine why it violates BRD/architecture
- preserve compatible behavior
- record meaningful architectural decisions in CURRENT_STATE

Do not casually rename projects or namespaces.

Do not replace the approved stack.

---

# 7. Validation

Executor sessions must, where applicable:

- restore when dependencies changed
- build the solution
- run targeted tests
- run required manual WinUI/provider validation
- review Git diff
- inspect accidental secrets

Never state “tests pass” unless they actually ran.

If a prerequisite prevents validation, record the exact limitation instead of pretending success.

---

# 8. CURRENT_STATE Contract

Before an executor stops, update `.ai/CURRENT_STATE.md` with:

- current phase/session
- completion status
- implemented work
- actual build/test result
- provider capability changes
- material decisions
- blockers
- next session
- reviewer verdict when applicable

Do not maintain volatile status in `CLAUDE.md`.

---

# 9. Final Principle

This is a local personal capacity monitor, not a SaaS platform.

Keep the implementation:

- accurate
- secure
- local-first
- maintainable
- responsive
- visually polished
- honest about unavailable provider data
