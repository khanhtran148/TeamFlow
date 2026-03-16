using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class InAppNotificationBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private Guid _recipientId = Guid.NewGuid();
    private string _type = "mention";
    private string _title = F.Lorem.Sentence(4);
    private string? _body = F.Lorem.Sentence(8);
    private Guid? _referenceId = Guid.NewGuid();
    private string? _referenceType = "Comment";
    private bool _isRead;

    public static InAppNotificationBuilder New() => new();

    public InAppNotificationBuilder WithRecipient(Guid recipientId) { _recipientId = recipientId; return this; }
    public InAppNotificationBuilder WithType(string type) { _type = type; return this; }
    public InAppNotificationBuilder WithTitle(string title) { _title = title; return this; }
    public InAppNotificationBuilder WithBody(string body) { _body = body; return this; }
    public InAppNotificationBuilder WithReference(Guid referenceId, string referenceType)
    {
        _referenceId = referenceId;
        _referenceType = referenceType;
        return this;
    }
    public InAppNotificationBuilder Read() { _isRead = true; return this; }

    public InAppNotification Build() => new()
    {
        RecipientId = _recipientId,
        Type = _type,
        Title = _title,
        Body = _body,
        ReferenceId = _referenceId,
        ReferenceType = _referenceType,
        IsRead = _isRead
    };
}
