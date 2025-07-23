public interface IPasswordSecurityService
{
    Task<string> EncryptPassword(string plainPasswordText);
    Task<string> DecryptPassword(string encryptedPasswordText);
}
