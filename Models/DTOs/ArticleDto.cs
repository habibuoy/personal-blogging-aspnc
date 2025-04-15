using System.ComponentModel;

namespace PersonalBlogging.Models.Dto;

public class ArticleDto
{
    public int Id { get; set; }
    public DateTime? PublishedDate { get; set; }
    [DisplayName("Last Update")]
    public DateTime? LastModifiedDate { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public IEnumerable<string>? Tags;
}

public static class ArticleDtoExtensions
{
    public static ArticleDto ToDto(this Article article)
    {
        return new ArticleDto
        {
            Id = article.Id,
            PublishedDate = article.PublishedDate,
            LastModifiedDate = article.LastModifiedDate,
            AuthorName = article.AuthorName,
            Title = article.Title,
            Content = article.Content,
            Tags = article.Tags?.Select(tag => tag.Name)
        };
    }
}