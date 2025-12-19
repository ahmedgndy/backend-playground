using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Payment.Configuration;
using Payment.Data;
using Payment.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

// Database - minimal storage for StripeCustomerId + Plan only
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IStripeService, StripeService>();

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Stripe Payment API",
        Version = "v1",
        Description = "Serverless Stripe payment integration - Stripe is the single source of truth"
    });
});

// CORS for test UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stripe Payment API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseStaticFiles();

app.MapControllers();

// Serve test UI at root
app.MapFallbackToFile("index.html");

// Auto-migrate database in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
