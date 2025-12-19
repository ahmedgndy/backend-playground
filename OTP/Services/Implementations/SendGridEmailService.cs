// =============================================================================
// SENDGRID EMAIL SERVICE
// =============================================================================
// Implementation using SendGrid API for sending emails.
// SendGrid is a popular email delivery service with a free tier.
//
// To use this:
// 1. Create a SendGrid account at https://sendgrid.com
// 2. Create an API key
// 3. Add the API key to appsettings.json
// =============================================================================

using OTP.Services.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace OTP.Services.Implementations;

/// <summary>
/// Email service implementation using SendGrid API.
/// </summary>
public class SendGridEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> SendOtpEmailAsync(string email, string otp)
    {
        try
        {
            // Get SendGrid configuration
            var apiKey = _configuration["Email:SendGrid:ApiKey"];
            var fromEmail = _configuration["Email:SendGrid:FromEmail"] ?? "noreply@example.com";
            var fromName = _configuration["Email:SendGrid:FromName"] ?? "OTP Demo App";

            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("SendGrid API key is not configured");
                return false;
            }

            // Create SendGrid client
            var client = new SendGridClient(apiKey);

            // Create email message
            var from = new EmailAddress(fromEmail, fromName);
            var to = new EmailAddress(email);
            var subject = "Your Verification Code";

            // Plain text content
            var plainTextContent = $@"
Your verification code is: {otp}

This code will expire in 10 minutes.

If you did not request this code, please ignore this email.
";

            // HTML content (looks nicer in email clients)
            var htmlContent = $@"
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

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            // Send email
            var response = await client.SendEmailAsync(msg);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("OTP email sent successfully via SendGrid to {Email}", MaskEmail(email));
                return true;
            }
            else
            {
                var responseBody = await response.Body.ReadAsStringAsync();
                _logger.LogError(
                    "SendGrid failed to send email. Status: {Status}, Body: {Body}",
                    response.StatusCode,
                    responseBody);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email via SendGrid");
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
