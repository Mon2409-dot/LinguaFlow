namespace LinguaFlow.Api.Models;

public class Book
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public Source Source { get; set; } = null!;
    public int LanguageId { get; set; }
    public Language Language { get; set; } = null!;
    public string Title { get; set; } = "";
    public string? Author { get; set; }
    public string Level { get; set; } = "";       // A1..C2 hoặc N5..N1
    public string? CoverPath { get; set; }
    public string? SourceUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Lesson> Lessons { get; set; } = new();
}