// =============================================================================
// SMTP EMAIL SERVICE
// =============================================================================
// Implementation using SMTP with MailKit library.
// Works with any SMTP server (Gmail, Outlook, custom mail servers).
//
// GMAIL SETUP:
// 1. Enable 2-factor authentication on your Google account
// 2. Generate an "App Password" at https://myaccount.google.com/apppasswords
// 3. Use that app password (not your regular password) in config
// =============================================================================

using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using OTP.Services.Interfaces;

namespace OTP.Services.Implementations;

/// <summary>
/// Email service implementation using SMTP with MailKit.
/// Compatible with any SMTP server.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendOtpEmailAsync(string email, string otp)
    {
        try
        {
            // Get SMTP configuration
            var smtpHost = _configuration["Email:Smtp:Host"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["Email:Smtp:Port"] ?? "587");
            var smtpUser = _configuration["Email:Smtp:Username"];
            var smtpPassword = _configuration["Email:Smtp:Password"];
            var fromEmail = _configuration["Email:Smtp:FromEmail"] ?? smtpUser;
            var fromName = _configuration["Email:Smtp:FromName"] ?? "OTP Demo App";
            var useSsl = bool.Parse(_configuration["Email:Smtp:UseSsl"] ?? "true");

            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogError("SMTP credentials are not configured");
                return false;
            }

            // Create email message using MimeKit
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Your Verification Code";

            // Create the HTML body
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .otp-code {{ 
            font-size: 32px; 
            font-weight: bold; 
            letter-spacing: 8px; 
            color: #2563eb; 
            background: #eff6ff; 
            padding: 20px; 
            text-align: center; 
            border-radius: 8px;
            margin: 20px 0;
        }}
        .warning {{ color: #666; font-size: 14px; }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>Your Verification Code</h2>
        <p>Use the following code to verify your email address:</p>
        <div class='otp-code'>{otp}</div>
        <p class='warning'>This code will expire in 10 minutes.</p>
        <p class='warning'>If you did not request this code, please ignore this email.</p>
    </div>
</body>
</html>
";

            // Create plain text alternative
            var textBody = $@"
Your verification code is: {otp}

This code will expire in 10 minutes.

If you did not request this code, please ignore this email.
";

            // Build the message body with both text and HTML versions
            var bodyBuilder = new BodyBuilder
            {
                TextBody = textBody,
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            // Send via SMTP
            using var client = new SmtpClient();

            // Connect with appropriate security
            var secureSocketOptions = useSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(smtpHost, smtpPort, secureSocketOptions);

            // Authenticate
            await client.AuthenticateAsync(smtpUser, smtpPassword);

            // Send
            await client.SendAsync(message);

            // Disconnect
            await client.DisconnectAsync(true);

            _logger.LogInformation("OTP email sent successfully via SMTP to {Email}", MaskEmail(email));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email via SMTP");
            return false;
        }
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
