using Microsoft.EntityFrameworkCore;
using RVPark.Models;

namespace RVPark.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<SiteType> SiteTypes => Set<SiteType>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<SitePhoto> SitePhotos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(user => user.Username).IsUnique();
        });

        modelBuilder.Entity<SiteType>(entity =>
        {
            entity.Property(siteType => siteType.Price).HasPrecision(8, 2);
        });

        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasIndex(site => site.SiteNumber).IsUnique();

            entity.Property(site => site.HookupType)
                .HasConversion<string>();

            entity.HasOne(site => site.SiteType)
                .WithMany(siteType => siteType.Sites)
                .HasForeignKey(site => site.SiteTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasIndex(reservation => reservation.ReservationNumber).IsUnique();

            entity.Property(reservation => reservation.Status)
                .HasConversion<string>();

            entity.HasOne(reservation => reservation.User)
                .WithMany(user => user.Reservations)
                .HasForeignKey(reservation => reservation.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(reservation => reservation.Site)
                .WithMany(site => site.Reservations)
                .HasForeignKey(reservation => reservation.SiteId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Bill>(entity =>
        {
            entity.Property(bill => bill.Type)
                .HasConversion<string>();

            entity.Property(bill => bill.Amount).HasPrecision(8, 2);

            entity.HasOne(bill => bill.Reservation)
                .WithMany(reservation => reservation.Bills)
                .HasForeignKey(bill => bill.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(payment => payment.PaymentMethod)
                .HasConversion<string>();

            entity.Property(payment => payment.Amount).HasPrecision(8, 2);

            entity.HasOne(payment => payment.Bill)
                .WithMany(bill => bill.Payments)
                .HasForeignKey(payment => payment.BillId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
