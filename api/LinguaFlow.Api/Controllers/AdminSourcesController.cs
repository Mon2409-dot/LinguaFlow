using LinguaFlow.Api.Data;
using LinguaFlow.Api.Dtos;
using LinguaFlow.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/admin/sources")]
[Authorize(Roles = "Admin")]
public class AdminSourcesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() =>
        Ok(await db.Sources.AsNoTracking().OrderBy(s => s.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(SourceRequest req)
    {
        var s = new Source { Name = req.Name, BaseUrl = req.BaseUrl, License = req.License, Attribution = req.Attribution };
        db.Sources.Add(s);
        await db.SaveChangesAsync();
        return Created($"/api/admin/sources/{s.Id}", s);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SourceRequest req)
    {
        var s = await db.Sources.FindAsync(id);
        if (s is null) return NotFound();
        s.Name = req.Name; s.BaseUrl = req.BaseUrl; s.License = req.License; s.Attribution = req.Attribution;
        await db.SaveChangesAsync();
        return Ok(s);
    }
}