using RouteCardProcess.Model;

namespace RouteCardProcess.Interfaces
{
    public interface IKblAuthService
    {
        Task<string> AuthenticateLoginAsync(KblLoginRequest request);
        Task<string> GetTokenAsync();
        Task<KblEmpInfoResponse> GetEmployeeInfoAsync(string token, string empId);
        Task<string?> EncryptPasswordAsync(string plainPassword);
    }
}
