using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common.Builders;

public sealed class InvitationBuilder
{
    private Guid _organizationId = Guid.NewGuid();
    private Guid _invitedByUserId = Guid.NewGuid();
    private string? _email = null;
    private OrgRole _role = OrgRole.Member;
    private string _tokenHash = "test-token-hash-" + Guid.NewGuid().ToString("N");
    private InviteStatus _status = InviteStatus.Pending;
    private DateTime _expiresAt = DateTime.UtcNow.AddDays(7);

    public static InvitationBuilder New() => new();

    public InvitationBuilder WithOrganization(Guid organizationId)
    {
        _organizationId = organizationId;
        return this;
    }

    public InvitationBuilder WithInvitedBy(Guid invitedByUserId)
    {
        _invitedByUserId = invitedByUserId;
        return this;
    }

    public InvitationBuilder WithEmail(string? email)
    {
        _email = email;
        return this;
    }

    public InvitationBuilder WithRole(OrgRole role)
    {
        _role = role;
        return this;
    }

    public InvitationBuilder WithTokenHash(string tokenHash)
    {
        _tokenHash = tokenHash;
        return this;
    }

    public InvitationBuilder WithStatus(InviteStatus status)
    {
        _status = status;
        return this;
    }

    public InvitationBuilder WithExpiresAt(DateTime expiresAt)
    {
        _expiresAt = expiresAt;
        return this;
    }

    public Invitation Build() => new()
    {
        OrganizationId = _organizationId,
        InvitedByUserId = _invitedByUserId,
        Email = _email,
        Role = _role,
        TokenHash = _tokenHash,
        Status = _status,
        ExpiresAt = _expiresAt
    };
}
