using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Features.Dashboard.GetReleaseProgress;

public sealed record GetReleaseProgressQuery(
    Guid ReleaseId,
    Guid ProjectId
) : IRequest<Result<ReleaseProgressDto>>;
