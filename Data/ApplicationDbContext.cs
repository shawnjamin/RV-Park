using Microsoft.EntityFrameworkCore;
using RVPark.Models;

namespace RVPark.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<RvSite> RvSites => Set<RvSite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RvSite>(entity =>
        {
            entity.HasIndex(site => site.SiteNumber).IsUnique();
            entity.Property(site => site.NightlyRate).HasPrecision(8, 2);
        });
    }
}
