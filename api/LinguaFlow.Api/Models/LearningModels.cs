namespace LinguaFlow.Api.Models;

public class Vocabulary
{
    public int Id { get; set; }
    public int LanguageId { get; set; }
    public Language Language { get; set; } = null!;
    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public string Word { get; set; } = "";
    public string? Reading { get; set; }      // furigana, phiên âm
    public string Meaning { get; set; } = ""; // nghĩa tiếng Việt
    public string? Example { get; set; }
    public string? Extra { get; set; }        // giống từ (Đức), trọng âm (Nga)...
}

public class UserVocabulary
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public int VocabularyId { get; set; }
    public Vocabulary Vocabulary { get; set; } = null!;
    public int Box { get; set; }              // hộp Leitner 0..5
    public int ReviewCount { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime NextReviewAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReviewedAt { get; set; }
}

public class LessonProgress
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public bool Completed { get; set; }
    public DateTime LastAccessAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class StudyActivity
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public DateOnly Day { get; set; }
    public int Count { get; set; }
}