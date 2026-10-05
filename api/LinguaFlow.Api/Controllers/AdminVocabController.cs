using LinguaFlow.Api.Data;
using LinguaFlow.Api.Dtos;
using LinguaFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/admin/vocab")]
[Authorize(Roles = "Admin")]
public class AdminVocabController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<VocabItem>>> List(
        string? lang, string? q, int page = 1, int pageSize = 50)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Vocabularies.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(lang)) query = query.Where(v => v.Language.Code == lang);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(v => v.Word.Contains(q) || v.Meaning.Contains(q));

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(v => v.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new VocabItem(v.Id, v.LessonId, v.Language.Code, v.Word,
                v.Reading, v.Meaning, v.Example, v.Extra))
            .ToListAsync();

        return new PagedResult<VocabItem>(items, total, page, pageSize);
    }

    [HttpPost]
    public async Task<IActionResult> Create(VocabRequest req)
    {
        var error = await Validate(req);
        if (error is not null) return BadRequest(new { message = error });

        var v = new Vocabulary
        {
            LanguageId = req.LanguageId,
            LessonId = req.LessonId,
            Word = req.Word,
            Reading = req.Reading,
            Meaning = req.Meaning,
            Example = req.Example,
            Extra = req.Extra
        };
        db.Vocabularies.Add(v);
        await db.SaveChangesAsync();
        return Created($"/api/admin/vocab/{v.Id}", new { v.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, VocabRequest req)
    {
        var v = await db.Vocabularies.FindAsync(id);
        if (v is null) return NotFound();
        var error = await Validate(req);
        if (error is not null) return BadRequest(new { message = error });

        v.LanguageId = req.LanguageId; v.LessonId = req.LessonId; v.Word = req.Word;
        v.Reading = req.Reading; v.Meaning = req.Meaning; v.Example = req.Example; v.Extra = req.Extra;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var v = await db.Vocabularies.FindAsync(id);
        if (v is null) return NotFound();
        db.Vocabularies.Remove(v);   // từ này cũng biến mất khỏi sổ từ của người học
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string?> Validate(VocabRequest req)
    {
        if (!await db.Languages.AnyAsync(l => l.Id == req.LanguageId)) return "LanguageId không tồn tại.";
        if (req.LessonId is int lid)
        {
            var lessonLang = await db.Lessons.Where(l => l.Id == lid)
                .Select(l => (int?)l.Book.LanguageId).FirstOrDefaultAsync();
            if (lessonLang is null) return "LessonId không tồn tại.";
            if (lessonLang != req.LanguageId) return "Bài học thuộc ngôn ngữ khác với từ vựng.";
        }
        return null;
    }
}