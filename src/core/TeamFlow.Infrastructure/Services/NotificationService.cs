using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Infrastructure.Services;

public sealed class NotificationService(
    IInAppNotificationRepository notificationRepository,
    IEmailOutboxRepository emailOutboxRepository,
    INotificationPreferenceRepository preferenceRepository,
    IUserRepository userRepository,
    IPublisher publisher) : INotificationService
{
    public async Task CreateNotificationAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string? body,
        Guid? referenceId,
        string? referenceType,
        Guid? projectId,
        CancellationToken ct = default)
    {
        var pref = await preferenceRepository.GetByUserAndTypeAsync(recipientId, type, ct);
        var inAppEnabled = pref?.InAppEnabled ?? true;
        var emailEnabled = pref?.EmailEnabled ?? true;

        var typeString = type.ToString();

        if (inAppEnabled)
        {
            var notification = new InAppNotification
            {
                RecipientId = recipientId,
                Type = typeString,
                Title = title,
                Body = body,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                IsRead = false
            };

            await notificationRepository.AddAsync(notification, ct);

            await publisher.Publish(new NotificationCreatedDomainEvent(
                notification.Id, recipientId, typeString, title), ct);
        }

        if (emailEnabled)
        {
            var user = await userRepository.GetByIdAsync(recipientId, ct);
            if (user?.Email is not null)
            {
                var emailEntry = new EmailOutbox
                {
                    RecipientEmail = user.Email,
                    RecipientId = recipientId,
                    TemplateType = typeString,
                    Subject = title,
                    BodyJson = System.Text.Json.JsonDocument.Parse(
                        System.Text.Json.JsonSerializer.Serialize(new { title, body, referenceId, referenceType })),
                    Status = EmailStatus.Pending,
                    NextRetryAt = DateTime.UtcNow
                };

                await emailOutboxRepository.AddAsync(emailEntry, ct);
            }
        }
    }
}
