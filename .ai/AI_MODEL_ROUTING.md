# AI_MODEL_ROUTING.md — Canonical AI Execution Routing Policy

This file is the canonical, cross-project source of truth for AI execution routing, model
portfolio, and provider quota governance. `AGENTS.md` and `CLAUDE.md` reference this file rather
than duplicating it. It governs **AI development/execution tooling**, not APO's product-domain
provider-monitoring functionality (see AGENTS.md §6).

---

## 1. Control Plane

**GPT-5.6 Sol** is the control plane: planner, architect, model router, quota governor, and
acceptance authority. Sol operates in **chat mode only** and must not become the routine Codex
repository executor.

---

## 2. Active Execution Providers

Only two providers are active for AI-assisted execution in this repository and this development
environment:

- **OpenAI / Codex**
- **Anthropic / Claude**

No other provider (Gemini, Z.ai, GLM, OpenCode, Kimi, or any other) is an active execution
provider unless the repository owner explicitly changes this policy. This does not remove or
weaken APO's own product-domain support for monitoring other AI providers — that is separate
product functionality, not orchestration-executor policy.

---

## 3. Model Portfolio

| Model | Provider pool | Default role |
|---|---|---|
| GPT-5.6 Sol High | OpenAI/Codex | Planner / Architect / Model Router / Quota Governor / Acceptance Authority (chat only) |
| Claude Haiku 4.5 High | Anthropic/Claude | Deterministic low-risk reconnaissance and mechanical work |
| Claude Sonnet 5 Medium | Anthropic/Claude | Primary routine bounded implementation executor |
| Claude Sonnet 5 High | Anthropic/Claude | Difficult bounded debugging/implementation; not used when Medium suffices |
| GPT-5.6 Luna xHigh | OpenAI/Codex | Architecture-sensitive, cross-cutting, high-blast-radius execution |
| GPT-5.6 Luna Max | OpenAI/Codex | Exceptional implementation escalation only |
| Claude Opus 5 Medium/High | Anthropic/Claude | Independent reviewer (not routine executor) |
| GPT-5.6 Terra Medium/High | OpenAI/Codex | Specialist security/concurrency/data-integrity assurance |

### Role detail

- **Haiku** — repository reconnaissance, file discovery, diff/log triage, documentation, evidence
  formatting, repetitive mechanical changes, structured data preparation. No architecture
  authority.
- **Sonnet Medium** — ordinary bugs, CRUD, DTOs, mappings, validators, API wiring, routine
  frontend/backend and WPF work, ordinary automation, tests, routine CI fixes, bounded refactors.
- **Sonnet High** — difficult bounded debugging, larger bounded features, substantial multi-file
  implementation, integration behavior, non-trivial regressions. Not used when Medium is enough.
- **Luna xHigh** — architecture-sensitive execution, cross-cutting behavior, complex
  persistence/state, concurrency-sensitive implementation, difficult integrations, difficult
  multi-module debugging, high regression blast radius. Not routine.
- **Luna Max** — exceptional escalation only; never the default executor.
- **Opus** — independent review only, roughly every fifth substantial implementation prompt where
  review adds meaningful value, or at genuinely critical checkpoints. Not for routine coding. Opus
  High is exceptional. Remains independent from the implementation executor by default.
- **Terra** — specialist assurance for security, trust boundaries, concurrency, authorization,
  data integrity, credential boundaries, destructive operations. Not a general executor.

One assigned Jira work item remains the maximum active scope for one executor.

---

## 4. Provider Quota Pools

Two shared executor quota pools:

- **OpenAI / Codex pool** — Luna xHigh, Luna Max, Terra Medium, Terra High, other Codex agentic
  execution.
- **Anthropic / Claude pool** — Haiku, Sonnet, Opus.

Luna → Terra is **not** provider diversification (same pool). Sonnet → Opus is **not** provider
diversification (same pool). Provider-level routing, not model-level routing, is what matters for
quota balancing.

---

## 5. Quota States

- `GREEN` — > ~60% remaining
- `AMBER` — ~35–60% remaining
- `RED` — ~15–35% remaining
- `CRITICAL` — < ~15% remaining
- `UNKNOWN` — exact quota unavailable

Exact percentages are used only when obtained from a provider UI/CLI, explicit user input, or
another trustworthy local source. Never invent a percentage.

### Shared cross-project quota location

`%USERPROFILE%\.ai-orchestration\` (outside any Git repository) holds:

- `quota-state.json` — current qualitative/quantitative state per provider pool.
- `execution-ledger.jsonl` — append-only non-secret operational history (timestamp, project, work
  item, provider, model, effort, tier, result, qualitative quota before/after, fallback/escalation,
  duration where known).

Never store passwords, tokens, cookies, API keys, credentials, prompts, source code, or transcripts
in either file. Never overwrite valid existing operational history.

---

## 6. Task Risk Tiers and Default Routing

| Tier | Description | Default executor |
|---|---|---|
| 1 | Documentation, deterministic evidence formatting, mechanical/reconnaissance work | Haiku |
| 2 | Ordinary bounded implementation (CRUD, DTOs, mappings, validators, routine WPF/UI, tests) | Sonnet Medium |
| 3 | Difficult bounded implementation/debugging, larger bounded features, non-trivial regressions | Sonnet High |
| 4 | Architecture-sensitive, cross-cutting, concurrency/persistence-critical, high blast radius | Luna xHigh (Luna Max exceptional) |

Independent review (Opus) and specialist assurance (Terra) are applied on top of the tier, not in
place of it, per the criteria in §3.

### Provider balancing rule

When two candidate executors both satisfy the required tier and role, prefer the provider pool
that is currently in a better quota state (§5), unless quality/risk considerations require a
specific model. Quality and risk come before quota preservation — never downgrade tier solely to
preserve quota.

---

## 7. APO-Specific Risk Appendix

The following APO areas are elevated risk (normally Tier 3 or Tier 4 depending on blast radius) and
require commensurate executor selection and, where appropriate, Opus/Terra review:

- routing authority and selected-agent execution truth;
- persisted execution plans and immutable execution authority;
- replay protection and recovery checkpoints;
- workspace isolation;
- process execution and process termination;
- residual/non-cooperative execution;
- credential boundaries;
- remote execution;
- provider connection-mode drift;
- schema/integrity evidence;
- destructive Git/workspace operations;
- autonomous background execution.

Lower-risk normal-volume APO work (Tier 1–2) includes documentation, deterministic evidence
formatting, straightforward WPF presentation, isolated DTO/view-model mapping, mechanical tests,
simple UI polish, and bounded cleanup.

Project risk overrides generic routing: a nominally small change in a high-risk area above is
routed at Tier 3/4, not by size alone.

---

## 8. Required Sol Route Declaration

After the prompt gate (see `.ai/AI_EXECUTION_POLICY.md`) opens, Sol's generated executor/reviewer
prompt must declare: the assigned Jira work item, the selected model/role/effort, the provider
pool, the current quota state used to inform (not override) the choice, and the risk tier
justification.

---

## 9. Final Acceptance

Sol remains the final acceptance authority for all routed work, regardless of which executor
performed it. Executor self-declaration is not final acceptance (see
`.ai/AI_EXECUTION_POLICY.md`).
