namespace LinguaFlow.Api.Models;

public class ImportJob
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public Source Source { get; set; } = null!;
    public int LanguageId { get; set; }
    public Language Language { get; set; } = null!;
    public string Url { get; set; } = "";
    public string Level { get; set; } = "";
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string Status { get; set; } = "Pending";   // Pending, Running, Completed, Failed
    public string? Log { get; set; }
    public int? BookId { get; set; }
    public Book? Book { get; set; }
    public string? ContentHash { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
}