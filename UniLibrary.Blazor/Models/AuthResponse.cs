namespace UniLibrary.Blazor.Models
{
    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserResponse User { get; set; } = new();
        public bool RequiresApproval { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
