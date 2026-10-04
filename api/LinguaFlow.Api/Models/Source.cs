namespace LinguaFlow.Api.Models;

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string License { get; set; } = "";
    public string? Attribution { get; set; }
}
