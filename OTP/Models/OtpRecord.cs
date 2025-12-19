// =============================================================================
// OTP RECORD - Database Entity
// =============================================================================
// This model represents a stored OTP in the database.
// IMPORTANT: We NEVER store the actual OTP! Only its hash.
// This is a critical security practice - if the database is compromised,
// attackers cannot retrieve the actual OTP values.
// =============================================================================

namespace OTP.Models;

/// <summary>
/// Represents an OTP record stored in the database.
/// Contains all information needed for secure OTP verification.
/// </summary>
public class OtpRecord
{
    /// <summary>
    /// Primary key - unique identifier for this OTP record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The email address this OTP was sent to.
    /// Used to look up OTPs during verification.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The hashed OTP value (NEVER the plaintext OTP!).
    /// We use SHA256 with a salt to create this hash.
    /// </summary>
    public string OtpHash { get; set; } = string.Empty;

    /// <summary>
    /// Random salt used when hashing the OTP.
    /// Salt prevents rainbow table attacks - each OTP gets a unique salt.
    /// Even if two users get the same OTP "123456", the hashes will be different.
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// When this OTP expires (UTC time).
    /// OTPs should be short-lived (typically 5-10 minutes).
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When this OTP was created (UTC time).
    /// Useful for auditing and debugging.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Number of failed verification attempts.
    /// After MaxAttempts (typically 3), the OTP is invalidated.
    /// This prevents brute-force attacks.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Maximum allowed verification attempts.
    /// Default is 3 - after 3 wrong guesses, the OTP is blocked.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Flag indicating if this OTP has already been used.
    /// OTPs are SINGLE-USE - once verified, they cannot be used again.
    /// This prevents replay attacks.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// When the OTP was successfully verified (if applicable).
    /// Null if not yet verified.
    /// </summary>
    public DateTime? VerifiedAt { get; set; }
}
