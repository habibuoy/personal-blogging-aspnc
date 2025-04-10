using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalBlogging.Models;

public class Article
{
    public int Id { get; set; }
    [DisplayName("Published Date")]
    public DateTime? PublishedDate { get; set; }
    [DisplayName("Last Update")]
    public DateTime? LastModifiedDate { get; set; }
    public string Title { get; set; } = string.Empty;
    [DisplayName("Author Name")]
    public string AuthorName { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string[]? Tags { get; set; }

    [NotMapped]
    public string TagsString
    {
        get
        {
            return Tags != null ? string.Join(", ", Tags) : string.Empty;
        }

        set
        {
            Tags = value?.Split(",").Select(tag => tag.Trim()).ToArray();
        }
    }

    public void EnsureMaximumTagsCount(int count)
    {
        if (Tags != null
            && Tags.Length > count)
        {
            var tags = Tags;
            Array.Resize(ref tags, count);
            Tags = tags;
        }
    }
}