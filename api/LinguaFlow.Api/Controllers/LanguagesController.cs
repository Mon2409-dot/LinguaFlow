using LinguaFlow.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguagesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await db.Languages.AsNoTracking()
        .OrderBy(l => l.Id)
        .Select(l => new { l.Id, l.Code, l.Name, l.LevelSystem })
        .ToListAsync());
}