# AI Usage Monitor

A personal Windows desktop utility that shows how much AI coding capacity you have left — across Codex, Claude, Kimi, GitHub Copilot, and Antigravity — in one place, before you start or continue work.

This is not a SaaS product, team platform, or commercial tool. See `docs/BRD v1.0.md` for full product requirements.

## Stack

- C# / .NET 10
- WinUI 3 + Windows App SDK
- MVVM, Modular Clean Architecture
- EF Core 10 + Microsoft SQL Server LocalDB (added in a later session)
- Serilog
- xUnit

## Repository Layout

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
│   └── SESSION_PROMPTS.md       Exact executor prompts per session
│
├── .ai/
│   └── CURRENT_STATE.md         Single mutable project handoff/status file
│
├── src/
│   ├── AIUsageMonitor.Desktop         WinUI 3 application (composition root)
│   ├── AIUsageMonitor.Domain          Technology-independent domain model
│   ├── AIUsageMonitor.Application     Application contracts / use cases
│   ├── AIUsageMonitor.Infrastructure  EF Core, credentials, OS integrations
│   └── AIUsageMonitor.Providers       Codex/Claude/Kimi/Copilot/Antigravity adapters
│
└── tests/
    ├── AIUsageMonitor.Domain.Tests
    └── AIUsageMonitor.Provider.Tests
```

## Building

Requires the .NET 10 SDK.

```powershell
dotnet restore AIUsageMonitor.sln
dotnet build AIUsageMonitor.sln
dotnet test tests/AIUsageMonitor.Domain.Tests/AIUsageMonitor.Domain.Tests.csproj
dotnet test tests/AIUsageMonitor.Provider.Tests/AIUsageMonitor.Provider.Tests.csproj
```

The `AIUsageMonitor.Desktop` project is an unpackaged WinUI 3 app; running `dotnet build` restores the Windows App SDK build tools it needs — no separate Windows SDK/Visual Studio install is required to build.

## Project Status

The current phase, session status, and validation results are tracked in [`.ai/CURRENT_STATE.md`](.ai/CURRENT_STATE.md) — read that file before starting or continuing any implementation session.

## AI Execution Contract

Every AI model working in this repository must follow [`AGENTS.md`](AGENTS.md) and, for Claude/Sonnet/Opus, [`CLAUDE.md`](CLAUDE.md).
