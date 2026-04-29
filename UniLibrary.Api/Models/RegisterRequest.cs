using System.ComponentModel.DataAnnotations;

namespace UniLibrary.Api.Models
{
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Введіть ім'я")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть email")]
        [EmailAddress(ErrorMessage = "Невірний формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть пароль")]
        [MinLength(6, ErrorMessage = "Пароль має містити мінімум 6 символів")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Student";
    }
}