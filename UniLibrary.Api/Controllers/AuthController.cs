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

            bool requiresAdminApproval = role == UserRoles.Teacher && !noAdminsExist;

            AppUser newUser = new AppUser
            {
                FullName = fullName,
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(request.Password),
                Role = role,
                IsApproved = !requiresAdminApproval,
                ApprovedAt = requiresAdminApproval ? null : DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Insert(newUser);

            if (requiresAdminApproval)
            {
                return Ok(new AuthResponse
                {
                    User = JwtTokenService.ToUserResponse(newUser),
                    RequiresApproval = true,
                    Message = "Акаунт викладача створено. Вхід буде доступний після підтвердження адміністратором."
                });
            }

            AuthResponse authResponse = _jwtTokenService.CreateAuthResponse(newUser);
            authResponse.Message = role == UserRoles.Admin
                ? "Реєстрація успішна. Це перший акаунт, тому він автоматично став адміністратором."
                : "Реєстрація успішна";

            return Ok(authResponse);
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

            if (!user.IsApproved)
            {
                return BadRequest("Акаунт викладача очікує підтвердження адміністратором. Після підтвердження ви зможете увійти.");
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
        [HttpPut("users/{id:int}/approve-teacher")]
        public ActionResult<UserResponse> ApproveTeacher(int id)
        {
            AppUser? user = _context.Users.FindById(id);

            if (user == null)
            {
                return NotFound("Користувача не знайдено");
            }

            if (user.Role != UserRoles.Teacher)
            {
                return BadRequest("Підтвердження потрібне тільки для акаунтів викладачів.");
            }

            AppUser? admin = GetCurrentUser();

            if (admin is null)
            {
                return Unauthorized();
            }

            if (!user.IsApproved)
            {
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedByUserId = admin.Id;
                user.ApprovedByFullName = admin.FullName;
                _context.Users.Update(user);
            }

            return Ok(JwtTokenService.ToUserResponse(user));
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

            foreach (Book book in _context.Books.FindAll().ToList())
            {
                book.Rentals ??= new List<BookRental>();

                List<BookRental> userRentals = book.Rentals
                    .Where(rental => rental.UserId == id
                        && (rental.Status == RentalStatuses.Active || rental.Status == RentalStatuses.ReturnPending))
                    .ToList();

                if (userRentals.Count == 0 && book.RentedByUserId != id)
                {
                    continue;
                }

                foreach (BookRental rental in userRentals)
                {
                    rental.Status = RentalStatuses.Returned;
                    rental.ReturnConfirmedAt = DateTime.UtcNow;
                    book.AvailableCopies = Math.Min(book.TotalCopies, book.AvailableCopies + 1);
                }

                if (book.RentedByUserId == id)
                {
                    book.RentedByUserId = null;
                    book.RentedByFullName = null;
                    book.RentedByEmail = null;
                    book.RentedAt = null;
                    book.RentDueAt = null;
                    book.AvailableCopies = Math.Min(book.TotalCopies, Math.Max(book.AvailableCopies, 1));
                }

                book.IsAvailable = book.IsDigital || book.AvailableCopies > 0;
                book.UpdatedAt = DateTime.UtcNow;
                _context.Books.Update(book);
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
