# AI Usage Monitor

AI Usage Monitor is a local-first Windows desktop application for monitoring AI subscription capacity, quota resets, credits, subscription periods, and historical usage across supported AI services.

The product is intended for individual AI users: developers, writers, researchers, designers, marketers, students, business users, and anyone else who wants one clear view of available capacity. Its core question is:

> Which AI subscription or service has enough available capacity for what I want to do right now?

The recommendation is deterministic and capacity-based. It is not a ranking of model intelligence or output quality.

## Architecture status

The approved V1 target architecture is:

- C# / .NET 10
- WPF + MVVM
- Modular Clean Architecture
- `System.Text.Json`
- JSON for settings, provider state, subscriptions, and other small local documents
- monthly-partitioned JSONL for usage history and event streams
- Windows Credential Manager or another approved Windows secure-storage mechanism for actual credentials
- self-contained Windows release artifacts
- no database and no ORM

Session 03R has migrated the active source to the approved WPF and JSON/JSONL architecture. The former WinUI/Windows App SDK and EF Core/LocalDB implementation remains only in Git history. The WPF shell is intentionally an empty connected-provider state until the planner authorizes Session 04; provider integrations and dashboard scope were not added by Session 03R.

See [`.ai/CURRENT_STATE.md`](.ai/CURRENT_STATE.md) for the factual handoff, validation evidence, and the distinction between completed migration work and future product sessions.

## Consumer release goal

The V1 consumer goal is a zero-prerequisite desktop experience: users should be able to download the application, run it, connect or configure providers, and view capacity without installing a runtime, database engine, or developer toolchain.

The normal release experience is:

```text
download -> run -> connect/configure providers -> view capacity
```

A released application must not require a separately installed .NET runtime, .NET SDK, Visual Studio, SQL Server/LocalDB, SQLite, Node.js, npm, Angular, or developer command-line tools. Provider CLIs are optional capabilities, never a prerequisite for the whole application. Self-contained publishing is part of the active release plan; single-file publishing will be evaluated for WPF without blindly enabling trimming.

Release consideration covers `win-x64`, `win-x86`, and `win-arm64`, with compatibility claims limited to environments actually validated.

## Build and release note

Development machines still need the .NET SDK to build and test the repository. The consumer release contract is different: published artifacts are self-contained and must not require a separately installed .NET runtime, database engine, or developer toolchain.

To inspect or validate the current solution:

```powershell
dotnet restore AIUsageMonitor.sln
dotnet build AIUsageMonitor.sln
dotnet test AIUsageMonitor.sln
```

Session 03R validates the WPF build, JSON/JSONL persistence, and self-contained publish configuration. Provider collection, dashboard behavior, and release testing on every supported Windows edition remain later-session work.

## Repository layout

```text
AI Usage Monitor Tool/
├── AGENTS.md                   AI execution contract
├── CLAUDE.md                   Claude/Sonnet/Opus adapter instructions
├── AIUsageMonitor.sln
├── Directory.Build.props
├── .editorconfig
├── .gitignore
│
├── docs/
│   ├── BRD v1.0.md              Business requirements
│   ├── IMPLEMENTATION_PLAN.md   Session-by-session delivery plan
│   └── SESSION_PROMPTS.md       Exact executor/reviewer prompts
│
├── .ai/
│   └── CURRENT_STATE.md         Single mutable project handoff/status file
│
├── src/
│   ├── AIUsageMonitor.Desktop         Desktop presentation project; WPF is the approved target
│   ├── AIUsageMonitor.Domain          Technology-independent domain model
│   ├── AIUsageMonitor.Application     Provider-independent contracts/use cases
│   ├── AIUsageMonitor.Infrastructure  JSON/JSONL, secure storage, logging, and OS integrations
│   └── AIUsageMonitor.Providers       Codex/Claude/Kimi/Copilot/Antigravity adapters
│
└── tests/
    ├── AIUsageMonitor.Domain.Tests
    ├── AIUsageMonitor.Provider.Tests
    └── AIUsageMonitor.Infrastructure.Tests  JSON/JSONL persistence and resilience tests
```

## Product scope

The initial provider sequence remains Codex, Claude, Kimi, GitHub Copilot, and Antigravity. Provider integrations must use verified, truthful collection paths and must support partial or manual data when official programmatic usage is unavailable. The application must never scrape browser cookies, store provider secrets in local files, or collect conversations/source code.

The full requirements and execution rules are in [`docs/BRD v1.0.md`](docs/BRD%20v1.0.md), [`AGENTS.md`](AGENTS.md), and [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md).
