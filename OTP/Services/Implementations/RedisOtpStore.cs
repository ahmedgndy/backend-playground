// =============================================================================
// REDIS OTP STORE - Optional Ephemeral Implementation
// =============================================================================
// This implementation stores OTPs in Redis - an in-memory data store.
// Benefits of Redis for OTP storage:
// - Automatic expiration (TTL) - Redis deletes keys automatically
// - Very fast - all operations are in-memory
// - No database cleanup needed
// 
// Use this in production if you have Redis available.
// For learning, the SQLite store is simpler to set up.
// =============================================================================

using System.Text.Json;
using OTP.Models;
using OTP.Services.Interfaces;
using StackExchange.Redis;

namespace OTP.Services.Implementations;

/// <summary>
/// Redis-backed OTP store for ephemeral OTP storage.
/// OTPs automatically expire based on Redis TTL (Time To Live).
/// </summary>
public class RedisOtpStore : IOtpStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisOtpStore> _logger;

    // Prefix for all OTP keys in Redis
    private const string KeyPrefix = "otp:";

    public RedisOtpStore(IConnectionMultiplexer redis, ILogger<RedisOtpStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveAsync(OtpRecord record)
    {
        var db = _redis.GetDatabase();

        // Create a unique key for this OTP
        var key = $"{KeyPrefix}{record.Email.ToLower()}";

        // Serialize the record to JSON
        var json = JsonSerializer.Serialize(record);

        // Calculate TTL (Time To Live) from expiration
        var ttl = record.ExpiresAt - DateTime.UtcNow;

        // Store in Redis with automatic expiration
        await db.StringSetAsync(key, json, ttl);

        _logger.LogInformation(
            "OTP stored in Redis for email: {Email}, TTL: {TTL}",
            MaskEmail(record.Email),
            ttl);
    }

    /// <inheritdoc />
    public async Task<OtpRecord?> GetActiveOtpAsync(string email)
    {
        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{email.ToLower()}";

        var json = await db.StringGetAsync(key);

        if (json.IsNullOrEmpty)
            return null;

        var record = JsonSerializer.Deserialize<OtpRecord>(json!);

        // Additional checks (Redis TTL should handle expiration, but be safe)
        if (record == null || record.IsUsed || record.AttemptCount >= record.MaxAttempts)
            return null;

        return record;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(OtpRecord record)
    {
        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{record.Email.ToLower()}";

        // Get remaining TTL
        var ttl = await db.KeyTimeToLiveAsync(key);

        if (ttl.HasValue && ttl.Value > TimeSpan.Zero)
        {
            var json = JsonSerializer.Serialize(record);
            await db.StringSetAsync(key, json, ttl.Value);
        }
    }

    /// <inheritdoc />
    public async Task InvalidateAllAsync(string email)
    {
        var db = _redis.GetDatabase();
        var key = $"{KeyPrefix}{email.ToLower()}";

        // Simply delete the key
        await db.KeyDeleteAsync(key);

        _logger.LogInformation(
            "Invalidated OTP in Redis for email: {Email}",
            MaskEmail(email));
    }

    /// <inheritdoc />
    public Task CleanupExpiredAsync()
    {
        // Redis automatically handles expiration via TTL!
        // No cleanup needed - this is one of the benefits of Redis
        _logger.LogDebug("Redis OTP cleanup called - no action needed (TTL handles expiration)");
        return Task.CompletedTask;
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
