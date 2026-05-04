namespace FormBuilder.Backend.Models;

public class HelpArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = "guides";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}