namespace core.Licensing;

public interface ILicenseService
{
    Task<LicenseValidationResult> ValidateAsync(string domain, string licenseKey);
}

public class LicenseValidationResult
{
    public bool Valid { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
