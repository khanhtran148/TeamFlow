using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.ProjectMemberships;

namespace TeamFlow.Application.Features.ProjectMemberships.ListProjectMemberships;

public sealed record ListProjectMembershipsQuery(Guid ProjectId) : IRequest<Result<IEnumerable<ProjectMembershipDto>>>;
