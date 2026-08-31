using AIUsageMonitor.Application.Handoffs;
using AIUsageMonitor.Application.Orchestration;
using AIUsageMonitor.Application.Projects;
using AIUsageMonitor.Application.RemoteEvidence;
using AIUsageMonitor.Application.Time;
using AIUsageMonitor.Application.Trackers;
using AIUsageMonitor.Application.Validation;
using AIUsageMonitor.Infrastructure.Execution;

namespace AIUsageMonitor.Infrastructure.Validation;

internal static class ValidationEvidenceFactory
{
    public static ValidationEvidence Create(
        ValidationCollectionContext context,
        ValidationEvidenceState state,
        ValidationOutcome outcome,
        DateTimeOffset capturedAt,
        bool securityBoundaryValid = true,
        string? targetIdentity = null,
        string? localHeadCommitSha = null,
        string? branchName = null,
        bool? localIsClean = null,
        string? repositoryIdentity = null,
        string? remoteCommitId = null,
        string? trackerProjectId = null,
        string? trackerWorkItemKey = null,
        string? trackerStatus = null,
        int stdoutBytes = 0,
        int stderrBytes = 0,
        bool outputTruncated = false,
        string? reasonCode = null) =>
        new(
            context.Plan.ProjectId,
            Guid.NewGuid(),
            context.Plan.Reference,
            context.Requirement.RequirementId,
            context.Authority.RunId,
            context.Authority.Reference,
            context.Plan.PlanningContractReference,
            context.Plan.WorkGraphReference,
            context.Plan.WorkGraphNodeId,
            context.CurrentCheckpoint.Reference,
            context.Plan.WorkspaceId,
            context.Plan.WorkspacePath,
            context.Plan.WorkspaceReceiptContentHash,
            context.Requirement.CollectorIdentifier,
            context.Requirement.EvidenceKind,
            state,
            outcome,
            context.Requirement.Coverage,
            context.Requirement.BaselineRelation,
            capturedAt,
            independentlyCaptured: true,
            securityBoundaryValid,
            baselineEvidenceReference: context.BaselineEvidenceReference,
            targetIdentity: targetIdentity,
            localHeadCommitSha: localHeadCommitSha,
            branchName: branchName,
            localIsClean: localIsClean,
            repositoryIdentity: repositoryIdentity,
            remoteCommitId: remoteCommitId,
            trackerProjectId: trackerProjectId,
            trackerWorkItemKey: trackerWorkItemKey,
            trackerStatus: trackerStatus,
            stdoutBytes: stdoutBytes,
            stderrBytes: stderrBytes,
            outputTruncated: outputTruncated,
            reasonCode: reasonCode,
            validationDefinitionId: context.Requirement.ValidationDefinitionId);
}

public sealed class DotNetValidationEvidenceCollector : IValidationEvidenceCollector
{
    public const string CollectorIdentifier = "dotnet";
    public static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromMinutes(10);

    private readonly IBoundedProcessHost _processHost;
    private readonly IClock _clock;
    private readonly IHandoffRedactionService _redaction;

    public DotNetValidationEvidenceCollector(IBoundedProcessHost processHost, IClock clock, IHandoffRedactionService redaction)
    {
        _processHost = processHost ?? throw new ArgumentNullException(nameof(processHost));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _redaction = redaction ?? throw new ArgumentNullException(nameof(redaction));
        Descriptor = new(CollectorIdentifier, [ValidationEvidenceKind.Build, ValidationEvidenceKind.Test], true, true);
    }

    public ValidationEvidenceCollectorDescriptor Descriptor { get; }

    public async Task<ValidationEvidence> CaptureAsync(ValidationCollectionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!TryGetTarget(context, out var target, out var relativeTarget))
            return ValidationEvidenceFactory.Create(context, ValidationEvidenceState.Invalid, ValidationOutcome.Unknown, _clock.UtcNow, reasonCode: ValidationReasonCodes.EvidenceNotUsable);

