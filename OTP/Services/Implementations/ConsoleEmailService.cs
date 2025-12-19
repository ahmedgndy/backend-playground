// =============================================================================
// CONSOLE EMAIL SERVICE (FOR DEVELOPMENT/TESTING)
// =============================================================================
// This implementation just logs the OTP to the console.
// Useful for development when you don't want to set up actual email.
// NEVER use this in production!
// =============================================================================

using OTP.Services.Interfaces;

namespace OTP.Services.Implementations;

/// <summary>
/// Development email service that logs OTP to console.
/// Use this for testing without setting up real email.
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> SendOtpEmailAsync(string email, string otp)
    {
        // In development, we log the OTP to console
        // This makes testing easy without needing real email setup

        _logger.LogWarning(
            "=== DEVELOPMENT MODE: OTP for {Email} is: {OTP} ===",
            email,
            otp);

        // Also write to console with nice formatting
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           DEVELOPMENT MODE - OTP NOTIFICATION            ║");
        Console.WriteLine("╠══════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║  Email: {email,-48} ║");
        Console.WriteLine($"║  OTP:   {otp,-48} ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        return Task.FromResult(true);
    }
}
