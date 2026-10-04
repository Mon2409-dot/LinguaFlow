using LinguaFlow.Api.Data;
using LinguaFlow.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookSummary>>> List(
        string? lang, string? level, string? q, int page = 1, int pageSize = 12)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = db.Books.AsNoTracking().Where(b => b.IsPublished);
        if (!string.IsNullOrWhiteSpace(lang)) query = query.Where(b => b.Language.Code == lang);
        if (!string.IsNullOrWhiteSpace(level)) query = query.Where(b => b.Level == level);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(b => b.Title.Contains(q) || (b.Author != null && b.Author.Contains(q)));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.CreatedAt).ThenBy(b => b.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => new BookSummary(b.Id, b.Title, b.Author, b.Level, b.Language.Code, b.CoverPath))
            .ToListAsync();

        return new PagedResult<BookSummary>(items, total, page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDetail>> Get(int id)
    {
        var book = await db.Books.AsNoTracking()
            .Where(b => b.Id == id && b.IsPublished)
            .Select(b => new BookDetail(
                b.Id, b.Title, b.Author, b.Level, b.Language.Code, b.CoverPath,
                b.Source.Name, b.Source.License, b.Source.Attribution, b.SourceUrl,
                b.Lessons.OrderBy(l => l.OrderNo)
                    .Select(l => new LessonSummary(l.Id, l.OrderNo, l.Title)).ToList()))
            .FirstOrDefaultAsync();

        if (book is null) return NotFound();
        return book;
    }
}