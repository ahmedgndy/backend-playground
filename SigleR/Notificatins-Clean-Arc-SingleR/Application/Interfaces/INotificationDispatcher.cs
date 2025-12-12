using Notificatins_Clean_Arc_SingleR.Domain.Entities;

namespace Notificatins_Clean_Arc_SingleR.Application.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(Notification notification, IEnumerable<string> connectionIds, CancellationToken cancellationToken = default);
}
