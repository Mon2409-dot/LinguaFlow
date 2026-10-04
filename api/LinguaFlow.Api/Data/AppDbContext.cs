using LinguaFlow.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LinguaFlow.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Lesson> Lessons => Set<Lesson>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);   // bắt buộc có dòng này khi dùng Identity

        b.Entity<Language>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Language>().HasData(
            new Language { Id = 1, Code = "en", Name = "Tiếng Anh", LevelSystem = "CEFR" },
            new Language { Id = 2, Code = "ja", Name = "Tiếng Nhật", LevelSystem = "JLPT" },
            new Language { Id = 3, Code = "ru", Name = "Tiếng Nga", LevelSystem = "CEFR" },
            new Language { Id = 4, Code = "de", Name = "Tiếng Đức", LevelSystem = "CEFR" });
    }
}