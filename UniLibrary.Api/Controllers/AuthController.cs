using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Data;
using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Services;

namespace UniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly LiteDbContext _context;

        public AuthController(LiteDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            string email = request.Email.Trim().ToLowerInvariant();
            string role = request.Role.Trim();

            if (role != UserRoles.Student && role != UserRoles.Teacher)
            {
                return BadRequest("Невірна роль користувача");
            }

            bool emailExists = _context.Users.Exists(user => user.Email == email);

            if (emailExists)
            {
                return BadRequest("Користувач з таким email вже існує");
            }

            AppUser newUser = new AppUser
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Insert(newUser);

            UserResponse response = new UserResponse
            {
                Id = newUser.Id,
                FullName = newUser.FullName,
                Email = newUser.Email,
                Role = newUser.Role
            };

            return Ok(response);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            string email = request.Email.Trim().ToLowerInvariant();

            AppUser? user = _context.Users.FindOne(user => user.Email == email);

            if (user == null)
            {
                return BadRequest("Користувача з таким email не знайдено");
            }

            bool isPasswordCorrect = PasswordHasher.VerifyPassword(
                request.Password,
                user.PasswordHash
            );

            if (!isPasswordCorrect)
            {
                return BadRequest("Невірний пароль");
            }

            UserResponse response = new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };

            return Ok(response);
        }
    }
}