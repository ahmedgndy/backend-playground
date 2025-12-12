using Notificatins_Clean_Arc_SingleR.Api.Hubs;
using Notificatins_Clean_Arc_SingleR.Application.Interfaces;
using Notificatins_Clean_Arc_SingleR.Application.Services;
using Notificatins_Clean_Arc_SingleR.Infrastructure.Persistence;
using Notificatins_Clean_Arc_SingleR.Infrastructure.Realtime;

var builder = WebApplication.CreateBuilder(args);

// MVC + SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Docs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Clean-ish DI wiring
builder.Services.AddSingleton<INotificationStore, InMemoryNotificationStore>();
builder.Services.AddSingleton<IOnlineUserTracker, InMemoryOnlineUserTracker>();
builder.Services.AddSingleton<INotificationDispatcher, SignalRNotificationDispatcher>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
