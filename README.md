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

The repository is currently between the historical persistence session and the approved architecture migration. Session 03 successfully implemented the originally approved WinUI/Windows App SDK + EF Core + SQL Server LocalDB design. That implementation is historical and architecturally superseded; the source still contains it until **Session 03R - Portable Consumer Desktop Architecture Migration** executes.

Do not read the approved WPF/JSON target as a claim that the migration has already been implemented. See [`.ai/CURRENT_STATE.md`](.ai/CURRENT_STATE.md) for the factual distinction between target and current source.

## Consumer release goal

The V1 consumer goal is a zero-prerequisite desktop experience: users should be able to download the application, run it, connect or configure providers, and view capacity without installing a runtime, database engine, or developer toolchain.

The normal release experience is:

```text
download -> run -> connect/configure providers -> view capacity
```

A released application must not require a separately installed .NET runtime, .NET SDK, Visual Studio, SQL Server/LocalDB, SQLite, Node.js, npm, Angular, or developer command-line tools. Provider CLIs are optional capabilities, never a prerequisite for the whole application. Self-contained publishing is part of the active release plan; single-file publishing will be evaluated for WPF without blindly enabling trimming.

Release consideration covers `win-x64`, `win-x86`, and `win-arm64`, with compatibility claims limited to environments actually validated.

## Historical/current build note

Until Session 03R, the current source is the historical WinUI/EF/LocalDB implementation. Development machines may need the .NET SDK and the current source toolchain. This is not the final consumer installation experience, and the repository is not yet a release-ready WPF application.

For the current historical source, contributors can inspect or validate the existing solution with:

```powershell
dotnet restore AIUsageMonitor.sln
dotnet build AIUsageMonitor.sln
dotnet test AIUsageMonitor.sln
```

The documentation-only architecture rebaseline does not claim that a WPF build, JSON persistence, or self-contained publish has been validated. Those belong to Session 03R and later sessions.

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
    └── AIUsageMonitor.Infrastructure.Tests  Historical persistence tests; to migrate to file tests in Session 03R
```

## Product scope

The initial provider sequence remains Codex, Claude, Kimi, GitHub Copilot, and Antigravity. Provider integrations must use verified, truthful collection paths and must support partial or manual data when official programmatic usage is unavailable. The application must never scrape browser cookies, store provider secrets in local files, or collect conversations/source code.

The full requirements and execution rules are in [`docs/BRD v1.0.md`](docs/BRD%20v1.0.md), [`AGENTS.md`](AGENTS.md), and [`docs/IMPLEMENTATION_PLAN.md`](docs/IMPLEMENTATION_PLAN.md).
