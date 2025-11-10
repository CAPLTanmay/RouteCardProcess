using Microsoft.AspNetCore.Mvc;
using RouteCardProcess.Model.DTOs.PasswordEncryption;

[Route("api/[controller]")]
[ApiController]

public class PasswordSecurityController : ControllerBase
{
    private readonly IPasswordSecurityService _passwordService;

    public PasswordSecurityController(IPasswordSecurityService passwordService)
    {
        _passwordService = passwordService;
    }

    [HttpPost("encrypt-password")]
    public IActionResult EncryptPassword([FromBody] PasswordEncryptionRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.PlainPasswordText))
            return BadRequest("Password text cannot be empty.");

        var encrypted = _passwordService.EncryptPassword(requestDto.PlainPasswordText);
        return Ok(new { EncryptedPasswordText = encrypted });
    }

    [HttpPost("decrypt-password")]
    public IActionResult DecryptPassword([FromBody] PasswordDecryptionRequestDto requestDto)
    {
        try
        {
            var decrypted = _passwordService.DecryptPassword(requestDto.EncryptedPasswordText);
            return Ok(new { DecryptedPasswordText = decrypted });
        }
        catch
        {
            return BadRequest("Invalid encrypted password.");
        }
    }
}
