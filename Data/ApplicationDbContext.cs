using Microsoft.EntityFrameworkCore;
using PersonalBlogging.Models;

namespace PersonalBlogging.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Article> Articles { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Article>()
            .HasMany(article => article.Tags)
            .WithMany(tag => tag.Articles);
    }
}