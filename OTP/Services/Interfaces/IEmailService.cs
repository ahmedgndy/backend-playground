// =============================================================================
// EMAIL SERVICE INTERFACE
// =============================================================================
// Interface for sending emails. We provide two implementations:
// 1. SendGridEmailService - Uses SendGrid API
// 2. SmtpEmailService - Uses SMTP with MailKit
// You can also create a mock implementation for testing.
// =============================================================================

namespace OTP.Services.Interfaces;

/// <summary>
/// Interface for email sending operations.
/// Implementations handle the actual email delivery mechanism.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an OTP email to the specified address.
    /// </summary>
    /// <param name="email">Recipient email address.</param>
    /// <param name="otp">The 6-digit OTP code.</param>
    /// <returns>True if email was sent successfully.</returns>
    Task<bool> SendOtpEmailAsync(string email, string otp);
}
