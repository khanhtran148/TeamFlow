using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.UpdateColumnsConfig;

public sealed record UpdateColumnsConfigCommand(
    Guid SessionId,
    JsonDocument ColumnsConfig
) : IRequest<Result>;
