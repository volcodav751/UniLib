using LiteDB;
using UniLibrary.Api.Data;
using UniLibrary.Api.Models;
using UniLibrary.Api.Interfaces;

namespace UniLibrary.Api.Repos;

public class BookRepository : IBookRepository
{
    private readonly LiteDbContext _context;

    public BookRepository(LiteDbContext context)
    {
        _context = context;
    }

    public List<Book> GetAll()
    {
        return _context.BookDocuments.FindAll()
            .Select(MapDocumentToBook)
            .ToList();
    }

    public Book? GetById(int id)
    {
        BsonDocument? document = _context.BookDocuments.FindById(id);
        return document is null ? null : MapDocumentToBook(document);
    }

    public int GetNextId()
    {
        int maxId = _context.BookDocuments.FindAll()
            .Select(document => ReadInt(document, "_id", ReadInt(document, "Id", 0)))
            .DefaultIfEmpty(0)
            .Max();

        return maxId + 1;
    }

    public void Add(Book book)
    {
        NormalizeBookShape(book);
        _context.Books.Insert(book);
    }

    public void Update(Book book)
    {
        NormalizeBookShape(book);
        _context.Books.Update(book);
    }

    public bool Delete(int id)
    {
        return _context.Books.Delete(id);
    }

    private static Book MapDocumentToBook(BsonDocument document)
    {
        BsonDocument? info = ReadDocument(document, "Info");
        BsonDocument? inventory = ReadDocument(document, "Inventory");
        BsonDocument? file = ReadDocument(document, "File");
        BsonDocument? preview = ReadDocument(document, "Preview");

        bool isDigital = ReadBool(inventory, "IsDigital", ReadBool(document, "IsDigital", false));
        int totalCopies = isDigital ? 0 : ReadInt(inventory, "TotalCopies", ReadInt(document, "TotalCopies", 1));
        bool legacyIsAvailable = ReadBool(document, "IsAvailable", true);
        int availableCopies = isDigital
            ? 0
            : ReadInt(inventory, "AvailableCopies", ReadInt(document, "AvailableCopies", legacyIsAvailable ? totalCopies : 0));

        Book book = new()
        {
            Id = ReadInt(document, "_id", ReadInt(document, "Id", 0)),
            Info = new BookInfo
            {
                Title = ReadString(info, "Title", ReadString(document, "Title", string.Empty)),
                Author = ReadString(info, "Author", ReadString(document, "Author", string.Empty)),
                Description = ReadString(info, "Description", ReadString(document, "Description", string.Empty)),
                Category = ReadString(info, "Category", ReadString(document, "Category", string.Empty)),
                Language = ReadString(info, "Language", ReadString(document, "Language", string.Empty)),
                Publisher = ReadString(info, "Publisher", ReadString(document, "Publisher", string.Empty)),
                PublicationYear = ReadInt(info, "PublicationYear", ReadInt(document, "PublicationYear", 0)),
                PageCount = ReadInt(info, "PageCount", ReadInt(document, "PageCount", 0)),
                Isbn = ReadString(info, "Isbn", ReadString(document, "Isbn", string.Empty))
            },
            Inventory = new BookInventory
            {
                IsDigital = isDigital,
                TotalCopies = Math.Max(0, totalCopies),
                AvailableCopies = Math.Max(0, availableCopies)
            },
            Tags = ReadStringList(document, "Tags"),
            Rentals = ReadRentals(document),
            CoverImageUrl = ReadNullableString(document, "CoverImageUrl"),
            CreatedAt = ReadDate(document, "CreatedAt", DateTime.UtcNow),
            UpdatedAt = ReadDate(document, "UpdatedAt", DateTime.UtcNow)
        };

        book.File = MapFileAttachment(file, document);
        book.Preview = MapPreviewAttachment(preview, document);

        NormalizeBookShape(book);
        return book;
    }

    private static BookFileAttachment? MapFileAttachment(BsonDocument? file, BsonDocument document)
    {
        string? fileId = ReadNullableString(file, "FileId") ?? ReadNullableString(document, "FileId");

        if (string.IsNullOrWhiteSpace(fileId))
        {
            return null;
        }

        return new BookFileAttachment
        {
            FileId = fileId,
            OriginalFileName = ReadString(file, "OriginalFileName", ReadString(document, "OriginalFileName", string.Empty)),
            StoredFileName = ReadString(file, "StoredFileName", ReadString(document, "StoredFileName", string.Empty)),
            ContentType = ReadString(file, "ContentType", ReadString(document, "ContentType", "application/octet-stream")),
            FileSizeBytes = ReadLong(file, "FileSizeBytes", ReadLong(document, "FileSizeBytes", 0)),
            UploadedAt = ReadDate(file, "UploadedAt", ReadDate(document, "FileUploadedAt", DateTime.UtcNow))
        };
    }

    private static BookPreviewAttachment? MapPreviewAttachment(BsonDocument? preview, BsonDocument document)
    {
        string? previewFileId = ReadNullableString(preview, "FileId") ?? ReadNullableString(document, "PreviewFileId");
        string? previewStatus = ReadNullableString(preview, "Status") ?? ReadNullableString(document, "PreviewStatus");
        string? previewError = ReadNullableString(preview, "Error") ?? ReadNullableString(document, "PreviewError");

        if (string.IsNullOrWhiteSpace(previewFileId)
            && string.IsNullOrWhiteSpace(previewStatus)
            && string.IsNullOrWhiteSpace(previewError))
        {
            return null;
        }

        return new BookPreviewAttachment
        {
            FileId = previewFileId,
            FileName = ReadNullableString(preview, "FileName") ?? ReadNullableString(document, "PreviewFileName"),
            ContentType = ReadNullableString(preview, "ContentType") ?? ReadNullableString(document, "PreviewContentType"),
            GeneratedAt = ReadNullableDate(preview, "GeneratedAt") ?? ReadNullableDate(document, "PreviewGeneratedAt"),
            Status = previewStatus,
            Error = previewError
        };
    }

