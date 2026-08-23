<p align="center">
  <img src="assets/Logo.png" alt="AI Project Orchestrator logo" width="360" />
</p>

<h1 align="center">AI Project Orchestrator</h1>

<p align="center">
  Intelligent orchestration for projects, AI agents, execution, and quality.
</p>

<p align="center">
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&amp;logoColor=white" alt=".NET 10" /></a>
  <a href="https://learn.microsoft.com/dotnet/desktop/wpf/"><img src="https://img.shields.io/badge/UI-WPF-0078D4?logo=windows&amp;logoColor=white" alt="WPF" /></a>
  <a href="https://www.microsoft.com/windows/"><img src="https://img.shields.io/badge/platform-Windows-0078D4?logo=windows&amp;logoColor=white" alt="Windows" /></a>
  <a href="https://learn.microsoft.com/dotnet/csharp/"><img src="https://img.shields.io/badge/language-C%23-239120?logo=csharp&amp;logoColor=white" alt="C#" /></a>
</p>

<p align="center">
  <img src="assets/readme/apo-flow.svg" alt="Animated target orchestration flow from owner to acceptance" width="900" />
</p>

> APO is a local-first Windows control center for supervising AI-assisted software delivery. It is being built to make project state, model choice, execution evidence, independent review, and human approval visible in one place.

## What is AI Project Orchestrator?

AI Project Orchestrator (APO) coordinates the work around software delivery: projects and
repositories, AI capacity, model and agent access, planner-authored execution contracts, bounded
execution, validation, independent review, acceptance, and audit history.

The product is not an AI chat application and does not replace an IDE, GitHub, Jira, or Azure
DevOps. Its job is to reduce fragile copy/paste handoffs while keeping the owner in charge of
high-risk actions. Quality and risk come before quota savings, and unavailable provider data stays
explicitly unavailable or manual.

## Core capabilities

The product direction brings these capabilities into one supervised workflow:

- :brain: Intelligent, quality-first model and agent routing with quota awareness.
- :bar_chart: AI capacity, subscription, quota, credit, and reset monitoring.
- :file_folder: Project awareness with isolated local workspaces and repository state.
- :robot: AI agent and model registry with truthful connection capabilities.
- :compass: Planner-authored work classification and execution contracts.
- :gear: Bounded, cancellable execution with explicit budgets and stop conditions.
- :test_tube: Validation evidence captured independently from executor claims.
- :mag: Independent review and bounded remediation at configured checkpoints.
- :shield: Human approval gates for high-risk decisions and delivery actions.
- :page_facing_up: Local activity, audit, history, notifications, and reconstructable state.

