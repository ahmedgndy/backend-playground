// =============================================================================
// AUTHENTICATION CONTROLLER
// =============================================================================
// This controller handles the OTP authentication flow:
// - POST /api/auth/request-otp - Request an OTP to be sent to email
// - POST /api/auth/verify-otp  - Verify the OTP code
//
// SECURITY NOTES:
// - Rate limiting is applied to prevent abuse
// - Generic error messages prevent email enumeration
// - All OTP operations are logged (but OTP values are NEVER logged)
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OTP.Models.DTOs;
using OTP.Services.Interfaces;

namespace OTP.Controllers;

/// <summary>
/// Controller for OTP-based authentication endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IOtpService otpService,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _otpService = otpService;
        _emailService = emailService;
        _logger = logger;
    }

    // =========================================================================
    // POST /api/auth/request-otp
    // =========================================================================
    /// <summary>
    /// Request an OTP to be sent to the provided email address.
    /// </summary>
    /// <param name="request">The email address to send OTP to.</param>
    /// <returns>Generic success message (doesn't reveal if email exists).</returns>
    /// <remarks>
    /// Rate limited to prevent abuse:
    /// - 5 requests per minute per IP
    /// - 3 requests per minute per email
    /// </remarks>
    [HttpPost("request-otp")]
    [EnableRateLimiting("otp-request")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequestDto request)
    {
        // Model validation is automatic via [ApiController]
        // Invalid models will return 400 automatically

        _logger.LogInformation(
            "OTP request received for email: {Email} from IP: {IP}",
            MaskEmail(request.Email),
            GetClientIp());

        try
        {
            // Generate OTP
            var otp = await _otpService.GenerateOtpAsync(request.Email);

            // Send OTP via email
            var emailSent = await _emailService.SendOtpEmailAsync(request.Email, otp);

            if (!emailSent)
            {
                _logger.LogWarning("Failed to send OTP email to {Email}", MaskEmail(request.Email));
                // Still return success to prevent email enumeration
                // In production, you might want to handle this differently
            }

            // IMPORTANT: Always return the same message regardless of whether
            // the email exists or was sent successfully.
            // This prevents attackers from discovering valid email addresses.
            return Ok(ApiResponse.Ok(
                "If this email is registered, you will receive a verification code shortly."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OTP request for {Email}", MaskEmail(request.Email));

            // Return generic error (don't expose internal details)
            return StatusCode(500, ApiResponse.Fail(
                "An error occurred while processing your request. Please try again."));
        }
    }

    // =========================================================================
    // POST /api/auth/verify-otp
    // =========================================================================
    /// <summary>
    /// Verify an OTP code entered by the user.
    /// </summary>
    /// <param name="request">Email and OTP code to verify.</param>
    /// <returns>Success or failure with appropriate message.</returns>
    [HttpPost("verify-otp")]
    [EnableRateLimiting("otp-verify")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto request)
    {
        _logger.LogInformation(
            "OTP verification attempt for email: {Email} from IP: {IP}",
            MaskEmail(request.Email),
            GetClientIp());

        try
        {
            // Verify the OTP
            var result = await _otpService.VerifyOtpAsync(request.Email, request.Otp);

            if (result.IsValid)
            {
                _logger.LogInformation(
                    "OTP verified successfully for {Email}",
                    MaskEmail(request.Email));

                // In a real application, you would:
                // - Generate a JWT token
                // - Create a session
                // - Set authentication cookies
                // For this demo, we just return success

                return Ok(ApiResponse<object>.Ok(
                    "Email verified successfully!",
                    new
                    {
                        verified = true,
                        message = "You would typically receive a JWT token here"
                    }));
            }
            else
            {
                _logger.LogWarning(
                    "OTP verification failed for {Email}: {Reason}",
                    MaskEmail(request.Email),
                    result.FailureReason);

                return BadRequest(ApiResponse.Fail(result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {Email}", MaskEmail(request.Email));

            return StatusCode(500, ApiResponse.Fail(
                "An error occurred while verifying your code. Please try again."));
        }
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Gets the client's IP address from the request.
    /// Handles forwarded headers for reverse proxy scenarios.
    /// </summary>
    private string GetClientIp()
    {
        // Check for forwarded IP (when behind a reverse proxy like nginx)
        var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // Fall back to direct connection IP
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
