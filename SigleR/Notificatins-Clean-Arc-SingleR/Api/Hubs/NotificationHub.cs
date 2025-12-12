using Microsoft.AspNetCore.SignalR;
using Notificatins_Clean_Arc_SingleR.Application.Interfaces;

namespace Notificatins_Clean_Arc_SingleR.Api.Hubs;

/// <summary>
/// SignalR hub for delivering notifications in real-time.
/// Clients must connect with a query string parameter <c>userId</c> (e.g. <c>?userId=alice</c>).
/// When connected the hub will send the caller their latest notifications and allow sending notifications to other users.
/// </summary>
public class NotificationHub : Hub
{
    private readonly INotificationService _notificationService;
    private readonly IOnlineUserTracker _tracker;

    /// <summary>
    /// Creates a new instance of <see cref="NotificationHub"/>.
    /// </summary>
    /// <param name="notificationService">Service for storing and publishing notifications.</param>
    /// <param name="tracker">Tracker for online user connections.</param>
    public NotificationHub(INotificationService notificationService, IOnlineUserTracker tracker)
    {
        _notificationService = notificationService;
        _tracker = tracker;
    }

    /// <summary>
    /// Called when a client connects. The <c>userId</c> query parameter is required. Registers the connection and
    /// sends up to the last 4 notifications to the caller as a <c>SeedNotifications</c> message.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var userId = httpContext?.Request.Query["userId"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new HubException("A userId query string parameter is required to connect.");
        }

        await _tracker.RegisterAsync(userId, Context.ConnectionId);

        var latest = await _notificationService.GetLatestAsync(userId, 4);
        if (latest.Count > 0)
        {
            await Clients.Caller.SendAsync("SeedNotifications", latest);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects. Unregisters the connection from the online tracker.
    /// </summary>
    /// <param name="exception">Optional exception that occurred during disconnect.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _tracker.UnregisterAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Server-invokable method to publish a notification to a target user.
    /// If the target user is online, the notification will be pushed immediately.
    /// </summary>
    /// <param name="targetUserId">User id that should receive the notification.</param>
    /// <param name="message">Notification message body.</param>
    /// <returns>A task that represents the publish operation.</returns>
    public Task SendNotification(string targetUserId, string message)
    {
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            throw new HubException("Target user is required.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Message is required.");
        }

        return _notificationService.PublishAsync(targetUserId, message);
    }

    /// <summary>
    /// Simple echo method useful for connectivity checks.
    /// </summary>
    /// <param name="message">Message to echo back to the caller.</param>
    /// <returns>A task that sends the echo back to the calling client.</returns>
    public Task Echo(string message) => Clients.Caller.SendAsync("Echo", message);
}
