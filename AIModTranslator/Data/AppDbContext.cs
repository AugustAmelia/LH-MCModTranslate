using Microsoft.EntityFrameworkCore;

namespace AIModTranslator.Data;

public class AppDbContext : DbContext
{
    public DbSet<TmEntry> TranslationMemory { get; set; }
    public DbSet<GlossaryEntry> Glossary { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Add unique constraint/index for fast lookups
        modelBuilder.Entity<TmEntry>().HasIndex(e => e.OriginalText).IsUnique();
    }
}
