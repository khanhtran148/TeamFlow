using FluentValidation;

namespace TeamFlow.Application.Features.Admin.ChangeUserStatus;

public sealed class AdminChangeUserStatusValidator : AbstractValidator<AdminChangeUserStatusCommand>
{
    public AdminChangeUserStatusValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
    }
}
