namespace AIUsageMonitor.Application.Agents;

public sealed class DefaultAgentCatalog : IDefaultAgentCatalog
{
    private static readonly IReadOnlyList<CatalogEntry> Entries =
    [
        new(
            Guid.Parse("b4c0b0d1-7f2c-4d4d-9f4d-000000000038"),
            "GPT-5.6 Sol",
            "OpenAI",
            "gpt-5.6-sol",
            "planner",
            [AgentRole.Planner, AgentRole.Architect, AgentRole.AcceptanceAuthority],
            [
                new(AgentRole.Planner, "planning/architecture/acceptance authority", "Default"),
                new(AgentRole.Architect, "planning/architecture/acceptance authority", "Default"),
                new(AgentRole.AcceptanceAuthority, "planning/architecture/acceptance authority", "Default")
            ]),
        new(
            Guid.Parse("b4c0b0d1-7f2c-4d4d-9f4d-000000000039"),
            "GPT-5.6 Luna xHigh",
            "OpenAI",
            "gpt-5.6-luna-xhigh",
            "executor",
            [AgentRole.Executor],
            [new(AgentRole.Executor, "primary implementation/remediation executor", "Primary")]),
        new(
            Guid.Parse("b4c0b0d1-7f2c-4d4d-9f4d-000000000040"),
            "Claude Sonnet 5",
            "Anthropic",
            "claude-sonnet-5",
            "executor",
            [AgentRole.Executor],
            [new(AgentRole.Executor, "exceptional alternative executor", "Exceptional")]),
        new(
            Guid.Parse("b4c0b0d1-7f2c-4d4d-9f4d-000000000041"),
            "Claude Opus 5",
            "Anthropic",
            "claude-opus-5",
            "reviewer",
            [AgentRole.Reviewer],
            [new(AgentRole.Reviewer, "periodic/critical independent reviewer", "Periodic/Critical")]),
        new(
            Guid.Parse("b4c0b0d1-7f2c-4d4d-9f4d-000000000042"),
            "GPT-5.6 Terra HIGH",
            "OpenAI",
            "gpt-5.6-terra-high",
            "security specialist",
            [AgentRole.SecuritySpecialist],
            [new(AgentRole.SecuritySpecialist, "risk-triggered optional specialist", "RiskTriggered")]),
        new(
            Guid.Parse("b4c0b0d1-7f2c-4d4d-9f4d-000000000043"),
            "Gemini 3.7",
            "Google",
            "gemini-3.7",
            "auxiliary executor",
            [AgentRole.AuxiliaryExecutor],
            [new(AgentRole.AuxiliaryExecutor, "suitable bounded/mechanical/quota-balancing work", "Auxiliary")])
    ];

    public IReadOnlyList<AgentDefinition> GetDefaults() =>
        Entries
            .Select(entry => entry.ToDefinition())
            .ToArray();

    private sealed class CatalogEntry
    {
        public CatalogEntry(
            Guid id,
            string displayName,
            string provider,
            string modelIdentifier,
            string legacyRole,
            IReadOnlyList<AgentRole> roles,
            IReadOnlyList<AgentRolePolicyMetadata> rolePolicyMetadata)
        {
            Id = id;
            DisplayName = displayName;
            Provider = provider;
            ModelIdentifier = modelIdentifier;
            LegacyRole = legacyRole;
            Roles = roles;
            RolePolicyMetadata = rolePolicyMetadata;
        }

        private Guid Id { get; }

        private string DisplayName { get; }

        private string Provider { get; }

        private string ModelIdentifier { get; }

        private string LegacyRole { get; }

        private IReadOnlyList<AgentRole> Roles { get; }

        private IReadOnlyList<AgentRolePolicyMetadata> RolePolicyMetadata { get; }

        public AgentDefinition ToDefinition() => new(
            Id,
            DisplayName,
            LegacyRole,
            AgentConnectionMode.Unsupported,
            AgentAvailability.Unknown,
            enabled: true,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch,
            provider: Provider,
            capabilities: [],
            limitations: ["Connection mode and current access are unverified; no provider probe has run."],
            costAndQuotaMetadata: null,
            roleCapabilities: Roles,
            supportedConnectionModes: [AgentConnectionMode.Unsupported],
            authenticationState: AgentAuthenticationState.Unknown,
            entitlementState: AgentEntitlementState.Unknown,
            modelIdentifier: ModelIdentifier,
            rolePolicyMetadata: RolePolicyMetadata);
    }
}
