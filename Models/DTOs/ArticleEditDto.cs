namespace PersonalBlogging.Models.Dto;

public class ArticleEditDto : ArticleCreateDto
{
    public int Id { get; set; }
}

public static class ArticleEditDtoExtensions
{
    public static Article ToArticle(this ArticleEditDto articleEditDto)
    {
        return new Article
        {
            Id = articleEditDto.Id,
            LastModifiedDate = DateTime.Now,
            AuthorName = articleEditDto.AuthorName,
            Title = articleEditDto.Title,
            Content = articleEditDto.Content,
            TagsString = articleEditDto.TagsString!,
        };
    }

    public static ArticleEditDto ToEditDto(this Article article)
    {
        return new ArticleEditDto
        {
            Id = article.Id,
            AuthorName = article.AuthorName,
            Title = article.Title,
            Content = article.Content,
            TagsString = article.TagsString,
        };
    }
}