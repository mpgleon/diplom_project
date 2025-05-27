using diplom_project.Models;

namespace diplom_project.Services;
public interface IAuthService
{
    string GenerateJwtToken(User user);
    Task<(string Token, DateTime Expiry)> GenerateRefreshToken(User user);
}
