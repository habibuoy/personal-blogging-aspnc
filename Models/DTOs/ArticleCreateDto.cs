namespace PersonalBlogging.Models.Dto;

public class ArticleCreateDto
{
    public string AuthorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? TagsString { get; set; }
}

public static class ArticleCreateDtoExtensions
{
    public static Article ToArticle(this ArticleCreateDto articleCreateDto)
    {
        return new Article
        {
            PublishedDate = DateTime.Now,
            LastModifiedDate = DateTime.Now,
            AuthorName = articleCreateDto.AuthorName,
            Title = articleCreateDto.Title,
            Content = articleCreateDto.Content,
            TagsString = articleCreateDto.TagsString!,
        };
    }
}