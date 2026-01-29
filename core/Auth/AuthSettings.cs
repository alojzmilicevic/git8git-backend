namespace core.Auth;

public class AuthSettings
{
    public string JwtSecret { get; set; } = string.Empty;
    public int JwtExpiryHours { get; set; } = 1;
    public string EncryptionKey { get; set; } = string.Empty;
    public string ChromeExtensionId { get; set; } = string.Empty;
}
