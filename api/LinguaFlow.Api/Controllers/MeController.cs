using System.Security.Claims;
using LinguaFlow.Api.Data;
using LinguaFlow.Api.Dtos;
using LinguaFlow.Api.Models;
using LinguaFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(AppDbContext db, ActivityService activity) : ControllerBase
{
    // Hộp Leitner 0..5: số ngày chờ trước lần ôn kế tiếp khi trả lời đúng
    private static readonly int[] IntervalDays = [0, 1, 3, 7, 14, 30];

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("vocab")]
    public async Task<ActionResult<PagedResult<MyVocabItem>>> MyVocab(
        string? lang, bool dueOnly = false, int page = 1, int pageSize = 50)
    {
        var uid = UserId;
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var now = DateTime.UtcNow;

        var query = db.UserVocabularies.AsNoTracking().Where(x => x.UserId == uid);
        if (!string.IsNullOrWhiteSpace(lang)) query = query.Where(x => x.Vocabulary.Language.Code == lang);
        if (dueOnly) query = query.Where(x => x.NextReviewAt <= now);

        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.NextReviewAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new MyVocabItem(x.VocabularyId, x.Vocabulary.Language.Code, x.Vocabulary.Word,
                x.Vocabulary.Reading, x.Vocabulary.Meaning, x.Vocabulary.Example, x.Vocabulary.Extra,
                x.Box, x.ReviewCount, x.NextReviewAt))
            .ToListAsync();

        return new PagedResult<MyVocabItem>(items, total, page, pageSize);
    }

    [HttpPost("vocab")]
    public async Task<IActionResult> AddVocab(AddVocabRequest req)
    {
        var uid = UserId;
        if (!await db.Vocabularies.AnyAsync(v => v.Id == req.VocabularyId))
            return NotFound(new { message = "Từ vựng không tồn tại." });

        if (await db.UserVocabularies.AnyAsync(x => x.UserId == uid && x.VocabularyId == req.VocabularyId))
            return NoContent();   // đã có trong sổ từ

        db.UserVocabularies.Add(new UserVocabulary { UserId = uid, VocabularyId = req.VocabularyId });
        await db.SaveChangesAsync();
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpDelete("vocab/{vocabularyId:int}")]
    public async Task<IActionResult> RemoveVocab(int vocabularyId)
    {
        var uid = UserId;
        var row = await db.UserVocabularies
            .FirstOrDefaultAsync(x => x.UserId == uid && x.VocabularyId == vocabularyId);
        if (row is null) return NotFound();
        db.UserVocabularies.Remove(row);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("vocab/review")]
    public async Task<ActionResult<List<MyVocabItem>>> ReviewQueue(string? lang, int limit = 20)
    {
        var uid = UserId;
        limit = Math.Clamp(limit, 1, 100);
        var now = DateTime.UtcNow;

        var query = db.UserVocabularies.AsNoTracking()
            .Where(x => x.UserId == uid && x.NextReviewAt <= now);
        if (!string.IsNullOrWhiteSpace(lang)) query = query.Where(x => x.Vocabulary.Language.Code == lang);

        return await query.OrderBy(x => x.NextReviewAt).Take(limit)
            .Select(x => new MyVocabItem(x.VocabularyId, x.Vocabulary.Language.Code, x.Vocabulary.Word,
                x.Vocabulary.Reading, x.Vocabulary.Meaning, x.Vocabulary.Example, x.Vocabulary.Extra,
                x.Box, x.ReviewCount, x.NextReviewAt))
            .ToListAsync();
    }

    [HttpPost("vocab/{vocabularyId:int}/review")]
    public async Task<IActionResult> Review(int vocabularyId, ReviewRequest req)
    {
        var uid = UserId;
        var row = await db.UserVocabularies
            .FirstOrDefaultAsync(x => x.UserId == uid && x.VocabularyId == vocabularyId);
        if (row is null) return NotFound();

        var now = DateTime.UtcNow;
        if (req.Correct)
        {
            row.Box = Math.Min(row.Box + 1, IntervalDays.Length - 1);
            row.NextReviewAt = now.AddDays(IntervalDays[row.Box]);
        }
        else
        {
            row.Box = 0;
            row.NextReviewAt = now.AddMinutes(10);
        }
        row.ReviewCount++;
        row.LastReviewedAt = now;

        await activity.RecordAsync(uid);
        await db.SaveChangesAsync();
        return Ok(new { row.Box, row.NextReviewAt });
    }

    [HttpPut("progress/lessons/{lessonId:int}")]
    public async Task<IActionResult> SaveLessonProgress(int lessonId, LessonProgressRequest req)
    {
        var uid = UserId;
        if (!await db.Lessons.AnyAsync(l => l.Id == lessonId && l.Book.IsPublished)) return NotFound();

        var now = DateTime.UtcNow;
        var row = await db.LessonProgresses.FirstOrDefaultAsync(x => x.UserId == uid && x.LessonId == lessonId);
        if (row is null)
        {
            row = new LessonProgress { UserId = uid, LessonId = lessonId };
            db.LessonProgresses.Add(row);
        }
        row.LastAccessAt = now;
        if (req.Completed && !row.Completed)
        {
            row.Completed = true;
            row.CompletedAt = now;
        }

        await activity.RecordAsync(uid);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("progress")]
    public async Task<IActionResult> Progress(string? lang)
    {
        var uid = UserId;
        var now = DateTime.UtcNow;

        var completedLessons = await db.LessonProgresses.CountAsync(x => x.UserId == uid && x.Completed);
        var words = db.UserVocabularies.Where(x => x.UserId == uid);
        var totalWords = await words.CountAsync();
        var dueWords = await words.CountAsync(x => x.NextReviewAt <= now);
        var masteredWords = await words.CountAsync(x => x.Box >= 4);
        var streak = await activity.CurrentStreakAsync(uid);

        var recent = db.LessonProgresses.AsNoTracking().Where(x => x.UserId == uid);
        if (!string.IsNullOrWhiteSpace(lang)) recent = recent.Where(x => x.Lesson.Book.Language.Code == lang);
        var continueLearning = await recent.OrderByDescending(x => x.LastAccessAt).Take(5)
            .Select(x => new
            {
                lessonId = x.LessonId,
                lessonTitle = x.Lesson.Title,
                bookId = x.Lesson.BookId,
                bookTitle = x.Lesson.Book.Title,
                language = x.Lesson.Book.Language.Code,
                completed = x.Completed,
                lastAccessAt = x.LastAccessAt
            }).ToListAsync();

        return Ok(new
        {
            streakDays = streak,
            completedLessons,
            words = new { total = totalWords, due = dueWords, mastered = masteredWords },
            continueLearning
        });
    }
}