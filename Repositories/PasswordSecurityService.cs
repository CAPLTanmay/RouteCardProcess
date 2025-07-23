using System.Security.Cryptography;
using System.Text;
using RouteCardProcess.Interfaces;

public class PasswordSecurityService : IPasswordSecurityService
{
    private readonly string _encryptionKey;
    private readonly ISystemLoggerRepository _systemLogger;

    // You can store this in config if needed. It must be exactly 16 bytes.
    private readonly byte[] _fixedIV = Encoding.UTF8.GetBytes("StaticInitVector"); // 16 bytes only

    public PasswordSecurityService(IConfiguration configuration, ISystemLoggerRepository systemLogger)
    {
        _encryptionKey = configuration["EncryptionSettings:Key"];
        _systemLogger = systemLogger;
    }

    public async Task<string> EncryptPassword(string plainPasswordText)
    {
        try
        {
            using var aes = Aes.Create();
            var key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.Key = key;
            aes.IV = _fixedIV; // Fixed IV for deterministic output

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainPasswordText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(encryptedBytes); // No IV concatenation now
        }
        catch (Exception ex)
        {
            await _systemLogger.LogAsync("PasswordSecurityService", "EncryptPassword", ex.ToString());
            return "Encryption failed. Please try again.";
        }
    }

    public async Task<string> DecryptPassword(string encryptedPasswordText)
    {
        try
        {
            var cipherBytes = Convert.FromBase64String(encryptedPasswordText);

            using var aes = Aes.Create();
            var key = Encoding.UTF8.GetBytes(_encryptionKey.PadRight(32).Substring(0, 32));
            aes.Key = key;
            aes.IV = _fixedIV; // Use same fixed IV

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            await _systemLogger.LogAsync("PasswordSecurityService", "DecryptPassword", ex.ToString());
            return "Invalid or corrupted encrypted password.";
        }
    }

}
