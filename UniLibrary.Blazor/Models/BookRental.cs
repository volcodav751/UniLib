namespace UniLibrary.Blazor.Models;

public class BookRental
{
    public int RentalId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RentedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnRequestedAt { get; set; }
    public DateTime? ReturnConfirmedAt { get; set; }
    public int? ConfirmedByUserId { get; set; }
    public string Status { get; set; } = RentalStatuses.Active;
}

public static class RentalStatuses
{
    public const string Active = "Active";
    public const string ReturnPending = "ReturnPending";
    public const string Returned = "Returned";
}
