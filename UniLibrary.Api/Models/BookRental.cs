namespace UniLibrary.Api.Models;

public class BookRental
{
    public int RentalId { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ReaderGroup { get; set; }
    public string? Note { get; set; }
    public DateTime RentedAt { get; set; } = DateTime.UtcNow;
    public DateTime DueAt { get; set; } = DateTime.UtcNow.AddDays(14);
    public DateTime? ReturnConfirmedAt { get; set; }
    public int? IssuedByUserId { get; set; }
    public string? IssuedByFullName { get; set; }
    public int? ReturnedByUserId { get; set; }
    public string? ReturnedByFullName { get; set; }
    public string? ReturnNote { get; set; }
    public string Status { get; set; } = RentalStatuses.Active;
}

public static class RentalStatuses
{
    public const string Active = "Active";
    public const string Returned = "Returned";
}
