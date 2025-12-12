using System.Collections.Concurrent;
using Notificatins_Clean_Arc_SingleR.Application.Interfaces;
using Notificatins_Clean_Arc_SingleR.Domain.Entities;

namespace Notificatins_Clean_Arc_SingleR.Infrastructure.Persistence;

public class InMemoryNotificationStore : INotificationStore
{
    private readonly ConcurrentDictionary<string, List<Notification>> _store = new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var list = _store.GetOrAdd(notification.UserId, _ => new List<Notification>());
        lock (list)
        {
            list.Add(notification);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Notification>> GetLatestAsync(string userId, int take, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(userId, out var list))
        {
            lock (list)
            {
                var items = list
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .Take(take)
                    .ToArray();
                return Task.FromResult<IReadOnlyCollection<Notification>>(items);
            }
        }

        return Task.FromResult<IReadOnlyCollection<Notification>>(Array.Empty<Notification>());
    }
}
