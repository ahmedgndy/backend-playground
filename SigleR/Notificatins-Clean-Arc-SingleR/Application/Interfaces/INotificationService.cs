using Notificatins_Clean_Arc_SingleR.Domain.Entities;

namespace Notificatins_Clean_Arc_SingleR.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyCollection<Notification>> GetLatestAsync(string userId, int take = 4, CancellationToken cancellationToken = default);
    Task<Notification> PublishAsync(string userId, string message, CancellationToken cancellationToken = default);
}
