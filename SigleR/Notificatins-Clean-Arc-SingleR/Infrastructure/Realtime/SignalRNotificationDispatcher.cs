using Microsoft.AspNetCore.SignalR;
using Notificatins_Clean_Arc_SingleR.Application.Interfaces;
using Notificatins_Clean_Arc_SingleR.Domain.Entities;
using Notificatins_Clean_Arc_SingleR.Api.Hubs;

namespace Notificatins_Clean_Arc_SingleR.Infrastructure.Realtime;

public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public SignalRNotificationDispatcher(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task DispatchAsync(Notification notification, IEnumerable<string> connectionIds, CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients.Clients(connectionIds).SendAsync(
            "ReceiveNotification",
            new
            {
                notification.Id,
                notification.Message,
                notification.UserId,
                notification.CreatedAtUtc
            },
            cancellationToken);
    }
}
