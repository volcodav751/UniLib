using LiteDB;

namespace UniLibrary.Api.Models;

public partial class Book
{
    [BsonIgnore]
    public string Title
    {
        get => EnsureInfo().Title;
        set => EnsureInfo().Title = value ?? string.Empty;
    }

    [BsonIgnore]
    public string Author
    {
        get => EnsureInfo().Author;
        set => EnsureInfo().Author = value ?? string.Empty;
    }

    [BsonIgnore]
    public string Description
    {
        get => EnsureInfo().Description;
        set => EnsureInfo().Description = value ?? string.Empty;
    }

    [BsonIgnore]
    public string Category
    {
        get => EnsureInfo().Category;
        set => EnsureInfo().Category = value ?? string.Empty;
    }

    [BsonIgnore]
    public string Language
    {
        get => EnsureInfo().Language;
        set => EnsureInfo().Language = value ?? string.Empty;
    }

    [BsonIgnore]
    public string Publisher
    {
        get => EnsureInfo().Publisher;
        set => EnsureInfo().Publisher = value ?? string.Empty;
    }

    [BsonIgnore]
    public int PublicationYear
    {
        get => EnsureInfo().PublicationYear;
        set => EnsureInfo().PublicationYear = value;
    }

    [BsonIgnore]
    public int PageCount
    {
        get => EnsureInfo().PageCount;
        set => EnsureInfo().PageCount = value;
    }

    [BsonIgnore]
    public string Isbn
    {
        get => EnsureInfo().Isbn;
        set => EnsureInfo().Isbn = value ?? string.Empty;
    }

    [BsonIgnore]
    public bool IsDigital
    {
        get => EnsureInventory().IsDigital;
        set => EnsureInventory().IsDigital = value;
    }

    [BsonIgnore]
    public bool IsAvailable
    {
        get => EnsureInventory().IsAvailable;
        set
        {
            BookInventory inventory = EnsureInventory();

            if (inventory.IsDigital)
            {
                return;
            }

            if (!value)
            {
                inventory.AvailableCopies = 0;
            }
            else if (inventory.AvailableCopies <= 0)
            {
                inventory.AvailableCopies = Math.Max(1, inventory.TotalCopies);
            }
        }
    }

    [BsonIgnore]
    public int TotalCopies
    {
        get => EnsureInventory().TotalCopies;
        set => EnsureInventory().TotalCopies = value;
    }

    [BsonIgnore]
    public int AvailableCopies
    {
        get => EnsureInventory().AvailableCopies;
        set => EnsureInventory().AvailableCopies = value;
    }

    [BsonIgnore]
    public string? FileId
    {
        get => File?.FileId;
        set => EnsureFile().FileId = value ?? string.Empty;
    }

    [BsonIgnore]
    public string? OriginalFileName
    {
        get => File?.OriginalFileName;
        set => EnsureFile().OriginalFileName = value ?? string.Empty;
    }

    [BsonIgnore]
    public string? StoredFileName
    {
        get => File?.StoredFileName;
        set => EnsureFile().StoredFileName = value ?? string.Empty;
    }

    [BsonIgnore]
    public string? ContentType
    {
        get => File?.ContentType;
        set => EnsureFile().ContentType = value ?? string.Empty;
    }

    [BsonIgnore]
    public long? FileSizeBytes
    {
        get => File?.FileSizeBytes;
        set
        {
            if (value.HasValue)
            {
                EnsureFile().FileSizeBytes = value.Value;
            }
        }
    }

    [BsonIgnore]
    public DateTime? FileUploadedAt
    {
        get => File?.UploadedAt;
        set
        {
            if (value.HasValue)
            {
                EnsureFile().UploadedAt = value.Value;
            }
        }
    }

    [BsonIgnore]
    public string? PreviewFileId
    {
        get => Preview?.FileId;
        set => EnsurePreview().FileId = value;
    }

    [BsonIgnore]
    public string? PreviewFileName
    {
        get => Preview?.FileName;
        set => EnsurePreview().FileName = value;
    }

    [BsonIgnore]
    public string? PreviewContentType
    {
        get => Preview?.ContentType;
        set => EnsurePreview().ContentType = value;
    }

    [BsonIgnore]
    public DateTime? PreviewGeneratedAt
    {
        get => Preview?.GeneratedAt;
        set => EnsurePreview().GeneratedAt = value;
    }

    [BsonIgnore]
    public string? PreviewStatus
    {
        get => Preview?.Status;
        set => EnsurePreview().Status = value;
    }

    [BsonIgnore]
    public string? PreviewError
    {
        get => Preview?.Error;
        set => EnsurePreview().Error = value;
    }

    public void ClearFileInfo()
    {
        File = null;
        Preview = null;
    }

    private BookInfo EnsureInfo()
    {
        Info ??= new BookInfo();
        return Info;
    }

    private BookInventory EnsureInventory()
    {
        Inventory ??= new BookInventory();
        return Inventory;
    }

    private BookFileAttachment EnsureFile()
    {
        File ??= new BookFileAttachment();
        return File;
    }

    private BookPreviewAttachment EnsurePreview()
    {
        Preview ??= new BookPreviewAttachment();
        return Preview;
    }
}
