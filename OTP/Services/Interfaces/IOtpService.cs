// =============================================================================
// OTP SERVICE INTERFACE
// =============================================================================
// This interface defines the contract for OTP operations.
// Using interfaces allows us to easily swap implementations (e.g., for testing)
// and follows the Dependency Inversion Principle (the 'D' in SOLID).
// =============================================================================

using OTP.Models;

namespace OTP.Services.Interfaces;

/// <summary>
/// Interface for OTP (One-Time Password) operations.
/// Defines methods for generating and verifying OTPs securely.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a new 6-digit OTP for the given email address.
    /// The OTP is hashed before storage - the plaintext OTP is returned
    /// so it can be sent to the user (but never stored).
    /// </summary>
    /// <param name="email">The email address to generate OTP for.</param>
    /// <returns>The plaintext 6-digit OTP (to be sent to user).</returns>
    Task<string> GenerateOtpAsync(string email);

    /// <summary>
    /// Verifies an OTP entered by the user.
    /// Checks expiration, attempt count, and hash match.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="otp">The OTP entered by the user.</param>
    /// <returns>True if valid, false otherwise.</returns>
    Task<OtpVerificationResult> VerifyOtpAsync(string email, string otp);

    /// <summary>
    /// Invalidates any existing OTPs for the given email.
    /// Called before generating a new OTP to ensure only one active OTP exists.
    /// </summary>
    /// <param name="email">The email address.</param>
    Task InvalidateExistingOtpsAsync(string email);
}

/// <summary>
/// Result of OTP verification operation.
/// Provides detailed information about why verification succeeded or failed.
/// </summary>
public class OtpVerificationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public OtpVerificationFailureReason? FailureReason { get; set; }

    public static OtpVerificationResult Success() => new()
    {
        IsValid = true,
        Message = "OTP verified successfully"
    };

    public static OtpVerificationResult Failure(OtpVerificationFailureReason reason, string message) => new()
    {
        IsValid = false,
        Message = message,
        FailureReason = reason
    };
}

/// <summary>
/// Enum describing why OTP verification failed.
/// Useful for logging and determining appropriate response.
/// </summary>
public enum OtpVerificationFailureReason
{
    NotFound,           // No OTP found for this email
    Expired,            // OTP has expired
    MaxAttemptsExceeded,// Too many wrong attempts
    AlreadyUsed,        // OTP was already used
    InvalidCode         // Wrong OTP code
}
