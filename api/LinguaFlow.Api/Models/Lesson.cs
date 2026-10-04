namespace LinguaFlow.Api.Models;

public class Lesson
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int OrderNo { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}