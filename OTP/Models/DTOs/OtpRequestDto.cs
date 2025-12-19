// =============================================================================
// OTP REQUEST DTO - Data Transfer Object
// =============================================================================
// DTOs are simple classes used to transfer data between the client and server.
// They help validate input and provide a clean API contract.
// =============================================================================

using System.ComponentModel.DataAnnotations;

namespace OTP.Models.DTOs;

/// <summary>
/// DTO for requesting an OTP to be sent to an email address.
/// Used by the POST /api/auth/request-otp endpoint.
/// </summary>
public class OtpRequestDto
{
    /// <summary>
    /// The email address to send the OTP to.
    /// Must be a valid email format.
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;
}
