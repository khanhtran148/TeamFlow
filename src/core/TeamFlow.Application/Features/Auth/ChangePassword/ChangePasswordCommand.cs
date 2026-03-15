using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Auth.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword
) : IRequest<Result>;
