// =============================================================================
// API RESPONSE - Standardized Response Format
// =============================================================================
// A consistent response format makes it easier for frontend developers
// to handle API responses. Every response has the same structure.
// =============================================================================

namespace OTP.Models.DTOs;

/// <summary>
/// Standard API response wrapper.
/// Provides a consistent format for all API responses.
/// </summary>
/// <typeparam name="T">The type of data being returned (if any).</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// A human-readable message describing the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The actual data payload (optional).
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// List of validation or error details (optional).
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful response with optional data.
    /// </summary>
    public static ApiResponse<T> Ok(string message, T? data = default)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Creates an error response with a message.
    /// </summary>
    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}

/// <summary>
/// Non-generic version for responses without data.
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response.
    /// </summary>
    public static ApiResponse Ok(string message)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public new static ApiResponse Fail(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}
