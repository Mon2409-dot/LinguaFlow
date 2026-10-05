using System.ComponentModel.DataAnnotations;

namespace LinguaFlow.Api.Dtos;

public class ImportJobRequest
{
    [Range(1, int.MaxValue)] public int SourceId { get; set; }
    [Range(1, int.MaxValue)] public int LanguageId { get; set; }
    [Required, MaxLength(1000)] public string Url { get; set; } = "";
    [Required, MaxLength(10)] public string Level { get; set; } = "";
    [MaxLength(300)] public string? Title { get; set; }
    [MaxLength(200)] public string? Author { get; set; }
}