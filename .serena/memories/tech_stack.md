# AI Project Orchestrator technology

- C# with TargetFramework net10.0 for Domain, Application, Infrastructure, Providers; Desktop targets net10.0-windows10.0.17763.0 with WPF and minimum Windows 10 build 17763.
- Desktop RIDs: win-x86, win-x64, win-arm64; x64 is primary validation target.
- Core packages: Microsoft.Extensions.DependencyInjection/Hosting/Http, Serilog 4.3.0, Serilog.Extensions.Hosting 9.0.0, Serilog.Sinks.File 7.0.0, Serilog.Sinks.Debug 3.0.0.
- Persistence uses System.Text.Json through JSON documents and monthly JSONL; schema versions are explicit and currently V1.
- Tests use xUnit project infrastructure; no database/ORM runtime dependency.