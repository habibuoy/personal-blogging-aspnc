using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalBlogging.Data;
using PersonalBlogging.Models;

namespace PersonalBlogging.Controllers;

[Route("")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> logger;
    private readonly ApplicationDbContext context;

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
    public async Task<IActionResult> Index(string? search, string[]? tags)
    {
        var articles = from article in context.Articles select article;
        var tagsArray = articles.SelectMany(article => article.Tags!);

        if (!string.IsNullOrEmpty(search))
        {
            articles = articles.Where(article => article.Title.ToUpper().Contains(search.ToUpper()));
        }

        if (tags != null 
            && tags.Length > 1
            || (tags!.Length == 1
                && tags[0] != "None"))
        {
            articles = articles.Where(article => article.Tags!.Intersect(tags).Count() > 0);
        }

        var result = new ArticleListViewModel()
        {
            Articles = await articles.ToListAsync(),
            Search = search,
            TagsOptions = new(await tagsArray.Distinct().ToListAsync()),
            Tags = tags
        };

        return View(result);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([Bind("AuthorName, Title, Content, TagsString")] Article article)
    {
        if (ModelState.IsValid)
        {
            try
            {
                article.PublishedDate = article.LastModifiedDate = DateTime.Now;
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
        return View();
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var article = await context.Articles.FindAsync(id);

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

        var article = await context.Articles.FindAsync(id);

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

        var (exists, existing) = ArticleExists(id.Value);
        
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
                existing!.Tags = article.Tags;
                existing!.LastModifiedDate = DateTime.Now;

                await context.SaveChangesAsync();
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

    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var article = await context.Articles.FindAsync(id);

        if (article == null)
        {
            logger.LogInformation($"{nameof(Edit)}: Article id {id} not found");
            return NotFound();
        }

        return View(article);
    }

    [HttpPost("delete/{id}")]
    public async Task<IActionResult> ConfirmDelete(int? id)
    {
        if (id == null)
        {
            return BadRequest();
        }

        var (exists, existing) = ArticleExists(id.Value);
        
        if (!exists)
        {
            logger.LogInformation($"{nameof(Delete)}: Article id {id} not found");
            return NotFound();
        }

        try
        {
            context.Articles.Remove(existing!);
            await context.SaveChangesAsync();
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

    private (bool exists, Article? article) ArticleExists(int id)
    {
        var article = context.Articles.Find(id);
        return (article != null, article);
    }
}