    private static List<BookRental> ReadRentals(BsonDocument document)
    {
        if (!TryRead(document, "Rentals", out BsonValue value) || !value.IsArray)
        {
            return [];
        }

        return value.AsArray
            .Where(item => item.IsDocument)
            .Select(item => MapRental(item.AsDocument))
            .ToList();
    }

    private static BookRental MapRental(BsonDocument document)
    {
        DateTime? returnConfirmedAt = ReadNullableDate(document, "ReturnConfirmedAt");
        string status = ReadString(document, "Status", returnConfirmedAt.HasValue ? RentalStatuses.Returned : RentalStatuses.Active);

        return new BookRental
        {
            RentalId = ReadInt(document, "RentalId", 0),
            UserId = ReadNullableInt(document, "UserId"),
            FullName = ReadString(document, "FullName", string.Empty),
            Email = ReadString(document, "Email", string.Empty),
            ReaderGroup = ReadNullableString(document, "ReaderGroup"),
            Note = ReadNullableString(document, "Note"),
            RentedAt = ReadDate(document, "RentedAt", DateTime.UtcNow),
            DueAt = ReadDate(document, "DueAt", DateTime.UtcNow.AddDays(14)),
            ReturnConfirmedAt = returnConfirmedAt,
            IssuedByUserId = ReadNullableInt(document, "IssuedByUserId"),
            IssuedByFullName = ReadNullableString(document, "IssuedByFullName"),
            ReturnedByUserId = ReadNullableInt(document, "ReturnedByUserId") ?? ReadNullableInt(document, "ConfirmedByUserId"),
            ReturnedByFullName = ReadNullableString(document, "ReturnedByFullName"),
            ReturnNote = ReadNullableString(document, "ReturnNote"),
            Status = string.IsNullOrWhiteSpace(status) ? RentalStatuses.Active : status
        };
    }

    private static void NormalizeBookShape(Book book)
    {
        book.Info ??= new BookInfo();
        book.Inventory ??= new BookInventory();
        book.Tags ??= [];
        book.Rentals ??= [];

        if (book.Inventory.IsDigital)
        {
            book.Inventory.TotalCopies = 0;
            book.Inventory.AvailableCopies = 0;
            return;
        }

        if (book.Inventory.TotalCopies < 1)
        {
            book.Inventory.TotalCopies = 1;
        }

        book.Inventory.AvailableCopies = Math.Clamp(book.Inventory.AvailableCopies, 0, book.Inventory.TotalCopies);
    }

    private static BsonDocument? ReadDocument(BsonDocument document, string key)
    {
        return TryRead(document, key, out BsonValue value) && value.IsDocument ? value.AsDocument : null;
    }

    private static string ReadString(BsonDocument? document, string key, string fallback)
    {
        return TryRead(document, key, out BsonValue value) && value.IsString ? value.AsString : fallback;
    }

    private static string? ReadNullableString(BsonDocument? document, string key)
    {
        return TryRead(document, key, out BsonValue value) && value.IsString ? value.AsString : null;
    }

    private static List<string> ReadStringList(BsonDocument document, string key)
    {
        if (!TryRead(document, key, out BsonValue value) || !value.IsArray)
        {
            return [];
        }

        return value.AsArray
            .Where(item => item.IsString && !string.IsNullOrWhiteSpace(item.AsString))
            .Select(item => item.AsString.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int ReadInt(BsonDocument? document, string key, int fallback)
    {
        if (!TryRead(document, key, out BsonValue value))
        {
            return fallback;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        if (value.IsInt64)
        {
            return (int)value.AsInt64;
        }

        if (value.IsDouble)
        {
            return (int)value.AsDouble;
        }

        return fallback;
    }

    private static int? ReadNullableInt(BsonDocument? document, string key)
    {
        if (!TryRead(document, key, out BsonValue value) || value.IsNull)
        {
            return null;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        if (value.IsInt64)
        {
            return (int)value.AsInt64;
        }

        if (value.IsDouble)
        {
            return (int)value.AsDouble;
        }

        return null;
    }

    private static long ReadLong(BsonDocument? document, string key, long fallback)
    {
        if (!TryRead(document, key, out BsonValue value))
        {
            return fallback;
        }

        if (value.IsInt64)
        {
            return value.AsInt64;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        if (value.IsDouble)
        {
            return (long)value.AsDouble;
        }

        return fallback;
    }

    private static bool ReadBool(BsonDocument? document, string key, bool fallback)
    {
        return TryRead(document, key, out BsonValue value) && value.IsBoolean ? value.AsBoolean : fallback;
    }

    private static DateTime ReadDate(BsonDocument? document, string key, DateTime fallback)
    {
        return ReadNullableDate(document, key) ?? fallback;
    }

    private static DateTime? ReadNullableDate(BsonDocument? document, string key)
    {
        if (!TryRead(document, key, out BsonValue value) || value.IsNull)
        {
            return null;
        }

        if (value.IsDateTime)
        {
            return value.AsDateTime;
        }

        if (value.IsString && DateTime.TryParse(value.AsString, out DateTime parsedDate))
        {
            return parsedDate;
        }

        return null;
    }

    private static bool TryRead(BsonDocument? document, string key, out BsonValue value)
    {
        if (document is not null && document.TryGetValue(key, out BsonValue foundValue))
        {
            value = foundValue;
            return true;
        }

        value = BsonValue.Null;
        return false;
    }
}
