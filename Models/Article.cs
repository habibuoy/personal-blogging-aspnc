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
    public ICollection<Tag>? Tags { get; set; }

    [NotMapped]
    public string TagsString
    {
        get
        {
            return Tags != null ? string.Join(", ", Tags.Select(tag => tag.Name)) : string.Empty;
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Tags = new List<Tag>();
                return;
            }

            Tags = value.Split(", ")
                .Distinct()
                .Select(tagName => new Tag { Name = tagName })
                .ToList();
        }
    }

    public void EnsureMaximumTagsCount(int count)
    {
        if (Tags != null
            && Tags.Count > count)
        {
            Tags = Tags.Take(count).ToList();
        }
    }
}