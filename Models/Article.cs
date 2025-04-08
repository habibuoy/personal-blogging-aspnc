using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace PersonalBlogging.Models;

public class Article
{
    public int Id { get; set; }
    [DisplayName("Published Date")]
    public DateTime? PublishedDate { get; set; }
    [DisplayName("Last Modified Date")]
    public DateTime? LastModifiedDate { get; set; }
    public string Title { get; set; }
    [DisplayName("Author Name")]
    public string AuthorName { get; set; }
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
}