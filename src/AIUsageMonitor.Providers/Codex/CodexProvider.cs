using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Providers.Common;

namespace AIUsageMonitor.Providers.Codex;

/// <summary>
/// Codex consumer-plan usage is intentionally not inferred from OpenAI API organization usage.
/// The documented Codex subscription status surface is interactive, so this adapter reports the
/// official CLI when installed and leaves automated consumer capacity manual/unsupported.
/// </summary>
public sealed class CodexProvider : UnsupportedCliProviderBase
{
    public CodexProvider(IClock clock, IExecutableLocator executableLocator)
        : base(clock, executableLocator, "codex")
    {
    }

    public override AIUsageMonitor.Domain.Providers.ProviderCode Code =>
        AIUsageMonitor.Domain.Providers.ProviderCode.Codex;
}
