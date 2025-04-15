using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalBlogging.Data;
using PersonalBlogging.Models;
using PersonalBlogging.Models.Dto;

namespace PersonalBlogging.Controllers;

[Route("")]
public class HomeController : Controller
{
    private const int MaximumTagCount = 5;
    private const string NoneTags = "None";

    private readonly Tag NoneTagObject = new() { Name = "None" };
    private readonly ILogger<HomeController> logger;
    private readonly ApplicationDbContext context;

    private static readonly string[] SortByOptions = { "Title", "Published Date", "Last Update" };
    private static readonly string[] SortOrderOptions = { "Asc", "Desc" };

    private static readonly Dictionary<string, Expression<Func<Article, object>>> SorterExpressions = new()
    {
        { SortByOptions[0], article => article.Title},
        { SortByOptions[1], article => article.PublishedDate!},
        { SortByOptions[2], article => article.LastModifiedDate!},
    };

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        this.logger = logger;
        this.context = context;
    }

    [HttpGet("")]
    public IActionResult NoRoute()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("index")]
    public async Task<IActionResult> Index(string? search, string[]? tags, string? sortBy, string? sortOrder)
    {
        var articles = context.Articles.Include(a => a.Tags).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            articles = articles.Where(article => article.Title.ToUpper().Contains(search.ToUpper()));
        }

        if (tags != null
            && tags.Length > 1
            || (tags!.Length == 1
                && tags[0] != NoneTags))
        {
            articles = articles.Where(article => article.Tags!.Any(tag => tags.Contains(tag.Name)));
        }
        else if (tags != null
                 && tags.Length == 0)
        {
            tags = [NoneTags];
        }

        if (!string.IsNullOrEmpty(sortBy)
            && SorterExpressions.TryGetValue(sortBy, out var sorter))
        {
            if (string.IsNullOrEmpty(sortOrder)
                || sortOrder.Equals(SortOrderOptions[0]))
            {
                articles = articles.OrderBy(sorter);
            }
            else
            {
                articles = articles.OrderByDescending(sorter);
            }
        }

        var tagsOptionList = await context.Tags.ToListAsync();
        tagsOptionList.Insert(0, NoneTagObject);

        var tagStrings = tagsOptionList.Select(tag => tag.Name);

        var result = new ArticleListViewModel()
        {
            Articles = await articles.Select(article => article.ToDto()).ToListAsync(),
            Search = search,
            TagsOptions = new(tagStrings),
            Tags = tags,
            SortByOptions = new(SortByOptions),
            SortBy = sortBy,
            SortOrderOptions = new(SortOrderOptions),
            SortOrder = sortOrder,
        };

        return View(result);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View(new Article());
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([Bind("AuthorName, Title, Content, TagsString")] Article article)
    {
        if (ModelState.IsValid)
        {
            try
            {
                article.PublishedDate = article.LastModifiedDate = DateTime.Now;

                var tagNames = article.Tags!.Select(t => t.Name).Distinct().ToList();

                var existingTags = await context.Tags
                    .Where(t => tagNames.Contains(t.Name))
                    .ToListAsync();

                var newTagNames = tagNames.Except(existingTags.Select(t => t.Name)).ToList();
                var newTags = newTagNames.Select(name => new Tag { Name = name }).ToList();

                article.Tags = existingTags.Concat(newTags).ToList();
                article.EnsureMaximumTagsCount(MaximumTagCount);

                context.Add(article);
                await context.SaveChangesAsync();
            }
            catch (DBConcurrencyException e)
            {
                logger.LogError($"{nameof(Create)}: {e}");
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        return View(article);
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var article = await context.Articles
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            logger.LogInformation($"{nameof(Details)}: Article id {id} not found");
            return NotFound();
        }

        return View(article.ToDto());
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var article = await context.Articles
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (article == null)
        {
            logger.LogInformation($"{nameof(Edit)}: Article id {id} not found");
            return NotFound();
        }

        return View(article);
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(int? id, [Bind("Id, AuthorName, Title, Content, TagsString")] Article article)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var (exists, existing) = await ArticleExists(id.Value);

        if (id != article.Id
            || !exists)
        {
            logger.LogInformation($"{nameof(Edit)}: Article id {id} not found");
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // use the load and update pattern instead of context.Update()
                // because that method is causing the Already Tracked exception
                existing!.AuthorName = article.AuthorName;
                existing!.Title = article.Title;
                existing!.Content = article.Content;
                existing!.LastModifiedDate = DateTime.Now;

                var tagNames = article.Tags!.Select(t => t.Name).Distinct().ToList();

                var existingTags = await context.Tags
                    .Where(t => tagNames.Contains(t.Name))
                    .ToListAsync();

                var newTagNames = tagNames.Except(existingTags.Select(t => t.Name)).ToList();
                var newTags = newTagNames.Select(name => new Tag { Name = name }).ToList();

                var finalTags = existingTags.Concat(newTags).ToList();

                existing.Tags!.Clear();
                foreach (var tag in finalTags)
                {
                    existing.Tags.Add(tag);
                }

                existing.EnsureMaximumTagsCount(MaximumTagCount);
                await context.SaveChangesAsync();

                await UpdateTags();
            }
            catch (DBConcurrencyException e)
            {
                logger.LogError($"{nameof(Edit)}: Exception {e}");
                throw;
            }

            return RedirectToAction(nameof(Details), new { Id = id });
        }

        logger.LogInformation($"{nameof(Edit)}: Invalid Article Model");
        return View(article);
    }

    [HttpPost("delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var (exists, existing) = await ArticleExists(id.Value);

        if (!exists)
        {
            logger.LogInformation($"{nameof(Delete)}: Article id {id} not found");
            return NotFound();
        }

        try
        {
            context.Articles.Remove(existing!);
            await context.SaveChangesAsync();

            await UpdateTags();
        }
        catch (DBConcurrencyException e)
        {
            logger.LogError($"{nameof(Delete)}: Exception {e}");
            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<(bool exists, Article? article)> ArticleExists(int id)
    {
        var article = await context.Articles
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.Id == id);
        return (article != null, article);
    }

    private async Task UpdateTags()
    {
        var unusedTags = await context.Tags
            .Where(t => !t.Articles.Any())
            .ToListAsync();

        if (unusedTags.Count != 0)
        {
            context.Tags.RemoveRange(unusedTags);
            await context.SaveChangesAsync();
        }
    }
}
