# TASK - APO-19 - Inventory and classify legacy implementation for APO reuse

**Status:** PREPARED EXECUTION CONTRACT - NOT EXECUTED BY APO-20
**Product:** AI Project Orchestrator (APO)
**Epic:** APO-1 - APO Product Rebrand & Governance Rebaseline
**Story:** APO-19 - Inventory and classify legacy implementation for APO reuse
**Planner / Acceptance Authority:** GPT-5.6 Sol
**Assigned Executor:** Gemini 3.7 (Auxiliary cost/quota-balancing executor)
**Dependency:** APO-20 repository work complete; physical local-root rename remains a prerequisite

## Repository and starting checkpoint

**Repository:** `https://github.com/Hossam1104/AI-Project-Orchestrator`
**Local root:** `D:\AI Tools\Hossam\AI Project Orchestrator`
**Default branch:** `main`
**Expected starting SHA:** `861dc99`
**Authoritative requirements:** `docs/BRD.md`
**Execution authority:** `AGENTS.md`
**Live handoff:** `.ai/CURRENT_STATE.md`

The GitHub repository identity was renamed by APO-20. The target local root is the path above, but
the physical folder move must complete before this Story executes. Preserve the technical
identifiers that still contain `AIUsageMonitor`; this Story is an inventory and classification
exercise, not a technical codebase rename.

## Objective

Inspect the existing implementation and Git history, map reusable foundation work to the approved
APO BRD and Epic structure, and produce durable evidence that lets Sol sequence the next
planner-controlled implementation Stories.

## In scope

- Read `AGENTS.md`, `docs/BRD.md`, `.ai/CURRENT_STATE.md`, `docs/IMPLEMENTATION_PLAN.md`, this
  `TASK.md`, and the relevant historical sections of `docs/SESSION_PROMPTS.md`.
- Inspect the active `AIUsageMonitor.sln`, all source and test projects, project references,
  Domain models, Application contracts, Infrastructure persistence/resilience, WPF composition,
  provider foundation, publish profiles, configuration, tests, and relevant Git history.
- Map each meaningful existing capability to the BRD requirements and approved APO Epics.
- Classify each meaningful area exactly once as **Reuse As-Is**, **Reuse With Extension**,
  **Refactor**, **Superseded**, or **Remove**, with evidence and rationale.
- Record technical boundaries, architecture compatibility, security/privacy implications,
  provider-truthfulness implications, cross-Windows/release implications, gaps, dependencies,
  and recommended Story sequencing.
- Update or create the durable inventory artifact at `docs/LEGACY_IMPLEMENTATION_MAP.md`.
- Preserve historical evidence accurately, including the pre-APO-20 repository/path baseline where
  it is relevant and explicitly identified as historical.

## Out of scope and prohibitions

- Do not modify product source code, tests, solution/project files, namespaces, assembly names, or
  persistence schemas.
- Do not rename `AIUsageMonitor.sln`, any `AIUsageMonitor.*` project/test directory, technical
  identifier, or `%LOCALAPPDATA%\AIUsageMonitor`.
- Do not implement providers, orchestration runtime, model routing, GitHub/Jira adapters, WPF
  redesign, new product functionality, or future APO Stories.
- Do not revive EF Core, SQL Server, LocalDB, SQLite, WinUI, Windows App SDK, Node/npm, Electron,
  Tauri, Chromium, or an APO-owned cloud backend.
- Do not create speculative Jira work items or execute any Story other than APO-19.
- Do not use live authenticated provider calls or credentials. Do not add secrets or generated
  build artifacts to source control.

## BRD requirements and acceptance criteria

The completed inventory must:

1. Treat `docs/BRD.md` as the requirements authority and preserve the active WPF/.NET/JSON/JSONL
   architecture boundary.
2. Cover all active solution projects and tests plus the relevant superseded history.
3. Provide an evidence-backed classification and requirement/Epic mapping for every meaningful
   capability area.
4. Identify reusable foundations, extensions, refactor areas, superseded work, removals, risks,
   dependencies, and gaps without claiming unimplemented APO capabilities as complete.
5. Explicitly verify the no-database/ORM active runtime, secure credential boundary, dynamic
   used/remaining capacity semantics, last-known-good behavior, project-isolation implications,
   and self-contained Windows release considerations.
6. Deliver a readable durable mapping artifact and update `.ai/CURRENT_STATE.md` with factual
   evidence, validation, limitations, and the next Sol planner boundary.
7. Make no product source/test/project changes.

## Required validation and evidence

Before and after the inventory:

- Inspect `git status`, current branch, remotes, `HEAD`, and the expected starting SHA.
- Use a bounded branch such as `docs/APO-19-legacy-implementation-map` and preserve unrelated
  owner changes.
- Run the validation appropriate to the inspected baseline:
  `dotnet restore AIUsageMonitor.sln`, `dotnet build AIUsageMonitor.sln`,
  `dotnet test AIUsageMonitor.sln`, and `git diff --check`.
- Record actual restore/build/test results, warning/error counts, test totals, and any baseline
  limitation; do not claim results that were not run.
- Scan the diff and repository status for source changes, secrets, credentials, and generated
  artifacts.
- Confirm the active source/configuration scope is unchanged except for the inventory artifact,
  current-state evidence, and any explicitly approved documentation updates.

## Delivery and stop condition

Follow the Git Delivery Contract in `AGENTS.md`: update `.ai/CURRENT_STATE.md`, commit and push the
assigned branch, integrate under repository policy, verify `origin/main`, and leave the working
tree clean. Stop after APO-19 evidence is delivered and the next planner boundary is recorded.
Do not begin APO-2, APO-3, APO-4, APO-5, APO-6, APO-7, APO-8, APO-9, APO-10, APO-11, APO-12, APO-13,
APO-14, APO-15, APO-16, APO-17, or any unassigned task.

This contract is prepared by APO-20 and is not being executed in the current session. Do not start
it until the physical local-root rename is complete and a fresh instruction authorizes APO-19.
