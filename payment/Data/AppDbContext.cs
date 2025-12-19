using Microsoft.EntityFrameworkCore;
using Payment.Models;

namespace Payment.Data;

// Minimal DbContext - ONLY stores StripeCustomerId and SubscriptionPlan
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.StripeCustomerId);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.StripeCustomerId).HasMaxLength(256);
        });
    }
}
