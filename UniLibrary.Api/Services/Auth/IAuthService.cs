using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Services.Auth;

public interface IAuthService
{
    ServiceResult<AuthResponse> Register(RegisterRequest request);
    ServiceResult<AuthResponse> Login(LoginRequest request);
    ServiceResult<UserResponse> GetCurrentUser();
    List<UserResponse> GetUsers();
    ServiceResult<UserResponse> ApproveTeacher(int id);
    ServiceResult DeleteUser(int id);
}
