using AIUsageMonitor.Domain.Providers;

namespace AIUsageMonitor.Application.Providers;

public sealed class ProviderDetectionResult
{
    public ProviderCode Code { get; }
    public bool IsDetected { get; }
    public string? DetectionMethod { get; }
    public DateTimeOffset DetectedAt { get; }

    public ProviderDetectionResult(ProviderCode code, bool isDetected, string? detectionMethod, DateTimeOffset detectedAt)
    {
        Code = code;
        IsDetected = isDetected;
        DetectionMethod = detectionMethod;
        DetectedAt = detectedAt;
    }
}
