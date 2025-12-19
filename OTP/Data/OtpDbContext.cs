// =============================================================================
// OTP DATABASE CONTEXT
// =============================================================================
// Entity Framework Core DbContext for the OTP application.
// This is the main entry point for database operations.
// 
// We're using SQLite for simplicity - it's just a file!
// No database server needed - perfect for learning.
// =============================================================================

using Microsoft.EntityFrameworkCore;
using OTP.Models;

namespace OTP.Data;

/// <summary>
/// Database context for OTP storage.
/// Manages the OTP records table.
/// </summary>
public class OtpDbContext : DbContext
{
    public OtpDbContext(DbContextOptions<OtpDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Table for storing OTP records.
    /// </summary>
    public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();

    /// <summary>
    /// Configure the model (table structure, indexes, etc.)
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure OtpRecord entity
        modelBuilder.Entity<OtpRecord>(entity =>
        {
            // Table name
            entity.ToTable("OtpRecords");

            // Primary key
            entity.HasKey(e => e.Id);

            // Email - required, indexed for fast lookups
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);

            // Index on Email for fast queries
            entity.HasIndex(e => e.Email);

            // OtpHash - required
            entity.Property(e => e.OtpHash)
                .IsRequired()
                .HasMaxLength(256);

            // Salt - required
            entity.Property(e => e.Salt)
                .IsRequired()
                .HasMaxLength(256);

            // Index on ExpiresAt for cleanup queries
            entity.HasIndex(e => e.ExpiresAt);

            // Composite index for common query pattern
            entity.HasIndex(e => new { e.Email, e.IsUsed, e.ExpiresAt });
        });
    }
}
