using System.Security.Cryptography;
using System.Text;

namespace core.Auth;

public class CryptoService(AuthSettings settings) : ICryptoService
{
    private readonly byte[] _key = SHA256.HashData(Encoding.UTF8.GetBytes(settings.EncryptionKey));

    public string Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(_key);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return $"{Convert.ToHexString(nonce)}:{Convert.ToHexString(tag)}:{Convert.ToHexString(cipherBytes)}";
    }

    public string Decrypt(string encryptedText)
    {
        var parts = encryptedText.Split(':');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid encrypted text format");

        var nonce = Convert.FromHexString(parts[0]);
        var tag = Convert.FromHexString(parts[1]);
        var cipherBytes = Convert.FromHexString(parts[2]);
        var plainBytes = new byte[cipherBytes.Length];

        using var aes = new AesGcm(_key);
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
