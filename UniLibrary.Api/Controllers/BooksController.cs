using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Models;

namespace UniLibrary.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private static List<Book> books = new()
    {
        new Book
        {
            Id = 1,
            Title = "Clean Code",
            Author = "Robert Martin",
            Year = 2008,
            AvailableCopies = 3
        },
        new Book
        {
            Id = 2,
            Title = "Design Patterns",
            Author = "Erich Gamma",
            Year = 1994,
            AvailableCopies = 2
        }
    };

    [HttpGet]
    public ActionResult<List<Book>> GetBooks()
    {
        return Ok(books);
    }
}