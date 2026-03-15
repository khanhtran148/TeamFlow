using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common.Builders;

public sealed class ProjectMembershipBuilder
{
    private Guid _projectId = Guid.NewGuid();
    private Guid _memberId = Guid.NewGuid();
    private string _memberType = "User";
    private ProjectRole _role = ProjectRole.Developer;

    public static ProjectMembershipBuilder New() => new();

    public ProjectMembershipBuilder WithProject(Guid projectId) { _projectId = projectId; return this; }
    public ProjectMembershipBuilder WithMember(Guid memberId) { _memberId = memberId; return this; }
    public ProjectMembershipBuilder WithMemberType(string memberType) { _memberType = memberType; return this; }
    public ProjectMembershipBuilder WithRole(ProjectRole role) { _role = role; return this; }
    public ProjectMembershipBuilder AsProductOwner() { _role = ProjectRole.ProductOwner; return this; }
    public ProjectMembershipBuilder AsViewer() { _role = ProjectRole.Viewer; return this; }

    public ProjectMembership Build() => new()
    {
        ProjectId = _projectId,
        MemberId = _memberId,
        MemberType = _memberType,
        Role = _role
    };
}