These are the approved product capabilities, not a claim that every adapter or runtime is already
implemented. See [Current implementation status](#current-implementation-status).

## Orchestration flow

The following is the target product flow. Several stages are still planned and must not be read as
an assertion that the autonomous runtime exists today.

```mermaid
flowchart TD
    Owner([Owner intent]) --> Work[Work item]
    Work --> Planner[Sol planner / execution contract]
    Planner --> Router[Quality-first routing]
    Router --> Executor[Assigned executor]
    Executor --> Validation[Independent validation]
    Validation --> Review[Independent review]
    Review -->|findings| Remediation[Bounded remediation]
    Remediation --> Validation
    Review --> Acceptance[Sol acceptance]
    Acceptance --> Gate{Human gate when required}
    Gate --> Delivery[Git / tracker delivery]
```

## Current implementation status

APO is an active foundation, not a finished orchestration product.

| Status | Current evidence |
| --- | --- |
| :white_check_mark: Implemented / validated | .NET 10 WPF foundation and resilient empty/no-provider startup shell |
| :white_check_mark: Implemented / validated | JSON documents and monthly JSONL local persistence with safe-write and recovery behavior |
| :white_check_mark: Implemented / validated | Provider-independent quota, subscription, usage, alert, and refresh contracts |
| :white_check_mark: Implemented / validated | Windows Credential Manager adapter with opaque credential references and focused security tests |
| :white_check_mark: Implemented / validated | Self-contained multi-RID publish configuration for `win-x64`, `win-x86`, and `win-arm64` |
| :white_check_mark: Implemented / validated | Project and orchestration storage foundation (APO-27 merged); stores and contracts for projects, agents, routing policies, runs, and review/audit records |
| :white_check_mark: Implemented / validated | Focused xUnit foundation suite; the current APO-27 branch validation is 36 focused storage tests and 190 full-suite tests, all passing |
| :construction: In progress / planned | Official provider capacity adapters for Codex, Claude, Kimi, GitHub Copilot, and Antigravity |
| :construction: In progress / planned | Project Registry, Git/GitHub and Jira/Azure DevOps awareness |
| :construction: In progress / planned | Agent registry, routing, bounded execution, validation, review, and acceptance services |
| :compass: Roadmap | Full command center, activity/audit experience, notifications, CI, and release qualification |

Not yet implemented: provider adapters beyond their bounded foundation, the autonomous runtime,
the routing engine, tracker automation, the user-facing Projects UI, the review/acceptance engines,
and the full APO-15 dashboard. APO does not fabricate provider numbers or claim CI status before
the relevant Story is delivered.

## WPF shell preview

The first branded shell is intentionally a foundation surface: it shows the product identity,
local-first state, and truthful future boundaries without inventing provider data or dashboard
metrics.

<p align="center">
  <img src="assets/readme/apo-shell.png" alt="AI Project Orchestrator branded WPF foundation shell" width="1000" />
</p>

## Architecture

The active architecture is a portable C#/.NET 10 WPF application with clean dependency direction,
JSON/JSONL local persistence, and secure external credential storage. No database engine, ORM,
Node.js runtime, embedded browser, or APO-owned cloud backend is required by V1.

```mermaid
flowchart LR
    Desktop[Desktop / WPF shell] --> Application[Application contracts & use cases]
    Application --> Domain[Provider-independent Domain]
    Infrastructure["Infrastructure<br/>JSON / JSONL / secure store / logging"] --> Application
    Providers["Providers & integrations<br/>verified collection / normalization"] --> Application
```

The technical solution and namespaces still use the compatibility-sensitive `AIUsageMonitor` name.
That is deliberate: the product identity has changed, but a large technical rename and LocalAppData
migration require their own planner-approved work.

## AI operating model

APO follows a quality- and risk-first operating policy. Capacity can inform routing, but it never
overrides capability, risk, or the required review gate. These are project roles and target policy;
automated model routing is not implemented yet.

| Model | Default role |
| --- | --- |
| **GPT-5.6 Sol** | Planner, architect, and acceptance authority |
| **GPT-5.6 Luna xHigh** | Substantial implementation and cross-cutting fixes |
| **Claude Sonnet 5** | Bounded implementation and focused bug fixing |
| **Claude Opus 5** | Periodic independent reviewer at critical checkpoints |
| **GPT-5.6 Terra HIGH** | Optional specialist security review when risk warrants it |
| **Gemini 3.7** | Bounded, repetitive, validation, documentation, or quota-balancing work |

GPT-5.6 Luna xHigh is the default substantial executor. Because Luna xHigh has a higher
hallucination risk, execution prompts must be detailed, precise, bounded, and verification-oriented.
Claude Opus 5 is used approximately every 5 meaningful implementation prompts and at genuinely
critical checkpoints only, not after every Story. GPT-5.6 Sol remains the Planner, Architect, and
Acceptance Authority. Codex/GPT and Claude remain the primary stack, while Gemini 3.7 remains
auxiliary for bounded work and quota balancing.

## Security and privacy

- :lock: Windows Credential Manager stores actual credentials outside JSON, JSONL, logs, and source control.
- :key: Application state stores only opaque credential references; raw tokens and passwords are not persisted.
- :no_entry_sign: APO does not extract browser cookies, scrape passwords, or treat a consumer subscription as API entitlement.
- :package: Local-first project state and history remain under per-user application data.
- :scroll: Evidence and audit history are designed to explain routing, validation, review, and approval decisions without storing secrets.
- :shield: High-risk operations stop at explicit human approval gates.

Project isolation, configured exclusions, secret scanning/redaction, least privilege, and visible
destinations are part of the product contract for future integrations.

## Windows compatibility

APO targets Windows 10 version 1809 (build 17763) and Windows 11, with these release RIDs under
consideration:

- `win-x64` - primary development and validation target.
- `win-x86` - configured and compile/publish validated where stated in the evidence.
- `win-arm64` - configured and compile/publish validated where stated; ARM64 hardware execution is not claimed.

The consumer goal is a self-contained Windows application that does not require a separately
installed .NET runtime, SDK, Visual Studio, database engine, Node/npm, embedded browser, or provider
CLI. Claims are limited to the environments and commands actually run.

## Repository structure

```text
AI-Project-Orchestrator/
|-- assets/
|   |-- Logo.png
|   |-- Colors.png
|   |-- runtime/apo-icon.ico
|   `-- readme/apo-flow.svg
|-- docs/
|   |-- BRD.md
|   |-- IMPLEMENTATION_PLAN.md
|   |-- LEGACY_IMPLEMENTATION_MAP.md
|   `-- SESSION_PROMPTS.md
|-- src/
|   |-- AIUsageMonitor.Desktop/
|   |   `-- Resources/ (WPF brand dictionaries)
|   |-- AIUsageMonitor.Application/
|   |-- AIUsageMonitor.Domain/
|   |-- AIUsageMonitor.Infrastructure/
|   `-- AIUsageMonitor.Providers/
|-- tests/
|   |-- AIUsageMonitor.Domain.Tests/
|   |-- AIUsageMonitor.Provider.Tests/
|   `-- AIUsageMonitor.Infrastructure.Tests/
|-- .ai/CURRENT_STATE.md
|-- AGENTS.md
|-- TASK.md
|-- Directory.Build.props
`-- AIUsageMonitor.sln
```

## Build and test

The commands below use the compatibility-preserved solution name:

```powershell
dotnet restore AIUsageMonitor.sln
dotnet build AIUsageMonitor.sln
dotnet test AIUsageMonitor.sln
```

For a self-contained Windows artifact, use one of the desktop publish profiles:

```powershell
dotnet publish src/AIUsageMonitor.Desktop/AIUsageMonitor.Desktop.csproj `
  -p:PublishProfile=win-x64
```

The matching profiles for `win-x86` and `win-arm64` are in
`src/AIUsageMonitor.Desktop/Properties/PublishProfiles/`. Build, test, and publish output should
be treated as evidence only after the command completes successfully on the current checkout.

## Documentation

- [Business Requirements Document](docs/BRD.md)
- [Implementation Plan](docs/IMPLEMENTATION_PLAN.md)
- [Legacy Implementation Map](docs/LEGACY_IMPLEMENTATION_MAP.md)
- [Session Prompt Library](docs/SESSION_PROMPTS.md)
- [Execution Contract and Sol Checkpoint](AGENTS.md)
- [Current State and Validation Handoff](.ai/CURRENT_STATE.md)
- [Current Task Contract](TASK.md)

## Roadmap

The roadmap follows the approved APO Epics rather than invented completion percentages:

1. **APO-2 / APO-3** - harden the Windows foundation, local persistence, and security boundaries.
2. **APO-4 / APO-5** - add truthful capacity adapters and isolated project/workspace awareness.
3. **APO-6 / APO-7** - connect GitHub, Jira, and Azure DevOps where explicitly configured.
4. **APO-8 / APO-9** - establish agent/model connectivity and quality-first routing.
5. **APO-10 / APO-11** - introduce planner contracts and bounded, cancellable execution.
6. **APO-12 / APO-13 / APO-14** - capture evidence, independent review, remediation, and human gates.
7. **APO-15 / APO-16 / APO-17** - grow the command center, activity history, notifications, CI, and release quality.

Each Story is scoped by a Sol-approved `TASK.md` contract. A completed foundation does not
authorize the next Story automatically.

## Contributing and governance

APO work is evidence-led and branch-scoped. A contributor or executor should:

1. read `AGENTS.md`, the BRD, current state, implementation plan, and the active `TASK.md`;
2. work only within the assigned Jira Story and preserve unrelated owner changes;
3. validate the relevant build, tests, security, compatibility, and documentation surfaces;
4. update `.ai/CURRENT_STATE.md` with factual evidence and limitations; and
5. stop at the planner boundary so GPT-5.6 Sol can accept, reject, or issue the next contract.

The owner remains the authority for protected-branch merges, high-risk actions, and material
architecture or requirement changes. `TASK.md` is an executable contract, not a historical log.

## Product identity mapping

| Surface | APO identity | Compatibility decision |
| --- | --- | --- |
| User-facing product, shell, README, and metadata | **AI Project Orchestrator (APO)** | Updated in the branding Story |
| Approved visual assets | `assets/Logo.png`, `assets/Colors.png` | Original bytes preserved |
| Runtime icon | `assets/runtime/apo-icon.ico` | Derived compact symbol for Windows surfaces |
| Solution, project, namespace, and test identifiers | `AIUsageMonitor.*` | Preserved for controlled future migration |
| Local persistence root | `%LOCALAPPDATA%\\AIUsageMonitor` | Preserved; no silent data migration |

## License

No license file is currently committed. Until the owner adds a license, reuse and redistribution
rights should not be inferred from this README.

<p align="center">
  <img src="assets/Logo.png" alt="AI Project Orchestrator" width="120" />
</p>

<p align="center"><sub>AI Project Orchestrator - APO</sub></p>
