// =============================================================================
// ASP.NET CORE OTP AUTHENTICATION DEMO
// =============================================================================
// This is the main entry point for the application.
// Here we configure all services, middleware, and the request pipeline.
//
// ARCHITECTURE OVERVIEW:
// - Controllers handle HTTP requests
// - Services contain business logic
// - Data layer handles database operations
// - Interfaces allow swapping implementations (Dependency Injection)
// =============================================================================

using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OTP.Data;
using OTP.Services.Implementations;
using OTP.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// CONFIGURE SERVICES (Dependency Injection Container)
// =============================================================================

// Add controller support (we're using traditional controllers, not minimal APIs)
builder.Services.AddControllers();

// Add Swagger/OpenAPI for API documentation and testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "OTP Authentication API",
        Version = "v1",
        Description = "A demo API for learning OTP-based email verification"
    });
});

// -----------------------------------------------------------------------------
// DATABASE CONFIGURATION
// -----------------------------------------------------------------------------
// Using SQLite for simplicity - it's just a file!
// The database file will be created automatically in the project folder.
builder.Services.AddDbContext<OtpDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=otp.db");
});

// -----------------------------------------------------------------------------
// OTP SERVICES CONFIGURATION
// -----------------------------------------------------------------------------

// Register the OTP store (choose one):
// Option 1: Database store (default) - persistent storage using SQLite
builder.Services.AddScoped<IOtpStore, DatabaseOtpStore>();

// Option 2: Redis store (uncomment to use) - ephemeral in-memory storage
// Requires Redis server running and configured connection string
// builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
//     ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
// builder.Services.AddScoped<IOtpStore, RedisOtpStore>();

// Register the OTP service
builder.Services.AddScoped<IOtpService, OtpService>();

// -----------------------------------------------------------------------------
// EMAIL SERVICE CONFIGURATION
// -----------------------------------------------------------------------------

// Choose your email provider based on configuration
var emailProvider = builder.Configuration["Email:Provider"] ?? "Console";

switch (emailProvider.ToLower())
{
    case "sendgrid":
        // Use SendGrid API for production
        builder.Services.AddScoped<IEmailService, SendGridEmailService>();
        break;

    case "smtp":
        // Use SMTP (Gmail, Outlook, etc.)
        builder.Services.AddScoped<IEmailService, SmtpEmailService>();
        break;

    case "console":
    default:
        // Development mode - just log to console
        builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
        break;
}

// -----------------------------------------------------------------------------
// RATE LIMITING CONFIGURATION
// -----------------------------------------------------------------------------
// Rate limiting prevents abuse by limiting how many requests can be made.
// This is crucial for OTP endpoints to prevent brute-force attacks.

builder.Services.AddRateLimiter(options =>
{
    // Global rejection response
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Rate limit for OTP requests (send OTP)
    // More restrictive: sending emails costs money and could be abused
    options.AddPolicy("otp-request", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            // Partition by IP address
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,           // Max 5 requests
                Window = TimeSpan.FromMinutes(1), // Per minute
                SegmentsPerWindow = 2,     // 2 segments for smoother limiting
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0             // No queuing, immediate rejection
            }));

    // Rate limit for OTP verification
    // Prevents brute-force attacks on OTP codes
    options.AddPolicy("otp-verify", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,          // Max 10 verification attempts
                Window = TimeSpan.FromMinutes(1), // Per minute
                SegmentsPerWindow = 2,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// -----------------------------------------------------------------------------
// CORS CONFIGURATION (for frontend)
// -----------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// =============================================================================
// CONFIGURE MIDDLEWARE PIPELINE
// =============================================================================

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OTP Authentication API v1");
    });
}

// Enable HTTPS redirection (commented out for local development)
// app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

// Enable rate limiting
app.UseRateLimiter();

// Serve static files (for the frontend)
app.UseDefaultFiles(); // Serves index.html by default
app.UseStaticFiles();  // Serves files from wwwroot

// Map controller routes
app.MapControllers();

// =============================================================================
// DATABASE INITIALIZATION
// =============================================================================
// Ensure the database is created on startup.
// In production, you would use migrations instead.

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OtpDbContext>();

    // Create database if it doesn't exist
    dbContext.Database.EnsureCreated();

    // Log database location
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Database initialized. Using SQLite at: otp.db");
}

// =============================================================================
// START THE APPLICATION
// =============================================================================

app.Logger.LogInformation("==============================================");
app.Logger.LogInformation("  OTP Authentication Demo is starting...");
app.Logger.LogInformation("  Swagger UI: http://localhost:5000/swagger");
app.Logger.LogInformation("  Frontend:   http://localhost:5000/");
app.Logger.LogInformation("==============================================");

app.Run();
