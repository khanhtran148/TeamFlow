using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Releases.GetReleaseDetail;

public sealed record GetReleaseDetailQuery(Guid ReleaseId) : IRequest<Result<ReleaseDetailDto>>;
