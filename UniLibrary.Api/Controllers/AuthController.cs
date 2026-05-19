using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Extensions;
using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Services.Auth;

namespace UniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public ActionResult<AuthResponse> Register(RegisterRequest request)
        {
            return this.ToActionResult(_authService.Register(request));
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public ActionResult<AuthResponse> Login(LoginRequest request)
        {
            return this.ToActionResult(_authService.Login(request));
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<UserResponse> Me()
        {
            return this.ToActionResult(_authService.GetCurrentUser());
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpGet("users")]
        public ActionResult<List<UserResponse>> GetUsers()
        {
            return Ok(_authService.GetUsers());
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpPut("users/{id:int}/approve-teacher")]
        public ActionResult<UserResponse> ApproveTeacher(int id)
        {
            return this.ToActionResult(_authService.ApproveTeacher(id));
        }

        [Authorize(Roles = UserRoles.Admin)]
        [HttpDelete("users/{id:int}")]
        public IActionResult DeleteUser(int id)
        {
            return this.ToActionResult(_authService.DeleteUser(id));
        }
    }
}
