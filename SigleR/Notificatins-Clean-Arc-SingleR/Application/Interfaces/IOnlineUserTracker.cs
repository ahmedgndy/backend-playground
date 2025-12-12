namespace Notificatins_Clean_Arc_SingleR.Application.Interfaces;

public interface IOnlineUserTracker
{
    Task RegisterAsync(string userId, string connectionId, CancellationToken cancellationToken = default);
    Task UnregisterAsync(string connectionId, CancellationToken cancellationToken = default);
    IReadOnlyCollection<string> GetConnections(string userId);
    IReadOnlyCollection<string> GetOnlineUserIds();
}
