# CLAUDE.md - Claude / Sonnet / Opus Repository Instructions

**Repository:** https://github.com/Hossam1104/AI-Usage-Monitor-Tool  
**Local Root:** `D:\AI Tools\Hossam\AI Usage Monitor Tool`

`AGENTS.md` is the universal authority for AI execution behavior. This file adapts Claude Code, Sonnet, and Opus to that contract without duplicating volatile project status.

---

# 1. Current Roles

- **Luna Max:** default implementation executor.
- **GPT-5.6 Sol:** planner and owner of approved architecture/sequence decisions.
- **Opus 5:** independent reviewer.
- **Sonnet/Terra:** fallback executors only when explicitly assigned.

The active architecture is WPF + .NET 10 + MVVM with JSON/JSONL local persistence and self-contained Windows release artifacts. The former WinUI/Windows App SDK/EF Core/SQL Server LocalDB implementation is historical and superseded after Session 03R.

---

# 2. Mandatory Startup

Before any work:

1. Read `AGENTS.md`.
2. Read `docs/BRD v1.0.md`.
3. Read `.ai/CURRENT_STATE.md`.
4. Read `docs/IMPLEMENTATION_PLAN.md`.
5. Read the active root `TASK.md`.
6. Read the exact assigned prompt/review gate from `docs/SESSION_PROMPTS.md` when it is referenced.
7. Inspect Git status, then inspect only task-relevant source/config files.

Do not rely on old Claude chat context. Repository state wins.

The root `TASK.md` is the current executor assignment and completion handoff. Follow it for the
named session only, keep factual project status in `.ai/CURRENT_STATE.md`, and stop before the
next session.

---

# 3. Sonnet/Terra Executor Mode

When Sonnet or Terra is explicitly assigned as an executor:

- execute only the assigned session
- preserve the approved WPF + JSON/JSONL architecture
- implement code, not just plan it
- use existing abstractions where sound
- validate actual changes
- review the own diff
- update `.ai/CURRENT_STATE.md`
- complete the Git Delivery Contract
- stop before the next session

Do not rewrite large areas merely for stylistic preference, add provider scope early, or introduce EF/LocalDB/SQLite/Angular/Electron without an explicit planner decision.

---

# 4. Opus 5 Reviewer Mode

When Opus is used as reviewer:

- inspect the real implementation and evidence
- challenge claims, especially provider evidence and release prerequisites
- classify findings as BLOCKER, HIGH, MEDIUM, or LOW
- issue the exact requested gate verdict
- do not add new product scope
- do not fix code unless explicitly instructed after the review

Prioritize:

1. provider correctness and truthfulness
2. zero-prerequisite self-contained deployment
3. JSON/JSONL history and file-integrity behavior
4. credential/security boundaries
5. used/remaining semantics
6. reset/timezone correctness
7. provider isolation
8. WPF responsiveness and graceful degradation
9. stale/error behavior
10. packaging/release safety

---

# 5. Cross-Windows Compatibility Reminder

`AGENTS.md` Section 3A is the primary source and applies in full:

- minimum baseline: Windows 10 1809 / build 17763
- supported architecture goal: x86, x64, ARM64
- x64 is the primary validation target
- any newer OS API/effect requires capability detection and a compatible fallback
- optional modern WPF effects must never gate functionality
- no hard hardware-acceleration prerequisite
- no dependency may silently raise the minimum OS

When the exact .NET 10/WPF OS support needs confirmation, verify current official Microsoft documentation before making a release claim.

---

# 6. Provider Discipline

For Codex, Claude, Kimi, Copilot, and Antigravity:

- use Session 04 evidence
- re-verify if current behavior differs
- prefer official APIs, account usage surfaces, OAuth/device auth, and documented endpoints
- treat an official CLI as optional, never as a whole-application prerequisite
- never invent endpoint URLs, file schemas, or response fields
- never infer billing data as fact
- never extract browser cookies or passwords
- never expose raw tokens
- never commit unsanitized account payloads

A provider field that cannot be obtained safely must remain unavailable or use an approved manual fallback.

---

# 7. Architecture and Editing Discipline

The active architecture is:

- WPF desktop presentation
- MVVM
- provider-independent Domain/Application layers
- Infrastructure-owned JSON/JSONL stores and secure-storage adapters
- provider-owned detection, collection, parsing, and normalization
- no database engine or ORM in V1

Before replacing an abstraction:

- determine why it violates the BRD or current architecture
- preserve compatible behavior
- record meaningful architectural decisions in `CURRENT_STATE`

Do not casually rename projects or namespaces. Preserve unrelated working behavior and user changes.

---

# 8. Context Efficiency

After mandatory governance files, read only what is relevant. Do not recursively consume `.git`, `bin`, `obj`, package caches, generated binaries, unrelated logs, or unrelated source folders. Use `.ai/CURRENT_STATE.md` as the handoff.

---

# 9. Validation

Executor sessions must, where applicable:

- restore when dependencies changed
- build the solution
- run focused tests
- run required manual WPF/provider/publish validation
- review the Git diff
- inspect accidental secrets and debug artifacts

Documentation-only governance work must not claim build validation for an architecture that is not yet implemented. Never state that tests pass unless they actually ran. Record blocked validation exactly.

---

# 10. Current Product Principle

This is a local-first consumer capacity monitor, not a SaaS platform. Keep the implementation:

- accurate
- secure
- usable without developer tooling or a mandatory provider CLI
- resilient when provider data is partial
- safe when local files are missing or corrupt
- maintainable
- responsive
- visually polished for developers and non-developers
- honest about unavailable data
