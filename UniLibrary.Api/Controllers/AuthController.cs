using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

            if (!UserRoles.IsValid(role))
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

            return Ok(ToUserResponse(newUser));
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

            return Ok(ToUserResponse(user));
        }

        [HttpGet("users")]
        public ActionResult<List<UserResponse>> GetUsers()
        {
            ActionResult? accessError = AdminOnly();

            if (accessError is not null)
            {
                return accessError;
            }

            List<UserResponse> users = _context.Users
                .FindAll()
                .OrderBy(user => user.Id)
                .Select(ToUserResponse)
                .ToList();

            return Ok(users);
        }

        [HttpDelete("users/{id:int}")]
        public IActionResult DeleteUser(int id)
        {
            ActionResult? accessError = AdminOnly();

            if (accessError is not null)
            {
                return accessError;
            }

            AppUser? user = _context.Users.FindById(id);

            if (user == null)
            {
                return NotFound("Користувача не знайдено");
            }

            if (TryGetCurrentUserId(out int currentUserId) && currentUserId == id)
            {
                return BadRequest("Адмін не може видалити власний акаунт під час активної сесії.");
            }

            if (user.Role == UserRoles.Admin)
            {
                int adminsCount = _context.Users.Count(existingUser => existingUser.Role == UserRoles.Admin);

                if (adminsCount <= 1)
                {
                    return BadRequest("Не можна видалити останнього адміністратора.");
                }
            }

            _context.Users.Delete(id);

            return NoContent();
        }

        private ActionResult? AdminOnly()
        {
            if (Request.Headers.TryGetValue("X-User-Role", out var roleHeader)
                && string.Equals(roleHeader.ToString(), UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return StatusCode(StatusCodes.Status403Forbidden, "Ця дія доступна тільки адміністратору.");
        }

        private bool TryGetCurrentUserId(out int currentUserId)
        {
            currentUserId = 0;

            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            {
                return false;
            }

            return int.TryParse(userIdHeader.ToString(), out currentUserId);
        }

        private static UserResponse ToUserResponse(AppUser user)
        {
            return new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };
        }
    }
}
