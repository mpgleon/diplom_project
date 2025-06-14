using diplom_project.Models;
using diplom_project.Controllers;

namespace diplom_project.Services;

public interface IAuthService
{
    string GenerateJwtToken(User user);
    Task<(string Token, DateTime Expiry)> GenerateRefreshToken(User user);
    Task<User> RegisterUser(RegisterModel model);

}
