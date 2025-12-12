using System.ComponentModel.DataAnnotations;

namespace Notificatins_Clean_Arc_SingleR.Api.Contracts;

public record NotificationCreateRequest(
    [Required] string UserId,
    [Required, MaxLength(500)] string Message
);
