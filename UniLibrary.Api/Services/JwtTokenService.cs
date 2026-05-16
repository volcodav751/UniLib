using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using UniLibrary.Api.Models;

namespace UniLibrary.Api.Services
{
    public class JwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AuthResponse CreateAuthResponse(AppUser user)
        {
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(GetExpiresMinutes());

            Claim[] claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            ];

            string key = _configuration["Jwt:Key"]
                ?? "UniLibrary development JWT secret key 2026. Change this long key before real deployment.";

            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(key));
            SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return new AuthResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = expiresAt,
                User = ToUserResponse(user)
            };
        }

        private int GetExpiresMinutes()
        {
            return int.TryParse(_configuration["Jwt:ExpiresMinutes"], out int minutes)
                ? minutes
                : 180;
        }

        public static UserResponse ToUserResponse(AppUser user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsApproved = user.IsApproved,
                IsApprovalRequired = user.Role == UserRoles.Teacher && !user.IsApproved,
                ApprovedAt = user.ApprovedAt,
                ApprovedByFullName = user.ApprovedByFullName ?? string.Empty
            };
        }
    }
}
