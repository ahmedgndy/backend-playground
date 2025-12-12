using Notificatins_Clean_Arc_SingleR.Domain.Entities;

namespace Notificatins_Clean_Arc_SingleR.Application.Interfaces;

public interface INotificationStore
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Notification>> GetLatestAsync(string userId, int take, CancellationToken cancellationToken = default);
}
