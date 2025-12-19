// =============================================================================
// OTP STORE INTERFACE
// =============================================================================
// This interface abstracts the OTP storage mechanism.
// We can implement this with different storage backends:
// - SQLite/SQL Server (persistent, uses EF Core)
// - Redis (ephemeral, in-memory, faster)
// This follows the Repository Pattern.
// =============================================================================

using OTP.Models;

namespace OTP.Services.Interfaces;

/// <summary>
/// Interface for OTP storage operations.
/// Abstracts the underlying storage mechanism (database, Redis, etc.).
/// </summary>
public interface IOtpStore
{
    /// <summary>
    /// Saves a new OTP record.
    /// </summary>
    /// <param name="record">The OTP record to save.</param>
    Task SaveAsync(OtpRecord record);

    /// <summary>
    /// Gets the most recent active (not used, not expired) OTP for an email.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <returns>The OTP record if found, null otherwise.</returns>
    Task<OtpRecord?> GetActiveOtpAsync(string email);

    /// <summary>
    /// Updates an existing OTP record.
    /// Used to increment attempt count or mark as used.
    /// </summary>
    /// <param name="record">The OTP record to update.</param>
    Task UpdateAsync(OtpRecord record);

    /// <summary>
    /// Invalidates (marks as used) all OTPs for an email.
    /// </summary>
    /// <param name="email">The email address.</param>
    Task InvalidateAllAsync(string email);

    /// <summary>
    /// Cleans up expired OTP records.
    /// Should be called periodically to prevent database bloat.
    /// </summary>
    Task CleanupExpiredAsync();
}
