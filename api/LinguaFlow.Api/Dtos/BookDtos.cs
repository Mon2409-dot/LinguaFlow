using System.ComponentModel.DataAnnotations;

namespace LinguaFlow.Api.Dtos;

public class SourceRequest
{
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required, MaxLength(500)] public string BaseUrl { get; set; } = "";
    [Required, MaxLength(100)] public string License { get; set; } = "";
    [MaxLength(500)] public string? Attribution { get; set; }
}

public class BookRequest
{
    [Range(1, int.MaxValue)] public int SourceId { get; set; }
    [Range(1, int.MaxValue)] public int LanguageId { get; set; }
    [Required, MaxLength(300)] public string Title { get; set; } = "";
    [MaxLength(200)] public string? Author { get; set; }
    [Required, MaxLength(10)] public string Level { get; set; } = "";
    [MaxLength(500)] public string? CoverPath { get; set; }
    [MaxLength(1000)] public string? SourceUrl { get; set; }
    public bool IsPublished { get; set; }
}

public class LessonRequest
{
    [Range(1, int.MaxValue)] public int OrderNo { get; set; }
    [Required, MaxLength(300)] public string Title { get; set; } = "";
    [Required] public string Content { get; set; } = "";
}

public record BookSummary(int Id, string Title, string? Author, string Level, string LanguageCode, string? CoverPath);
public record LessonSummary(int Id, int OrderNo, string Title);
public record BookDetail(int Id, string Title, string? Author, string Level, string LanguageCode, string? CoverPath,
    string SourceName, string License, string? Attribution, string? SourceUrl, List<LessonSummary> Lessons);
public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);