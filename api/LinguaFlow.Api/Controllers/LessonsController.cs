using LinguaFlow.Api.Data;
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
}