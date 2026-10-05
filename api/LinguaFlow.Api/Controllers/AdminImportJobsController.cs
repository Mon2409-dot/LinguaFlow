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
[Route("api/admin/import-jobs")]
[Authorize(Roles = "Admin")]
public class AdminImportJobsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.ImportJobs.AsNoTracking()
        .OrderByDescending(j => j.Id).Take(50)
        .Select(j => new
        {
            j.Id,
            j.Status,
            j.Url,
            Source = j.Source.Name,
            Language = j.Language.Code,
            j.Level,
            j.BookId,
            j.CreatedAt,
            j.FinishedAt
        }).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var job = await db.ImportJobs.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.Url,
                Source = x.Source.Name,
                Language = x.Language.Code,
                x.Level,
                x.Title,
                x.Author,
                x.BookId,
                x.Log,
                x.CreatedAt,
                x.StartedAt,
                x.FinishedAt
            }).FirstOrDefaultAsync();
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ImportJobRequest req)
    {
        var source = await db.Sources.FirstOrDefaultAsync(s => s.Id == req.SourceId);
        if (source is null) return BadRequest(new { message = "SourceId không tồn tại." });
        if (!await db.Languages.AnyAsync(l => l.Id == req.LanguageId))
            return BadRequest(new { message = "LanguageId không tồn tại." });
        if (string.IsNullOrWhiteSpace(source.License))
            return BadRequest(new { message = "Nguồn chưa khai báo giấy phép." });
        if (!Uri.TryCreate(req.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return BadRequest(new { message = "URL phải là địa chỉ https hợp lệ." });
        if (!ContentImporter.HostAllowed(uri, source.BaseUrl))
            return BadRequest(new { message = "URL không thuộc tên miền của nguồn đã khai báo." });
        if (await db.ImportJobs.AnyAsync(j => j.Url == req.Url && j.Status != "Failed"))
            return Conflict(new { message = "URL này đã có job đang chờ, đang chạy hoặc đã hoàn tất." });

        var job = new ImportJob
        {
            SourceId = req.SourceId,
            LanguageId = req.LanguageId,
            Url = req.Url,
            Level = req.Level,
            Title = req.Title,
            Author = req.Author,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();
        return Created($"/api/admin/import-jobs/{job.Id}", new { job.Id, job.Status });
    }

    [HttpPost("{id:int}/retry")]
    public async Task<IActionResult> Retry(int id)
    {
        var job = await db.ImportJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();
        if (job.Status != "Failed") return BadRequest(new { message = "Chỉ thử lại được job đã lỗi." });
        job.Status = "Pending"; job.Log = null; job.StartedAt = null; job.FinishedAt = null;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var job = await db.ImportJobs.FirstOrDefaultAsync(j => j.Id == id);
        if (job is null) return NotFound();
        if (job.Status != "Completed" || job.BookId is null)
            return BadRequest(new { message = "Chỉ duyệt được job đã hoàn tất." });

        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == job.BookId);
        if (book is null) return NotFound(new { message = "Sách đã bị xóa." });
        book.IsPublished = true;
        await db.SaveChangesAsync();
        return NoContent();
    }
}