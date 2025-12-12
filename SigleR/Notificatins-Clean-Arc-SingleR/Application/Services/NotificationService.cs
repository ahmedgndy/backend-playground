using Notificatins_Clean_Arc_SingleR.Application.Interfaces;
using Notificatins_Clean_Arc_SingleR.Domain.Entities;

namespace Notificatins_Clean_Arc_SingleR.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationStore _store;
    private readonly IOnlineUserTracker _tracker;
    private readonly INotificationDispatcher _dispatcher;

    public NotificationService(
        INotificationStore store,
        IOnlineUserTracker tracker,
        INotificationDispatcher dispatcher)
    {
        _store = store;
        _tracker = tracker;
        _dispatcher = dispatcher;
    }

    public async Task<IReadOnlyCollection<Notification>> GetLatestAsync(string userId, int take = 4, CancellationToken cancellationToken = default)
    {
        return await _store.GetLatestAsync(userId, take, cancellationToken);
    }

    public async Task<Notification> PublishAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Message = message
        };

        await _store.AddAsync(notification, cancellationToken);

        var connectionIds = _tracker.GetConnections(userId);
        if (connectionIds.Count > 0)
        {
            await _dispatcher.DispatchAsync(notification, connectionIds, cancellationToken);
        }

        return notification;
    }
}
