using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(LiteDbContext context, JwtTokenService jwtTokenService)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(RegisterRequest request)
        {
            string fullName = request.FullName.Trim();
            string email = request.Email.Trim().ToLowerInvariant();
            string requestedRole = request.Role.Trim();
            bool noAdminsExist = _context.Users.Count(user => user.Role == UserRoles.Admin) == 0;

            string role = noAdminsExist ? UserRoles.Admin : requestedRole;

            if (!noAdminsExist && !UserRoles.CanSelfRegister(role))
            {
                return BadRequest("Через звичайну реєстрацію можна створити тільки студента або викладача. Якщо в системі ще немає адміністратора, наступний зареєстрований акаунт автоматично стане Admin.");
            }

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
                FullName = fullName,
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Insert(newUser);

            return Ok(_jwtTokenService.CreateAuthResponse(newUser));
        }

        [AllowAnonymous]
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

            return Ok(_jwtTokenService.CreateAuthResponse(user));
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<UserResponse> Me()
        {
            AppUser? user = GetCurrentUser();

            if (user is null)
            {
                return Unauthorized();
            }

            return Ok(JwtTokenService.ToUserResponse(user));
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpGet("users")]
        public ActionResult<List<UserResponse>> GetUsers()
        {
            List<UserResponse> users = _context.Users
                .FindAll()
                .OrderBy(user => user.Id)
                .Select(JwtTokenService.ToUserResponse)
                .ToList();

            return Ok(users);
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpDelete("users/{id:int}")]
        public IActionResult DeleteUser(int id)
        {
            AppUser? user = _context.Users.FindById(id);

            if (user == null)
            {
                return NotFound("Користувача не знайдено");
            }

            int? currentUserId = GetCurrentUserId();

            if (currentUserId == id)
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

        private AppUser? GetCurrentUser()
        {
            int? userId = GetCurrentUserId();

            return userId is null ? null : _context.Users.FindById(userId.Value);
        }

        private int? GetCurrentUserId()
        {
            string? userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(userIdText, out int userId) ? userId : null;
        }
    }
}
