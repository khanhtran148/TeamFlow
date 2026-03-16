using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common.Builders;

public sealed class OrganizationMemberBuilder
{
    private Guid _organizationId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private OrgRole _role = OrgRole.Member;

    public static OrganizationMemberBuilder New() => new();

    public OrganizationMemberBuilder WithOrganization(Guid organizationId)
    {
        _organizationId = organizationId;
        return this;
    }

    public OrganizationMemberBuilder WithUser(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public OrganizationMemberBuilder WithRole(OrgRole role)
    {
        _role = role;
        return this;
    }

    public OrganizationMember Build() => new()
    {
        OrganizationId = _organizationId,
        UserId = _userId,
        Role = _role
    };
}
