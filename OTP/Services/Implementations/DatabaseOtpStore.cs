// =============================================================================
// DATABASE OTP STORE - SQLite Implementation
// =============================================================================
// This implementation stores OTPs in a SQLite database using Entity Framework Core.
// SQLite is perfect for learning and small applications - it's just a file!
// For production with high traffic, consider using SQL Server or PostgreSQL.
// =============================================================================

using Microsoft.EntityFrameworkCore;
using OTP.Data;
using OTP.Models;
using OTP.Services.Interfaces;

namespace OTP.Services.Implementations;

/// <summary>
/// Database-backed OTP store using Entity Framework Core.
/// Stores OTPs persistently in SQLite database.
/// </summary>
public class DatabaseOtpStore : IOtpStore
{
    // Dependency injection: we receive the database context
    private readonly OtpDbContext _context;
    private readonly ILogger<DatabaseOtpStore> _logger;

    public DatabaseOtpStore(OtpDbContext context, ILogger<DatabaseOtpStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveAsync(OtpRecord record)
    {
        // Add the new OTP record to the database
        _context.OtpRecords.Add(record);
        await _context.SaveChangesAsync();

        // Log the save (but NEVER log the actual OTP!)
        _logger.LogInformation(
            "OTP record created for email: {Email}, expires at: {ExpiresAt}",
            MaskEmail(record.Email),  // Mask email for privacy in logs
            record.ExpiresAt);
    }

    /// <inheritdoc />
    public async Task<OtpRecord?> GetActiveOtpAsync(string email)
    {
        // Find the most recent OTP that:
        // 1. Matches the email (case-insensitive)
        // 2. Has not been used
        // 3. Has not expired
        // 4. Has not exceeded max attempts
        var now = DateTime.UtcNow;

        return await _context.OtpRecords
            .Where(o => o.Email.ToLower() == email.ToLower())
            .Where(o => !o.IsUsed)
            .Where(o => o.ExpiresAt > now)
            .Where(o => o.AttemptCount < o.MaxAttempts)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(OtpRecord record)
    {
        _context.OtpRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task InvalidateAllAsync(string email)
    {
        // Mark all OTPs for this email as used
        var activeOtps = await _context.OtpRecords
            .Where(o => o.Email.ToLower() == email.ToLower())
            .Where(o => !o.IsUsed)
            .ToListAsync();

        foreach (var otp in activeOtps)
        {
            otp.IsUsed = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Invalidated {Count} OTP(s) for email: {Email}",
            activeOtps.Count,
            MaskEmail(email));
    }

    /// <inheritdoc />
    public async Task CleanupExpiredAsync()
    {
        // Delete OTPs that expired more than 24 hours ago
        // We keep them for a while for auditing purposes
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var expiredCount = await _context.OtpRecords
            .Where(o => o.ExpiresAt < cutoff)
            .ExecuteDeleteAsync();

        if (expiredCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired OTP records", expiredCount);
        }
    }

    /// <summary>
    /// Masks an email address for safe logging.
    /// Example: "user@example.com" becomes "u***@example.com"
    /// </summary>
    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";

        var localPart = parts[0];
        var domain = parts[1];

        if (localPart.Length <= 1)
            return $"*@{domain}";

        return $"{localPart[0]}***@{domain}";
    }
}
