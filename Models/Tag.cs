using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PersonalBlogging.Models;

public class Tag
{
    [Key]
    public required string Name { get; set; }
    [JsonIgnore]
    public ICollection<Article> Articles { get; set; } = new List<Article>();
}