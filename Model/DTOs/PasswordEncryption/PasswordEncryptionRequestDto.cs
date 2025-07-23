namespace RouteCardProcess.Model.DTOs.PasswordEncryption
{
    // Models/EncryptionSettings.cs
    public class EncryptionSettings
    {
        public string Key { get; set; }
    }

    public class PasswordEncryptionRequestDto
    {
        public string PlainPasswordText { get; set; } // Input plain password
    }
    public class PasswordDecryptionRequestDto
    {
        public string EncryptedPasswordText { get; set; } // Input encrypted password
    }
}
