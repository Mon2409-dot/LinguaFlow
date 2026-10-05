using System.ComponentModel.DataAnnotations;

namespace LinguaFlow.Api.Dtos;

public class VocabRequest
{
    [Range(1, int.MaxValue)] public int LanguageId { get; set; }
    public int? LessonId { get; set; }
    [Required, MaxLength(200)] public string Word { get; set; } = "";
    [MaxLength(200)] public string? Reading { get; set; }
    [Required, MaxLength(500)] public string Meaning { get; set; } = "";
    [MaxLength(1000)] public string? Example { get; set; }
    [MaxLength(200)] public string? Extra { get; set; }
}

public class AddVocabRequest { [Range(1, int.MaxValue)] public int VocabularyId { get; set; } }
public class ReviewRequest { public bool Correct { get; set; } }
public class LessonProgressRequest { public bool Completed { get; set; } }

public record VocabItem(int Id, int? LessonId, string LanguageCode, string Word,
    string? Reading, string Meaning, string? Example, string? Extra);

public record MyVocabItem(int VocabularyId, string LanguageCode, string Word,
    string? Reading, string Meaning, string? Example, string? Extra,
    int Box, int ReviewCount, DateTime NextReviewAt);