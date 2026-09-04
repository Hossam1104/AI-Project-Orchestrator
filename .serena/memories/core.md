# AI_Orchestrator source map

- Ten-project solution: 5 production (Domain -> Application; Infrastructure -> Application/Domain; Providers -> Application/Domain; Desktop/WPF composes all) + 5 test (Domain.Tests, Provider.Tests, Infrastructure.Tests, Desktop.Tests, Connection.Tests).
- Active product architecture is C#/.NET 10 WPF/MVVM, local-first JSON/JSONL persistence, secure Windows credential storage, provider-independent application contracts.
- Current product identity is AI_Orchestrator (previously AI Project Orchestrator, AI Usage Monitor), but technical assembly/namespaces remain AIUsageMonitor intentionally; do not rename without planner authorization.
- Governance source hierarchy: docs/BRD.md, AGENTS.md, .ai/CURRENT_STATE.md, Jira APO, docs/IMPLEMENTATION_PLAN.md, TASK.md. Current operational AI policy is in .ai/AI_MODEL_ROUTING.md and .ai/AI_EXECUTION_POLICY.md.
- V1 excludes active EF/SQL/LocalDB/SQLite, WinUI, Node/npm, embedded browser, and APO cloud backend. Read `mem:tech_stack` for pins and `mem:conventions` for boundaries.