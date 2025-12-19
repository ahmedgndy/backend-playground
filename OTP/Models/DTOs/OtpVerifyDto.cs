// =============================================================================
// OTP VERIFY DTO - Data Transfer Object
// =============================================================================
// Used when verifying an OTP entered by the user.
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace OTP.Models.DTOs;

/// <summary>
/// DTO for verifying an OTP.
/// Used by the POST /api/auth/verify-otp endpoint.
/// </summary>
public class OtpVerifyDto
{
    /// <summary>
    /// The email address the OTP was sent to.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The 6-digit OTP code entered by the user.
    /// </summary>
    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be exactly 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must contain only digits")]
    public string Otp { get; set; } = string.Empty;
}
