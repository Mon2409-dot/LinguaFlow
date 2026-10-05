using LinguaFlow.Api.Data;
using LinguaFlow.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/lessons")]
public class LessonsController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var lesson = await db.Lessons.AsNoTracking()
            .Where(l => l.Id == id && l.Book.IsPublished)
            .Select(l => new { l.Id, l.BookId, l.OrderNo, l.Title, l.Content })
            .FirstOrDefaultAsync();

        return lesson is null ? NotFound() : Ok(lesson);
    }

    [HttpGet("{id:int}/vocab")]
    public async Task<IActionResult> Vocab(int id)
    {
        var items = await db.Vocabularies.AsNoTracking()
            .Where(v => v.LessonId == id && v.Lesson!.Book.IsPublished)
            .OrderBy(v => v.Id)
            .Select(v => new VocabItem(v.Id, v.LessonId, v.Language.Code, v.Word,
                v.Reading, v.Meaning, v.Example, v.Extra))
            .ToListAsync();
        return Ok(items);
    }
}