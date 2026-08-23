using AIUsageMonitor.Application.Providers;
using AIUsageMonitor.Domain.Providers;
using AIUsageMonitor.Providers.Copilot;

namespace AIUsageMonitor.Desktop.ViewModels;

public sealed class ProviderConnectionEditorViewModel : ObservableObject
{
    private readonly IProviderConnectionService _service;
    private readonly ProviderCode _code;
    private ProviderConnection? _connection;
    private CopilotBillingScope _copilotScope = CopilotBillingScope.PersonalUser;
    private string _username = string.Empty;
    private string _organization = string.Empty;
    private string _serverAddress = "http://127.0.0.1:58627/";
    private string? _validationMessage;
    private bool _isSaving;

    public ProviderConnectionEditorViewModel(
        ProviderCode code,
        ProviderConnection? connection,
        IProviderConnectionService service)
    {
        _code = code;
        _connection = connection;
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Hydrate(connection);
    }

    public string Title => _code switch
    {
        ProviderCode.Copilot => "Connect GitHub Copilot",
        ProviderCode.Claude => "Connect Anthropic Organization API",
        ProviderCode.Kimi => "Connect Kimi Code local API",
        _ => "Provider connection"
    };

    public string ChannelLabel => _code switch
    {
        ProviderCode.Copilot => "GitHub Copilot billing usage",
        ProviderCode.Claude => "Anthropic Organization API",
        ProviderCode.Kimi => "Kimi Code local API",
        _ => "Provider connection"
    };

    public bool IsCopilot => _code == ProviderCode.Copilot;

    public bool IsClaude => _code == ProviderCode.Claude;

    public bool IsKimi => _code == ProviderCode.Kimi;

    public CopilotBillingScope CopilotScope
    {
        get => _copilotScope;
        set => SetProperty(ref _copilotScope, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Organization
    {
        get => _organization;
        set => SetProperty(ref _organization, value);
    }

    public string ServerAddress
    {
        get => _serverAddress;
        set => SetProperty(ref _serverAddress, value);
    }

    public bool CredentialSaved => !string.IsNullOrWhiteSpace(_connection?.CredentialReference);

    public string CredentialStateText => CredentialSaved
        ? "Credential saved — the existing secret is never loaded into this editor."
        : "No credential saved.";

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    public async Task<bool> SaveAsync(string? newSecret, CancellationToken cancellationToken = default)
    {
        ValidationMessage = Validate(newSecret, removing: false);
        if (ValidationMessage is not null)
        {
            return false;
        }

        IsSaving = true;
        try
        {
            _connection = await _service.SaveAsync(
                new ProviderConnectionEdit(
                    _code,
                    ConnectionTypeFor(_code),
                    BuildConfiguration(),
                    string.IsNullOrWhiteSpace(newSecret) ? null : newSecret),
                cancellationToken).ConfigureAwait(true);
            OnPropertyChanged(nameof(CredentialSaved));
            OnPropertyChanged(nameof(CredentialStateText));
            ValidationMessage = null;
            return true;
        }
        catch
        {
            ValidationMessage = "The connection could not be saved. The previous saved state was preserved.";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public async Task<bool> RemoveCredentialAsync(CancellationToken cancellationToken = default)
    {
        ValidationMessage = Validate(null, removing: true);
        if (ValidationMessage is not null)
        {
            return false;
        }

        IsSaving = true;
        try
        {
            _connection = await _service.SaveAsync(
                new ProviderConnectionEdit(
                    _code,
                    ConnectionTypeFor(_code),
                    BuildConfiguration(),
                    removeCredential: true),
                cancellationToken).ConfigureAwait(true);
            OnPropertyChanged(nameof(CredentialSaved));
            OnPropertyChanged(nameof(CredentialStateText));
            ValidationMessage = null;
            return true;
        }
        catch
        {
            ValidationMessage = "The credential could not be removed. The previous saved state was preserved.";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private string? Validate(string? newSecret, bool removing)
    {
        if (_code == ProviderCode.Copilot &&
            CopilotScope == CopilotBillingScope.Organization &&
            string.IsNullOrWhiteSpace(Organization))
        {
            return "Organization is required for organization billing scope.";
        }

        if (_code == ProviderCode.Kimi &&
            (!Uri.TryCreate(ServerAddress, UriKind.Absolute, out var address) ||
             !address.IsLoopback ||
             (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps)))
        {
            return "Kimi server address must be an absolute loopback HTTP or HTTPS address.";
        }

        if (!removing && string.IsNullOrWhiteSpace(newSecret) && !CredentialSaved)
        {
            return "Enter a credential before saving this connection.";
        }

        if (removing && !CredentialSaved)
        {
            return "There is no saved credential to remove.";
        }

        return null;
    }

    private IReadOnlyDictionary<string, string?> BuildConfiguration()
    {
        var configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (_code == ProviderCode.Copilot)
        {
            configuration[ProviderConnectionConfigurationKeys.CopilotScope] = CopilotScope.ToString();
            configuration[ProviderConnectionConfigurationKeys.CopilotUsername] =
                string.IsNullOrWhiteSpace(Username) ? null : Username.Trim();
            configuration[ProviderConnectionConfigurationKeys.CopilotOrganization] =
                string.IsNullOrWhiteSpace(Organization) ? null : Organization.Trim();
        }
        else if (_code == ProviderCode.Claude)
        {
            configuration[ProviderConnectionConfigurationKeys.AnthropicChannel] = "organization-api";
        }
        else if (_code == ProviderCode.Kimi)
        {
            configuration[ProviderConnectionConfigurationKeys.KimiServerAddress] = ServerAddress.Trim();
        }

        return configuration;
    }

    private void Hydrate(ProviderConnection? connection)
    {
        if (connection is null)
        {
            return;
        }

        if (connection.Configuration.TryGetValue(ProviderConnectionConfigurationKeys.CopilotScope, out var scope) &&
            Enum.TryParse<CopilotBillingScope>(scope, ignoreCase: true, out var parsedScope))
        {
            CopilotScope = parsedScope;
        }

        if (connection.Configuration.TryGetValue(ProviderConnectionConfigurationKeys.CopilotUsername, out var username))
        {
            Username = username ?? string.Empty;
        }

        if (connection.Configuration.TryGetValue(ProviderConnectionConfigurationKeys.CopilotOrganization, out var organization))
        {
            Organization = organization ?? string.Empty;
        }

        if (connection.Configuration.TryGetValue(ProviderConnectionConfigurationKeys.KimiServerAddress, out var serverAddress) &&
            !string.IsNullOrWhiteSpace(serverAddress))
        {
            ServerAddress = serverAddress;
        }
    }

    private static ProviderConnectionType ConnectionTypeFor(ProviderCode code) => code switch
    {
        ProviderCode.Kimi => ProviderConnectionType.LocalMetadata,
        _ => ProviderConnectionType.OfficialApi
    };
}
