using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalBlogging.Data;
using PersonalBlogging.Models;
using PersonalBlogging.Models.Dto;
using ZiggyCreatures.Caching.Fusion;

namespace PersonalBlogging.Controllers;

[Route("")]
public class HomeController : Controller
{
    private const int MaximumTagCount = 5;
    private const string NoneTags = "None";
    private const string AllTagsCacheKey = "AllTags";
    private const string ArticleCacheKeyPrefix = "Article:";

    private readonly Tag NoneTagObject = new() { Name = "None" };
    private readonly ILogger<HomeController> logger;
    private readonly ApplicationDbContext context;
    private readonly IFusionCache cache;

    private static readonly string[] SortByOptions = { "Title", "Published Date", "Last Update" };
    private static readonly string[] SortOrderOptions = { "Asc", "Desc" };

    private static readonly Dictionary<string, Expression<Func<Article, object>>> SorterExpressions = new()
    {
        { SortByOptions[0], article => article.Title},
        { SortByOptions[1], article => article.PublishedDate!},
        { SortByOptions[2], article => article.LastModifiedDate!},
    };

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IFusionCache cache)
    {
        this.logger = logger;
        this.context = context;
        this.cache = cache;
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

        var tagsOptionList = await cache.GetOrSetAsync<List<Tag>>(
            AllTagsCacheKey,
            async (ctx, ct) => await context.Tags.ToListAsync(cancellationToken: ct));

        if (tagsOptionList != null
            && tagsOptionList.Count > 0
            && !tagsOptionList.Any(tag => tag.Name == NoneTags))
        {
            tagsOptionList.Insert(0, NoneTagObject);
        }

        var tagStrings = tagsOptionList!.Select(tag => tag.Name);

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
        return View(new ArticleCreateDto()
        {
            MaxTagCount = MaximumTagCount
        });
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([Bind("AuthorName, Title, Content, TagsString")] ArticleCreateDto input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var article = input.ToArticle();

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
                await UpdateTags(newTagNames);
            }
            catch (DBConcurrencyException e)
            {
                logger.LogError($"{nameof(Create)}: {e}");
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        return View(input);
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var article = await cache.GetOrSetAsync<ArticleDto>($"{ArticleCacheKeyPrefix}{id}",
            async (ctx, ct) =>
            {
                var article = await context.Articles
                .Include(a => a.Tags)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken: ct);

                return article?.ToDto();
            },
            options => options.SetDuration(TimeSpan.FromSeconds(10)));

        if (article == null)
        {
            logger.LogInformation($"{nameof(Details)}: Article id {id} not found");
            return NotFound();
        }

        return View(article);
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

        return View(article.ToEditDto());
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(int? id, [Bind("Id, AuthorName, Title, Content, TagsString")] ArticleEditDto input)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var (exists, existing) = await ArticleExists(id.Value);

        if (id != input.Id
            || !exists)
        {
            logger.LogInformation($"{nameof(Edit)}: Article id {id} not found");
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var article = input.ToArticle();
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

                await cache.RemoveAsync($"{ArticleCacheKeyPrefix}{id}");

                await UpdateTags(newTagNames);
            }
            catch (DBConcurrencyException e)
            {
                logger.LogError($"{nameof(Edit)}: Exception {e}");
                throw;
            }

            return RedirectToAction(nameof(Details), new { Id = id });
        }

        logger.LogInformation($"{nameof(Edit)}: Invalid Article Model");
        return View(input);
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

            await UpdateTags(null);
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

    private async Task UpdateTags(IEnumerable<string>? newTags)
    {
        var unusedTags = await context.Tags
            .Where(t => !t.Articles.Any())
            .ToListAsync();

        bool tagsChanged = false;

        if (unusedTags.Count > 0
            || newTags != null && newTags.Any())
        {
            if (unusedTags.Count > 0)
            {
                context.Tags.RemoveRange(unusedTags);
            }
            tagsChanged = true;
        }

        if (tagsChanged)
        {
            await context.SaveChangesAsync();
            await cache.RemoveAsync(AllTagsCacheKey);
        }
    }
}
