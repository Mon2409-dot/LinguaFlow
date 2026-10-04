namespace LinguaFlow.Api.Models;

public class Language
{
    public int Id { get; set; }
    public string Code { get; set; } = "";        // en, ja, ru, de
    public string Name { get; set; } = "";
    public string LevelSystem { get; set; } = "CEFR";  // CEFR hoặc JLPT
}