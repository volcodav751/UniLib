using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Extensions;
using UniLibrary.Api.Models;
using UniLibrary.Api.Models.Requests;
using UniLibrary.Api.Models.Responses;
using UniLibrary.Api.Services.Books;
using UniLibrary.Api.Services.Files;
using UniLibrary.Api.Services.Results;

namespace UniLibrary.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;
    private readonly IBookFileService _bookFileService;

    public BooksController(IBookService bookService, IBookFileService bookFileService)
    {
        _bookService = bookService;
        _bookFileService = bookFileService;
    }

    [HttpGet]
    public ActionResult<List<Book>> GetAll()
    {
        return Ok(_bookService.GetAll());
    }

    [HttpGet("{id:int}")]
    public ActionResult<Book> GetById(int id)
    {
        return this.ToActionResult(_bookService.GetById(id));
    }

    [Authorize(Roles = UserRoles.TeacherOrAdmin)]
    [HttpGet("rentals/active")]
    public ActionResult<List<Book>> GetActiveRentals()
    {
        return Ok(_bookService.GetBooksWithActiveRentals());
    }

    [Authorize(Roles = UserRoles.TeacherOrAdmin)]
    [HttpPost]
    public ActionResult<Book> Create([FromBody] CreateBookRequest request)
    {
        ServiceResult<Book> result = _bookService.Create(request);

        if (result.Status == ServiceResultStatus.Created && result.Value is not null)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }

        return this.ToActionResult(result);
    }

    [Authorize(Roles = UserRoles.TeacherOrAdmin)]
    [HttpPut("{id:int}")]
    public ActionResult<Book> Update(int id, [FromBody] CreateBookRequest request)
    {
        return this.ToActionResult(_bookService.Update(id, request));
    }

    [Authorize(Roles = UserRoles.TeacherOrAdmin)]
    [HttpPost("{id:int}/staff-rent")]
    public ActionResult<Book> StaffRentBook(int id, [FromBody] StaffCreateRentalRequest request)
    {
        return this.ToActionResult(_bookService.StaffRentBook(id, request));
    }

    [Authorize(Roles = UserRoles.TeacherOrAdmin)]
    [HttpPost("{id:int}/rentals/{rentalId:int}/staff-return")]
    public ActionResult<Book> StaffReturnBook(int id, int rentalId, [FromBody] StaffReturnRentalRequest? request = null)
    {
        return this.ToActionResult(_bookService.StaffReturnBook(id, rentalId, request));
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        return this.ToActionResult(_bookService.Delete(id));
    }

    [Authorize(Roles = UserRoles.TeacherOrAdmin)]
    [HttpPost("{id:int}/file")]
    public async Task<ActionResult<BookFileUploadResponse>> UploadFile(int id, [FromForm] UploadBookFileRequest request)
    {
        ServiceResult<BookFileUploadResponse> result = await _bookFileService.UploadFileAsync(id, request);
        return this.ToActionResult(result);
    }

    [HttpGet("{id:int}/file")]
    public IActionResult DownloadFile(int id)
    {
        ServiceResult<BookFileDownloadResult> result = _bookFileService.DownloadFile(id);
        return ToFileActionResult(result);
    }

    [Authorize(Roles = UserRoles.TeacherOrAdmin)]
    [HttpDelete("{id:int}/file")]
    public IActionResult DeleteFile(int id)
    {
        return this.ToActionResult(_bookFileService.DeleteFile(id));
    }

    [HttpGet("{id:int}/preview")]
    public IActionResult PreviewFile(int id)
    {
        ServiceResult<BookFileDownloadResult> result = _bookFileService.PreviewFile(id);
        return ToFileActionResult(result);
    }

    private IActionResult ToFileActionResult(ServiceResult<BookFileDownloadResult> result)
    {
        if (result.Status != ServiceResultStatus.Ok || result.Value is null)
        {
            return this.ToIActionResult(result);
        }

        if (result.Value.Inline)
        {
            Response.Headers["Content-Disposition"] = BuildInlineContentDisposition(result.Value.FileName);
        }

        return File(result.Value.Stream, result.Value.ContentType, result.Value.Inline ? null : result.Value.FileName);
    }

    private static string BuildInlineContentDisposition(string fileName)
    {
        string safeFileName = SanitizeHeaderFileName(fileName);
        string encodedFileName = Uri.EscapeDataString(Path.GetFileName(fileName));

        return $"inline; filename=\"{safeFileName}\"; filename*=UTF-8''{encodedFileName}";
    }

    private static string SanitizeHeaderFileName(string fileName)
    {
        string fileNameOnly = Path.GetFileName(fileName);

        if (string.IsNullOrWhiteSpace(fileNameOnly))
        {
            return "file";
        }

        var builder = new System.Text.StringBuilder(fileNameOnly.Length);

        foreach (char ch in fileNameOnly)
        {
            bool isUnsafe = ch <= 31 || ch == 127 || ch > 126 || ch == '\\' || ch == '"' || ch == ';';
            builder.Append(isUnsafe ? '_' : ch);
        }

        string sanitized = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}
