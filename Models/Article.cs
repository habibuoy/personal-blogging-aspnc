namespace PersonalBlogging.Models;

public class Article
{
    public int Id { get; set; }
    public DateTime? PublishedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string Title { get; set; }
    public string AuthorName { get; set; }
    public string? Content { get; set; }
    public string[]? Tags { get; set; }
}