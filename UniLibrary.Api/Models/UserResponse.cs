namespace UniLibrary.Api.Models
{
    public class UserResponse
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = true;

        public bool IsApprovalRequired { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string ApprovedByFullName { get; set; } = string.Empty;
    }
}
