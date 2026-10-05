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
    public DbSet<Vocabulary> Vocabularies => Set<Vocabulary>();
    public DbSet<UserVocabulary> UserVocabularies => Set<UserVocabulary>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();
    public DbSet<StudyActivity> StudyActivities => Set<StudyActivity>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);   // bắt buộc có dòng này khi dùng Identity

        b.Entity<Language>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Language>().HasData(
            new Language { Id = 1, Code = "en", Name = "Tiếng Anh", LevelSystem = "CEFR" },
            new Language { Id = 2, Code = "ja", Name = "Tiếng Nhật", LevelSystem = "JLPT" },
            new Language { Id = 3, Code = "ru", Name = "Tiếng Nga", LevelSystem = "CEFR" },
            new Language { Id = 4, Code = "de", Name = "Tiếng Đức", LevelSystem = "CEFR" });

        b.Entity<Vocabulary>(e =>
        {
            e.Property(v => v.Word).HasMaxLength(200);
            e.HasIndex(v => new { v.LanguageId, v.Word });
            e.HasOne(v => v.Language).WithMany().HasForeignKey(v => v.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Lesson).WithMany().HasForeignKey(v => v.LessonId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<UserVocabulary>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.VocabularyId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.NextReviewAt });
        });

        b.Entity<LessonProgress>().HasIndex(x => new { x.UserId, x.LessonId }).IsUnique();

        b.Entity<StudyActivity>(e =>
        {
            e.HasIndex(x => new { x.UserId, x.Day }).IsUnique();
            e.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<ImportJob>(e =>
        {
            e.Property(x => x.Url).HasMaxLength(1000);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Language).WithMany().HasForeignKey(x => x.LanguageId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Book).WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}