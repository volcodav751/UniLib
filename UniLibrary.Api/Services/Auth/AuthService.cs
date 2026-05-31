using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Repos;
using UniLibrary.Api.Services.Books;
using UniLibrary.Api.Services.Common;
using UniLibrary.Api.Services.Results;
using UniLibrary.Api.Interfaces;

namespace UniLibrary.Api.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IBookRepository _books;
    private readonly ICurrentUserService _currentUser;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(
        IUserRepository users,
        IBookRepository books,
        ICurrentUserService currentUser,
        JwtTokenService jwtTokenService)
    {
        _users = users;
        _books = books;
        _currentUser = currentUser;
        _jwtTokenService = jwtTokenService;
    }

    public ServiceResult<AuthResponse> Register(RegisterRequest request)
    {
        string fullName = request.FullName.Trim();
        string email = request.Email.Trim().ToLowerInvariant();
        string requestedRole = request.Role.Trim();
        bool noAdminsExist = _users.CountAdmins() == 0;

        string role = noAdminsExist ? UserRoles.Admin : requestedRole;

        if (!noAdminsExist && !UserRoles.CanSelfRegister(role))
        {
            return ServiceResult<AuthResponse>.BadRequest("Через звичайну реєстрацію можна створити тільки студента або викладача. Якщо в системі ще немає адміністратора, наступний зареєстрований акаунт автоматично стане Admin.");
        }

        if (!UserRoles.IsValid(role))
        {
            return ServiceResult<AuthResponse>.BadRequest("Невірна роль користувача");
        }

        if (_users.EmailExists(email))
        {
            return ServiceResult<AuthResponse>.BadRequest("Користувач з таким email вже існує");
        }

        bool requiresAdminApproval = role == UserRoles.Teacher && !noAdminsExist;

        AppUser newUser = new()
        {
            FullName = fullName,
            Email = email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = role,
            IsApproved = !requiresAdminApproval,
            ApprovedAt = requiresAdminApproval ? null : DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _users.Add(newUser);

        if (requiresAdminApproval)
        {
            return ServiceResult<AuthResponse>.Ok(new AuthResponse
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

        return ServiceResult<AuthResponse>.Ok(authResponse);
    }

    public ServiceResult<AuthResponse> Login(LoginRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        AppUser? user = _users.GetByEmail(email);

        if (user is null)
        {
            return ServiceResult<AuthResponse>.BadRequest("Користувача з таким email не знайдено");
        }

        bool isPasswordCorrect = PasswordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!isPasswordCorrect)
        {
            return ServiceResult<AuthResponse>.BadRequest("Невірний пароль");
        }

        if (!user.IsApproved)
        {
            return ServiceResult<AuthResponse>.BadRequest("Акаунт викладача очікує підтвердження адміністратором. Після підтвердження ви зможете увійти.");
        }

        return ServiceResult<AuthResponse>.Ok(_jwtTokenService.CreateAuthResponse(user));
    }

    public ServiceResult<UserResponse> GetCurrentUser()
    {
        int? userId = _currentUser.UserId;

        if (userId is null)
        {
            return ServiceResult<UserResponse>.Unauthorized();
        }

        AppUser? user = _users.GetById(userId.Value);

        return user is null
            ? ServiceResult<UserResponse>.Unauthorized()
            : ServiceResult<UserResponse>.Ok(JwtTokenService.ToUserResponse(user));
    }

    public List<UserResponse> GetUsers()
    {
        return _users.GetAll()
            .OrderBy(user => user.Id)
            .Select(JwtTokenService.ToUserResponse)
            .ToList();
    }

    public ServiceResult<UserResponse> ApproveTeacher(int id)
    {
        AppUser? user = _users.GetById(id);

        if (user is null)
        {
            return ServiceResult<UserResponse>.NotFound("Користувача не знайдено");
        }

        if (user.Role != UserRoles.Teacher)
        {
            return ServiceResult<UserResponse>.BadRequest("Підтвердження потрібне тільки для акаунтів викладачів.");
        }

        AppUser? admin = GetCurrentAppUser();

        if (admin is null)
        {
            return ServiceResult<UserResponse>.Unauthorized();
        }

        if (!user.IsApproved)
        {
            user.IsApproved = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedByUserId = admin.Id;
            user.ApprovedByFullName = admin.FullName;
            _users.Update(user);
        }

        return ServiceResult<UserResponse>.Ok(JwtTokenService.ToUserResponse(user));
    }

    public ServiceResult DeleteUser(int id)
    {
        AppUser? user = _users.GetById(id);

        if (user is null)
        {
            return ServiceResult.NotFound("Користувача не знайдено");
        }

        if (_currentUser.UserId == id)
        {
            return ServiceResult.BadRequest("Адмін не може видалити власний акаунт під час активної сесії.");
        }

        if (user.Role == UserRoles.Admin && _users.CountAdmins() <= 1)
        {
            return ServiceResult.BadRequest("Не можна видалити останнього адміністратора.");
        }

        ReturnActiveRentalsForDeletedUser(id);
        _users.Delete(id);

        return ServiceResult.NoContent();
    }

    private AppUser? GetCurrentAppUser()
    {
        int? userId = _currentUser.UserId;
        return userId is null ? null : _users.GetById(userId.Value);
    }

    private void ReturnActiveRentalsForDeletedUser(int userId)
    {
        foreach (Book book in _books.GetAll())
        {
            bool changed = BookRentalManager.MarkRentalsReturnedForUser(book, userId);

            if (changed)
            {
                _books.Update(book);
            }
        }
    }
}
