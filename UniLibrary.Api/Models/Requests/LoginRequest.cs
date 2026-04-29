using System.ComponentModel.DataAnnotations;

namespace UniLibrary.Api.Models.Requests
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль")]
        public string Password { get; set; } = string.Empty;
    }
}