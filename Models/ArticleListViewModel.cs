using Microsoft.AspNetCore.Mvc.Rendering;

namespace PersonalBlogging.Models;

public class ArticleListViewModel
{
    public string? Search { get; set; }
    public List<Article>? Articles { get; set; }
    public SelectList? TagsOptions { get; set; }
    public string[]? Tags { get; set; }
}