namespace diplom_project.Services
{
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.Extensions.Configuration;
    using System.Threading.Tasks;
    using diplom_project.Models;
    using diplom_project.Controllers;
    using Microsoft.EntityFrameworkCore;
    


    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public string GenerateJwtToken(User user)
        {
            var userRoles = _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ToList(); // Предварительно загружаем данные

            var roles = userRoles
                .Join(_context.Roles ?? Enumerable.Empty<Role>(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name)
                .ToList();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            }.Concat(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<(string Token, DateTime Expiry)> GenerateRefreshToken(User user)
        {
            var token = Guid.NewGuid().ToString(); // Заменить на криптографически безопасный генератор
            var expiry = DateTime.UtcNow.AddDays(7);

            user.RefreshToken = token;
            user.RefreshTokenExpiryTime = expiry;
            await _context.SaveChangesAsync();

            return (token, expiry);
        }
        public async Task<User> RegisterUser(RegisterModel model)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null)
            {
                throw new Exception("User with this email already exists");
            }

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var user = new User
            {
                Email = model.Email,
                Phone = model.Phone,
                Password = hashedPassword,
                Datestamp = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userProfile = new UserProfile
            {
                UserId = user.Id,
                Email = user.Email,
                Phone = user.Phone,
                FirstName = model.FirstName ?? "Unknown",
                LastName = model.LastName ?? "Unknown",
                DateOfBirth = DateTime.Today,
                IsVerified = false,
                Rating = 0.0M,
                Description = null,
                PhotoUrl = null
            };

            _context.UserProfiles.Add(userProfile);
            await _context.SaveChangesAsync();

            // Назначение роли "Tenant" по умолчанию
            var tenantRole = await _context.Roles.FirstAsync(r => r.Name == "Tenant");
            _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = tenantRole.Id });
            await _context.SaveChangesAsync();

            return user;
        }
    }
}
