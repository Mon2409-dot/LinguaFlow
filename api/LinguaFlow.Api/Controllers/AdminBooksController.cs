using LinguaFlow.Api.Data;
using LinguaFlow.Api.Dtos;
using LinguaFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/admin/books")]
[Authorize(Roles = "Admin")]
public class AdminBooksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Books.AsNoTracking()
        .OrderByDescending(b => b.Id)
        .Select(b => new
        {
            b.Id,
            b.Title,
            b.Author,
            b.Level,
            Language = b.Language.Code,
            Source = b.Source.Name,
            b.IsPublished,
            LessonCount = b.Lessons.Count
        }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(BookRequest req)
    {
        var error = await Validate(req);
        if (error is not null) return BadRequest(new { message = error });

        var b = new Book
        {
            SourceId = req.SourceId,
            LanguageId = req.LanguageId,
            Title = req.Title,
            Author = req.Author,
            Level = req.Level,
            CoverPath = req.CoverPath,
            SourceUrl = req.SourceUrl,
            IsPublished = req.IsPublished
        };
        db.Books.Add(b);
        await db.SaveChangesAsync();
        return Created($"/api/books/{b.Id}", new { b.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, BookRequest req)
    {
        var b = await db.Books.FindAsync(id);
        if (b is null) return NotFound();
        var error = await Validate(req);
        if (error is not null) return BadRequest(new { message = error });

        b.SourceId = req.SourceId; b.LanguageId = req.LanguageId; b.Title = req.Title; b.Author = req.Author;
        b.Level = req.Level; b.CoverPath = req.CoverPath; b.SourceUrl = req.SourceUrl; b.IsPublished = req.IsPublished;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var b = await db.Books.FindAsync(id);
        if (b is null) return NotFound();
        db.Books.Remove(b);   // các bài học của sách cũng bị xóa theo
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/lessons")]
    public async Task<IActionResult> AddLesson(int id, LessonRequest req)
    {
        if (!await db.Books.AnyAsync(b => b.Id == id)) return NotFound();
        var l = new Lesson { BookId = id, OrderNo = req.OrderNo, Title = req.Title, Content = req.Content };
        db.Lessons.Add(l);
        await db.SaveChangesAsync();
        return Created($"/api/lessons/{l.Id}", new { l.Id });
    }

    private async Task<string?> Validate(BookRequest req)
    {
        if (!await db.Sources.AnyAsync(s => s.Id == req.SourceId)) return "SourceId không tồn tại.";
        if (!await db.Languages.AnyAsync(l => l.Id == req.LanguageId)) return "LanguageId không tồn tại.";
        return null;
    }
}