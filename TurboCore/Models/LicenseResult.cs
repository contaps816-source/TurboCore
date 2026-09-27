namespace TurboCore.Models;

public class LicenseResult
{
    public bool IsValid { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? PlanName { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public static LicenseResult Ok(string? plan = null, DateTime? expires = null) => new()
    {
        IsValid = true,
        Message = "Key válida.",
        PlanName = plan,
        ExpiresAt = expires
    };

    public static LicenseResult Fail(string message) => new()
    {
        IsValid = false,
        Message = message
    };
}
