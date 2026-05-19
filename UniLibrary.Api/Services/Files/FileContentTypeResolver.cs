namespace UniLibrary.Api.Services.Files;

public static class FileContentTypeResolver
{
    public static readonly string[] AllowedExtensions = [".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt"];

    public static string Resolve(string extension, string? browserContentType)
    {
        if (!string.IsNullOrWhiteSpace(browserContentType)
            && browserContentType != "application/octet-stream")
        {
            return browserContentType;
        }

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            ".rtf" => "application/rtf",
            ".odt" => "application/vnd.oasis.opendocument.text",
            _ => "application/octet-stream"
        };
    }

    public static bool IsPdf(string? fileName, string? contentType)
    {
        return string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            || fileName?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true;
    }
}
