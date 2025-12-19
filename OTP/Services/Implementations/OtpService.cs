// =============================================================================
// OTP SERVICE IMPLEMENTATION
// =============================================================================
// This is the core OTP service that handles:
// - Generating secure OTPs using cryptographic random number generator
// - Hashing OTPs with salt using SHA256
// - Verifying OTPs using constant-time comparison
//
// SECURITY FEATURES:
// 1. Cryptographically secure random numbers (not Math.Random!)
// 2. Salted hashing (each OTP has a unique salt)
// 3. Constant-time comparison (prevents timing attacks)
// 4. Single-use OTPs
// 5. Maximum attempt limiting
// 6. Expiration enforcement
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using OTP.Models;
using OTP.Services.Interfaces;

namespace OTP.Services.Implementations;

/// <summary>
/// Core OTP service implementation with full security features.
/// </summary>
public class OtpService : IOtpService
{
    private readonly IOtpStore _otpStore;
    private readonly ILogger<OtpService> _logger;

    // Configuration constants (could be moved to appsettings.json)
    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRATION_MINUTES = 10;
    private const int MAX_VERIFICATION_ATTEMPTS = 3;

    public OtpService(IOtpStore otpStore, ILogger<OtpService> logger)
    {
        _otpStore = otpStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string> GenerateOtpAsync(string email)
    {
        // Step 1: Invalidate any existing OTPs for this email
        // This ensures only ONE active OTP exists at a time
        await InvalidateExistingOtpsAsync(email);

        // Step 2: Generate a 6-digit OTP using secure random number generator
        // IMPORTANT: We use RandomNumberGenerator, NOT Random!
        // Random is predictable and NOT suitable for security purposes.
        var otp = GenerateSecureOtp();

        // Step 3: Generate a random salt for hashing
        // Salt prevents rainbow table attacks
        var salt = GenerateSalt();

        // Step 4: Hash the OTP with the salt
        // We NEVER store the plaintext OTP!
        var hash = HashOtp(otp, salt);

        // Step 5: Create and save the OTP record
        var record = new OtpRecord
        {
            Email = email.ToLower(), // Normalize email to lowercase
            OtpHash = hash,
            Salt = salt,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRATION_MINUTES),
            MaxAttempts = MAX_VERIFICATION_ATTEMPTS,
            AttemptCount = 0,
            IsUsed = false
        };

        await _otpStore.SaveAsync(record);

        _logger.LogInformation(
            "OTP generated for {Email}, expires in {Minutes} minutes",
            MaskEmail(email),
            OTP_EXPIRATION_MINUTES);

        // Return the plaintext OTP (to be sent to user via email)
        // After this point, we have NO way to recover the OTP - only verify it
        return otp;
    }

    /// <inheritdoc />
    public async Task<OtpVerificationResult> VerifyOtpAsync(string email, string otp)
    {
        // Step 1: Get the active OTP record
        var record = await _otpStore.GetActiveOtpAsync(email);

        if (record == null)
        {
            _logger.LogWarning(
                "OTP verification failed: No active OTP found for {Email}",
                MaskEmail(email));

            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.NotFound,
                "No active OTP found. Please request a new one.");
        }

        // Step 2: Check if OTP has expired
        if (record.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning(
                "OTP verification failed: OTP expired for {Email}",
                MaskEmail(email));

            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.Expired,
                "OTP has expired. Please request a new one.");
        }

        // Step 3: Check if max attempts exceeded
        if (record.AttemptCount >= record.MaxAttempts)
        {
            _logger.LogWarning(
                "OTP verification failed: Max attempts exceeded for {Email}",
                MaskEmail(email));

            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.MaxAttemptsExceeded,
                "Too many failed attempts. Please request a new OTP.");
        }

        // Step 4: Check if already used
        if (record.IsUsed)
        {
            _logger.LogWarning(
                "OTP verification failed: OTP already used for {Email}",
                MaskEmail(email));

            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.AlreadyUsed,
                "This OTP has already been used. Please request a new one.");
        }

        // Step 5: Hash the provided OTP and compare
        var providedHash = HashOtp(otp, record.Salt);

        // IMPORTANT: Use constant-time comparison!
        // Regular string comparison (==) can leak timing information
        // An attacker could measure how long comparison takes and deduce characters
        var isValid = ConstantTimeEquals(providedHash, record.OtpHash);

        if (!isValid)
        {
            // Increment attempt count
            record.AttemptCount++;
            await _otpStore.UpdateAsync(record);

            _logger.LogWarning(
                "OTP verification failed: Invalid code for {Email}, attempt {Attempt}/{Max}",
                MaskEmail(email),
                record.AttemptCount,
                record.MaxAttempts);

            var remainingAttempts = record.MaxAttempts - record.AttemptCount;
            return OtpVerificationResult.Failure(
                OtpVerificationFailureReason.InvalidCode,
                $"Invalid OTP. {remainingAttempts} attempt(s) remaining.");
        }

        // Step 6: Mark OTP as used (single-use enforcement)
        record.IsUsed = true;
        record.VerifiedAt = DateTime.UtcNow;
        await _otpStore.UpdateAsync(record);

        _logger.LogInformation(
            "OTP verified successfully for {Email}",
            MaskEmail(email));

        return OtpVerificationResult.Success();
    }

    /// <inheritdoc />
    public async Task InvalidateExistingOtpsAsync(string email)
    {
        await _otpStore.InvalidateAllAsync(email);
    }

    // =========================================================================
    // PRIVATE HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Generates a cryptographically secure 6-digit OTP.
    /// Uses RandomNumberGenerator which is suitable for security purposes.
    /// </summary>
    private static string GenerateSecureOtp()
    {
        // Create a buffer for random bytes
        var randomBytes = new byte[4];

        // Fill with cryptographically secure random bytes
        RandomNumberGenerator.Fill(randomBytes);

        // Convert to a positive integer
        var randomNumber = Math.Abs(BitConverter.ToInt32(randomBytes, 0));

        // Get a 6-digit number (000000-999999)
        var otp = randomNumber % 1000000;

        // Pad with leading zeros if necessary
        return otp.ToString().PadLeft(OTP_LENGTH, '0');
    }

    /// <summary>
    /// Generates a random salt for hashing.
    /// Salt is 32 bytes (256 bits) encoded as Base64.
    /// </summary>
    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        RandomNumberGenerator.Fill(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// Hashes an OTP with a salt using SHA256.
    /// Format: SHA256(salt + otp)
    /// </summary>
    private static string HashOtp(string otp, string salt)
    {
        // Combine salt and OTP
        var combined = salt + otp;

        // Hash using SHA256
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));

        // Return as Base64 string
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Performs constant-time string comparison.
    /// This prevents timing attacks where an attacker measures response times
    /// to deduce correct characters.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        // First, convert to bytes
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);

        // Use CryptographicOperations for constant-time comparison
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }

    /// <summary>
    /// Masks an email address for safe logging.
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
