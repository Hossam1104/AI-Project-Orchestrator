# AI Project Orchestrator (APO)

AI Project Orchestrator is a local-first Windows control center for AI-assisted software delivery.
It brings together:

- AI capacity, subscription, quota, credit, and reset monitoring;
- project and workspace awareness;
- Git and work-item awareness;
- AI agent/model registry and supported connectivity;
- quality- and risk-first model routing;
- planner-authored execution contracts;
- bounded autonomous execution;
- validation and evidence capture;
- independent review and remediation;
- human approval gates; and
- activity history, auditability, notifications, and command-center UX.

APO is intended to reduce repetitive handoffs while keeping the owner in control of high-risk
actions. Capacity informs routing, but quality and risk take priority. Provider and model claims
must be evidence-based; unavailable data remains explicitly unavailable or manual.

## Product and repository continuity

The product identity is now AI Project Orchestrator. The GitHub repository and local project folder
remain unchanged for now:

`Hossam1104/AI-Usage-Monitor-Tool`
`D:\AI Tools\Hossam\AI Usage Monitor Tool`

The active requirements baseline is [`docs/BRD.md`](docs/BRD.md). Jira project `APO` is the
authoritative work-tracking system. The old versioned BRDs and provider-only session plan are
historical inputs and are no longer active authorities.

## Current reusable foundation

The repository already contains a validated, reusable technical foundation:

- C# / .NET 10;
- WPF desktop shell with MVVM-oriented composition;
- provider-independent Domain and Application contracts;
- JSON and JSONL local persistence under per-user LocalAppData;
- atomic/synchronized file handling and corruption/startup resilience;
- dynamic quota and remaining-capacity domain safeguards;
- focused xUnit tests;
- Windows x86/x64/ARM64 target configuration; and
- self-contained publish profiles for the portable Windows foundation.

The current source still uses `AIUsageMonitor` technical identifiers and a minimal shell. APO
source-code mapping, product-surface refactoring, and implementation of orchestration capabilities
have not started under APO-18. Existing code is therefore a reusable foundation, not a claim that
all APO capabilities are implemented.

Historical EF Core/SQL Server LocalDB and WinUI/Windows App SDK work remains recorded in Git and
`.ai/CURRENT_STATE.md` as completed historical work, but is architecturally superseded and is not
part of the active runtime.

## Consumer deployment goal

The V1 consumer goal is a self-contained Windows desktop experience:

```text
download -> run -> connect/configure supported services -> supervise work
```

The released application must not require a separately installed .NET runtime, SDK, Visual Studio,
database engine, Node.js/npm, embedded browser, or provider CLI. Mutable data belongs in per-user
application-data locations and actual credentials remain outside JSON/JSONL behind secure storage.
Release consideration covers `win-x64`, `win-x86`, and `win-arm64`, with claims limited to
environments actually validated.

## Governance and planning

The execution flow is:

```text
Jira Work Item -> Sol execution contract -> TASK.md -> assigned executor
-> validation -> independent review where required -> Sol acceptance -> Jira/Git synchronization
```

APO-1 through APO-17 are the approved Epic capability structure. APO-18 is the current governance
rebaseline boundary. GPT-5.6 Sol must perform repository-to-Jira legacy mapping and prepare the
next approved Story before source refactoring or new capability implementation begins.

See [`AGENTS.md`](AGENTS.md), [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md),
[`docs/SESSION_PROMPTS.md`](docs/SESSION_PROMPTS.md), and [`.ai/CURRENT_STATE.md`](.ai/CURRENT_STATE.md)
for execution rules, dependencies, history, and evidence.

## Repository layout

```text
AI Usage Monitor Tool/
â”œâ”€â”€ AGENTS.md
â”œâ”€â”€ CLAUDE.md
â”œâ”€â”€ AIUsageMonitor.sln
â”œâ”€â”€ Directory.Build.props
â”œâ”€â”€ docs/
â”‚   â”œâ”€â”€ BRD.md
â”‚   â”œâ”€â”€ IMPLEMENTATION_PLAN.md
â”‚   â””â”€â”€ SESSION_PROMPTS.md
â”œâ”€â”€ .ai/
â”‚   â””â”€â”€ CURRENT_STATE.md
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ AIUsageMonitor.Desktop
â”‚   â”œâ”€â”€ AIUsageMonitor.Domain
â”‚   â”œâ”€â”€ AIUsageMonitor.Application
â”‚   â”œâ”€â”€ AIUsageMonitor.Infrastructure
â”‚   â””â”€â”€ AIUsageMonitor.Providers
â””â”€â”€ tests/
    â”œâ”€â”€ AIUsageMonitor.Domain.Tests
    â”œâ”€â”€ AIUsageMonitor.Provider.Tests
    â””â”€â”€ AIUsageMonitor.Infrastructure.Tests
```

## Development validation

Development machines need the .NET SDK to build and test the repository. That is different from
the consumer release contract.

```powershell
dotnet restore AIUsageMonitor.sln
dotnet build AIUsageMonitor.sln
dotnet test AIUsageMonitor.sln
```

The baseline implementation is validated as a foundation. Provider integrations, orchestration
runtime, routing, dashboard expansion, project/tracker adapters, independent review automation,
and final release qualification remain planner-controlled APO work.