        var arguments = new List<string>
        {
            context.Requirement.EvidenceKind == ValidationEvidenceKind.Build ? "build" : "test",
            relativeTarget,
            "--no-restore",
            "--nologo"
        };
        if (context.Requirement.EvidenceKind == ValidationEvidenceKind.Test && context.Requirement.TestFilter is { } filter)
        {
            if (!IsSafeArgument(filter))
                return ValidationEvidenceFactory.Create(context, ValidationEvidenceState.Invalid, ValidationOutcome.Unknown, _clock.UtcNow, targetIdentity: relativeTarget, reasonCode: ValidationReasonCodes.EvidenceNotUsable);
            arguments.Add("--filter");
            arguments.Add(filter);
        }

        var result = await _processHost.RunAsync(
            new BoundedProcessRequest("dotnet", arguments, context.Plan.WorkspacePath, context.Requirement.Timeout ?? DefaultCommandTimeout),
            cancellationToken).ConfigureAwait(false);
        var output = result.StandardOutput + Environment.NewLine + result.StandardError;
        if (_redaction.ValidateIdentityText(output).RequiresRedaction)
            return ValidationEvidenceFactory.Create(context, ValidationEvidenceState.RedactionRejected, ValidationOutcome.Unknown, _clock.UtcNow,
                targetIdentity: target,
                stdoutBytes: System.Text.Encoding.UTF8.GetByteCount(result.StandardOutput),
                stderrBytes: System.Text.Encoding.UTF8.GetByteCount(result.StandardError),
                outputTruncated: result.StandardOutputTruncated || result.StandardErrorTruncated,
                reasonCode: ValidationReasonCodes.SecurityBoundaryInvalid);
        var state = result.Outcome switch
        {
            BoundedProcessOutcome.ExitedSuccessfully or BoundedProcessOutcome.NonZeroExit => ValidationEvidenceState.Available,
            BoundedProcessOutcome.Cancelled => ValidationEvidenceState.Cancelled,
            BoundedProcessOutcome.TimedOut => ValidationEvidenceState.TimedOut,
            _ => ValidationEvidenceState.Unavailable
        };
        var outcome = result.Outcome switch
        {
            BoundedProcessOutcome.ExitedSuccessfully => ValidationOutcome.Passed,
            BoundedProcessOutcome.NonZeroExit => ValidationOutcome.Failed,
            _ => ValidationOutcome.Unknown
        };
        return ValidationEvidenceFactory.Create(
            context,
            state,
            outcome,
            _clock.UtcNow,
            targetIdentity: target,
            stdoutBytes: System.Text.Encoding.UTF8.GetByteCount(result.StandardOutput),
            stderrBytes: System.Text.Encoding.UTF8.GetByteCount(result.StandardError),
            outputTruncated: result.StandardOutputTruncated || result.StandardErrorTruncated,
            reasonCode: result.Outcome == BoundedProcessOutcome.NonZeroExit ? ValidationReasonCodes.EvidenceFailed : null);
    }

    private static bool TryGetTarget(ValidationCollectionContext context, out string target, out string relativeTarget)
    {
        target = string.Empty;
        relativeTarget = string.Empty;
        var candidate = context.Requirement.TargetPath;
        if (string.IsNullOrWhiteSpace(candidate) || !IsSafeArgument(candidate) || Path.IsPathFullyQualified(candidate)) return false;
        try
        {
            var workspace = Path.GetFullPath(context.Plan.WorkspacePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(workspace, candidate));
            var relative = Path.GetRelativePath(workspace, fullPath);
            if (relative == "." || relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathFullyQualified(relative)) return false;
            target = fullPath;
            relativeTarget = relative;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool IsSafeArgument(string value) =>
        value.Length <= ValidationLimits.MaxIdentityLength &&
        value.All(static character => !char.IsControl(character));
}

public sealed class LocalRepositoryValidationEvidenceCollector : IValidationEvidenceCollector
{
    public const string CollectorIdentifier = "local-git";
    private readonly ILocalRepositoryInspector _inspector;
    private readonly IClock _clock;

    public LocalRepositoryValidationEvidenceCollector(ILocalRepositoryInspector inspector, IClock? clock = null)
    {
        _inspector = inspector ?? throw new ArgumentNullException(nameof(inspector));
        _clock = clock ?? new SystemClock();
        Descriptor = new(CollectorIdentifier, [ValidationEvidenceKind.LocalRepository], false, true);
    }

    public ValidationEvidenceCollectorDescriptor Descriptor { get; }

    public async Task<ValidationEvidence> CaptureAsync(ValidationCollectionContext context, CancellationToken cancellationToken = default)
    {
        var inspection = await _inspector.InspectAsync(context.Plan.WorkspacePath, context.Project.RepositoryUrl, cancellationToken).ConfigureAwait(false);
        var available = inspection.Status is RepositoryVerificationStatus.AvailableClean or RepositoryVerificationStatus.AvailableDirty;
        var state = available ? ValidationEvidenceState.Available : inspection.Status switch
        {
            RepositoryVerificationStatus.PathMissing => ValidationEvidenceState.Missing,
            RepositoryVerificationStatus.NotGitRepository => ValidationEvidenceState.Invalid,
            RepositoryVerificationStatus.PathUnavailable or RepositoryVerificationStatus.GitUnavailable => ValidationEvidenceState.Unavailable,
            _ => ValidationEvidenceState.Unavailable
        };
        var outcome = available && !(context.Requirement.RequireCleanWorktree == true && inspection.IsClean != true)
            ? ValidationOutcome.Passed
            : available ? ValidationOutcome.Failed : ValidationOutcome.Unknown;
        return ValidationEvidenceFactory.Create(context, state, outcome, _clock.UtcNow,
            localHeadCommitSha: inspection.HeadSha,
            branchName: inspection.BranchName,
            localIsClean: inspection.IsClean,
            repositoryIdentity: inspection.RepositoryRoot,
            reasonCode: outcome == ValidationOutcome.Failed ? ValidationReasonCodes.RepositoryMismatch : null);
    }
}

public sealed class RemoteValidationEvidenceCollector : IValidationEvidenceCollector
{
    public const string CollectorIdentifier = "remote-repository";
    private readonly IRemoteRepositoryEvidenceService _service;

    public RemoteValidationEvidenceCollector(IRemoteRepositoryEvidenceService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Descriptor = new(CollectorIdentifier, [ValidationEvidenceKind.RemoteRepository, ValidationEvidenceKind.RemoteCi], false, true);
    }

    public ValidationEvidenceCollectorDescriptor Descriptor { get; }

    public async Task<ValidationEvidence> CaptureAsync(ValidationCollectionContext context, CancellationToken cancellationToken = default)
    {
        var remote = await _service.InspectAsync(context.Project, context.Requirement.RequestedBranch, context.Requirement.PullRequestNumber, cancellationToken).ConfigureAwait(false);
        var repositoryIdentity = remote.Repository?.CanonicalName;
        var repositoryCommitId = remote.Branch?.CommitId ?? remote.PullRequest?.HeadCommitId;
        if (context.Requirement.EvidenceKind == ValidationEvidenceKind.RemoteRepository)
        {
            var state = Map(remote.RepositoryState);
            if (state == ValidationEvidenceState.Available && remote.Repository is null) state = ValidationEvidenceState.Missing;
            return ValidationEvidenceFactory.Create(context, state, state == ValidationEvidenceState.Available ? ValidationOutcome.Passed : ValidationOutcome.Unknown, remote.CapturedAt,
                repositoryIdentity: repositoryIdentity, remoteCommitId: repositoryCommitId, reasonCode: state == ValidationEvidenceState.Available ? null : ValidationReasonCodes.EvidenceNotUsable);
        }

        var targetCiCommitId = context.Requirement.PullRequestNumber is not null
            ? remote.PullRequest?.HeadCommitId
            : remote.Branch?.CommitId;
        var ciCommitIds = remote.CiRuns.Where(value => value.HeadCommitId is not null).Select(value => value.HeadCommitId!).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var commitMustBeProven = remote.CiResult is RemoteCiState.Passing or RemoteCiState.Failing;
        var missingCiIdentity = commitMustBeProven && (string.IsNullOrWhiteSpace(targetCiCommitId) || remote.CiRuns.Count == 0 || remote.CiRuns.Any(value => string.IsNullOrWhiteSpace(value.HeadCommitId)));
        var conflictingCiIdentity = commitMustBeProven && !missingCiIdentity && ciCommitIds.Any(value => !string.Equals(value, targetCiCommitId, StringComparison.OrdinalIgnoreCase));
        if (missingCiIdentity || conflictingCiIdentity)
        {
            return ValidationEvidenceFactory.Create(context, ValidationEvidenceState.Invalid, ValidationOutcome.Unknown, remote.CapturedAt,
                repositoryIdentity: repositoryIdentity, remoteCommitId: targetCiCommitId,
                reasonCode: missingCiIdentity ? ValidationReasonCodes.RemoteCiCommitIdentityMissing : ValidationReasonCodes.RemoteCiCommitConflict);
        }

        var ciState = remote.CiResult switch
        {
            RemoteCiState.Passing => ValidationEvidenceState.Available,
            RemoteCiState.Failing => ValidationEvidenceState.Available,
            RemoteCiState.NoEvidence => ValidationEvidenceState.Missing,
            RemoteCiState.Pending => ValidationEvidenceState.Partial,
            RemoteCiState.Cancelled => ValidationEvidenceState.Cancelled,
            _ => Map(remote.CiState)
        };
        var outcome = remote.CiResult switch
        {
            RemoteCiState.Passing => ValidationOutcome.Passed,
            RemoteCiState.Failing => ValidationOutcome.Failed,
            _ => ValidationOutcome.Unknown
        };
        return ValidationEvidenceFactory.Create(context, ciState, outcome, remote.CapturedAt,
            repositoryIdentity: repositoryIdentity, remoteCommitId: targetCiCommitId,
            reasonCode: outcome == ValidationOutcome.Failed ? ValidationReasonCodes.EvidenceFailed : null);
    }

    private static ValidationEvidenceState Map(RemoteEvidenceState state) => state switch
    {
        RemoteEvidenceState.Available => ValidationEvidenceState.Available,
        RemoteEvidenceState.Partial => ValidationEvidenceState.Partial,
        RemoteEvidenceState.AuthenticationRequired => ValidationEvidenceState.AuthenticationRequired,
        RemoteEvidenceState.PermissionDenied => ValidationEvidenceState.PermissionDenied,
        RemoteEvidenceState.Unsupported => ValidationEvidenceState.Unsupported,
        RemoteEvidenceState.RateLimited => ValidationEvidenceState.RateLimited,
        RemoteEvidenceState.Stale => ValidationEvidenceState.Stale,
        RemoteEvidenceState.NotConfigured => ValidationEvidenceState.Missing,
        RemoteEvidenceState.InvalidResponse => ValidationEvidenceState.Invalid,
        RemoteEvidenceState.Cancelled => ValidationEvidenceState.Cancelled,
        _ => ValidationEvidenceState.Unavailable
    };
}

public sealed class TrackerValidationEvidenceCollector : IValidationEvidenceCollector
{
    public const string CollectorIdentifier = "tracker";
    private readonly IWorkItemTrackerAdapterResolver _resolver;
    private readonly IClock _clock;

    public TrackerValidationEvidenceCollector(IWorkItemTrackerAdapterResolver resolver, IClock clock)
    {
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Descriptor = new(CollectorIdentifier, [ValidationEvidenceKind.Tracker], false, true);
    }

    public ValidationEvidenceCollectorDescriptor Descriptor { get; }

    public async Task<ValidationEvidence> CaptureAsync(ValidationCollectionContext context, CancellationToken cancellationToken = default)
    {
        var resolution = _resolver.Resolve(context.Project);
        if (!resolution.Succeeded || resolution.Configuration is null || resolution.Adapter is null)
        {
            var resolutionState = resolution.Status switch
            {
                TrackerAdapterResolutionStatus.NotConfigured => ValidationEvidenceState.Missing,
                TrackerAdapterResolutionStatus.ConfigurationConflict => ValidationEvidenceState.ConfigurationConflict,
                _ => ValidationEvidenceState.Unsupported
            };
            return ValidationEvidenceFactory.Create(context, resolutionState, ValidationOutcome.Unknown, _clock.UtcNow, reasonCode: ValidationReasonCodes.EvidenceNotUsable);
        }

        var key = context.Requirement.ExpectedTrackerWorkItemKey;
        if (string.IsNullOrWhiteSpace(key))
            return ValidationEvidenceFactory.Create(context, ValidationEvidenceState.Invalid, ValidationOutcome.Unknown, _clock.UtcNow, reasonCode: ValidationReasonCodes.TrackerMismatch);

        var target = new TrackerWorkItemIdentity(resolution.Configuration.Identity.Provider, resolution.Configuration.Identity.ProjectId, key);
        var read = await resolution.Adapter.ReadAsync(resolution.Configuration, target, cancellationToken: cancellationToken).ConfigureAwait(false);
        var state = Map(read.State);
        var outcome = state == ValidationEvidenceState.Available && read.Value is not null ? ValidationOutcome.Passed : ValidationOutcome.Unknown;
        return ValidationEvidenceFactory.Create(context, state, outcome, read.CapturedAt,
            trackerProjectId: read.Project.ProjectId,
            trackerWorkItemKey: read.Target?.KeyOrId,
            trackerStatus: read.Value?.Status.Id,
            reasonCode: state == ValidationEvidenceState.Available ? null : ValidationReasonCodes.EvidenceNotUsable);
    }

    private static ValidationEvidenceState Map(TrackerEvidenceState state) => state switch
    {
        TrackerEvidenceState.Available => ValidationEvidenceState.Available,
        TrackerEvidenceState.NotConfigured or TrackerEvidenceState.NotFound => ValidationEvidenceState.Missing,
        TrackerEvidenceState.AuthenticationRequired => ValidationEvidenceState.AuthenticationRequired,
        TrackerEvidenceState.PermissionDenied => ValidationEvidenceState.PermissionDenied,
        TrackerEvidenceState.Unsupported => ValidationEvidenceState.Unsupported,
        TrackerEvidenceState.RateLimited => ValidationEvidenceState.RateLimited,
        TrackerEvidenceState.Partial => ValidationEvidenceState.Partial,
        TrackerEvidenceState.Stale => ValidationEvidenceState.Stale,
        TrackerEvidenceState.InvalidResponse => ValidationEvidenceState.Invalid,
        TrackerEvidenceState.Cancelled => ValidationEvidenceState.Cancelled,
        _ => ValidationEvidenceState.Unavailable
    };
}

public sealed class SecurityValidationEvidenceCollector : IValidationEvidenceCollector
{
    public const string CollectorIdentifier = "security-boundary";
    private readonly IHandoffRedactionService _redaction;
    private readonly IClock _clock;

    public SecurityValidationEvidenceCollector(IHandoffRedactionService redaction, IClock clock)
    {
        _redaction = redaction ?? throw new ArgumentNullException(nameof(redaction));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Descriptor = new(CollectorIdentifier, [ValidationEvidenceKind.Security], false, true);
    }

    public ValidationEvidenceCollectorDescriptor Descriptor { get; }

    public Task<ValidationEvidence> CaptureAsync(ValidationCollectionContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var texts = new List<string> { context.Plan.WorkspacePath, context.Requirement.CollectorIdentifier };
        var safeCollectors = new[] { DotNetValidationEvidenceCollector.CollectorIdentifier, LocalRepositoryValidationEvidenceCollector.CollectorIdentifier, RemoteValidationEvidenceCollector.CollectorIdentifier, TrackerValidationEvidenceCollector.CollectorIdentifier, CollectorIdentifier, RuntimeValidationEvidenceCollector.CollectorIdentifier };
        var evidenceTexts = context.ExistingEvidence.SelectMany(static value => new[] { value.WorkspacePath, value.CollectorIdentifier, value.RepositoryIdentity, value.TrackerProjectId, value.TrackerWorkItemKey, value.TrackerStatus, value.DiagnosticSummary, value.TargetIdentity }).Where(static value => value is not null).Select(static value => value!);
        var collectorSetIsSafe = safeCollectors.Contains(context.Requirement.CollectorIdentifier, StringComparer.Ordinal) && context.ExistingEvidence.All(value => safeCollectors.Contains(value.CollectorIdentifier, StringComparer.Ordinal));
        var workspace = Path.GetFullPath(context.Plan.WorkspacePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetPathsAreSafe = context.ExistingEvidence.Where(value => value.TargetIdentity is not null).All(value => !Path.IsPathFullyQualified(value.TargetIdentity!) || IsWithin(workspace, value.TargetIdentity!));
        var rejected = !collectorSetIsSafe || !targetPathsAreSafe || texts.Concat(evidenceTexts).Any(value => _redaction.ValidateIdentityText(value).RequiresRedaction);
        return Task.FromResult(ValidationEvidenceFactory.Create(context,
            rejected ? ValidationEvidenceState.RedactionRejected : ValidationEvidenceState.Available,
            rejected ? ValidationOutcome.Unknown : ValidationOutcome.Passed,
            _clock.UtcNow,
            securityBoundaryValid: !rejected,
            reasonCode: rejected ? ValidationReasonCodes.SecurityBoundaryInvalid : ValidationReasonCodes.Satisfied));
    }

    private static bool IsWithin(string root, string path)
    {
        try
        {
            var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(full, root, StringComparison.OrdinalIgnoreCase) || full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

public sealed class RuntimeValidationEvidenceCollector : IValidationEvidenceCollector
{
    public const string CollectorIdentifier = "runtime-checkpoint";
    private readonly IClock _clock;

    public RuntimeValidationEvidenceCollector(IClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Descriptor = new(CollectorIdentifier, [ValidationEvidenceKind.Runtime], false, true);
    }

    public ValidationEvidenceCollectorDescriptor Descriptor { get; }

    public Task<ValidationEvidence> CaptureAsync(ValidationCollectionContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var valid = context.Authority.ProjectId == context.Plan.ProjectId &&
            context.Authority.WorkspaceId == context.Plan.WorkspaceId &&
            string.Equals(context.Authority.WorkspacePath, context.Plan.WorkspacePath, StringComparison.Ordinal) &&
            string.Equals(context.Authority.WorkspaceReceiptContentHash, context.Plan.WorkspaceReceiptContentHash, StringComparison.OrdinalIgnoreCase) &&
            context.CurrentCheckpoint.Reference.ToString() == context.Plan.CurrentRecoveryCheckpointReference.ToString() &&
            context.CurrentCheckpoint.LifecycleState == RecoveryCheckpointLifecycleState.Ready &&
            context.CurrentCheckpoint.NextSafeAction == RecoveryNextSafeAction.RunValidation;
        return Task.FromResult(ValidationEvidenceFactory.Create(context,
            valid ? ValidationEvidenceState.Available : ValidationEvidenceState.Invalid,
            valid ? ValidationOutcome.Passed : ValidationOutcome.Failed,
            _clock.UtcNow,
            reasonCode: valid ? ValidationReasonCodes.Satisfied : ValidationReasonCodes.EvidenceNotUsable));
    }
}
