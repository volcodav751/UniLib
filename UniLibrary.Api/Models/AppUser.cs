using LiteDB;

namespace UniLibrary.Api.Models
{
    public class AppUser
    {
        [BsonId]
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Student";

        public bool IsApproved { get; set; } = true;

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedByUserId { get; set; }

        public string? ApprovedByFullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
