using Microsoft.AspNetCore.Mvc;
using UniLibrary.Api.Data;
using UniLibrary.Api.Models;

namespace UniLibrary.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly LiteDbContext _context;

        public BooksController(LiteDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<List<Book>> GetAll()
        {
            return Ok(_context.Books.FindAll().ToList());
        }

        [HttpGet("{id:int}")]
        public ActionResult<Book> GetById(int id)
        {
            var book = _context.Books.FindById(id);

            if (book == null)
                return NotFound();

            return Ok(book);
        }

        [HttpPost]
        public ActionResult<Book> Create([FromBody] Book book)
        {
            _context.Books.Insert(book);
            return Ok(book);
        }
    }
}