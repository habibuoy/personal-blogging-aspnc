using Microsoft.EntityFrameworkCore;
using PersonalBlogging.Models;

namespace PersonalBlogging.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
    
    public DbSet<Article> Articles { get; set; }
}