using System.Collections.Concurrent;
using System.Collections.Immutable;
using Notificatins_Clean_Arc_SingleR.Application.Interfaces;

namespace Notificatins_Clean_Arc_SingleR.Infrastructure.Realtime;

public class InMemoryOnlineUserTracker : IOnlineUserTracker
{
    private readonly ConcurrentDictionary<string, string> _connectionToUser = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _userToConnections = new(StringComparer.OrdinalIgnoreCase);

    public Task RegisterAsync(string userId, string connectionId, CancellationToken cancellationToken = default)
    {
        _connectionToUser[connectionId] = userId;
        var set = _userToConnections.GetOrAdd(userId, _ => new HashSet<string>());
        lock (set)
        {
            set.Add(connectionId);
        }
        return Task.CompletedTask;
    }

    public Task UnregisterAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        if (_connectionToUser.TryRemove(connectionId, out var userId))
        {
            if (_userToConnections.TryGetValue(userId, out var set))
            {
                lock (set)
                {
                    set.Remove(connectionId);
                    if (set.Count == 0)
                    {
                        _userToConnections.TryRemove(userId, out _);
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<string> GetConnections(string userId)
    {
        if (_userToConnections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                return set.ToImmutableArray();
            }
        }

        return Array.Empty<string>();
    }

    public IReadOnlyCollection<string> GetOnlineUserIds()
    {
        return _userToConnections.Keys.ToImmutableArray();
    }
}
