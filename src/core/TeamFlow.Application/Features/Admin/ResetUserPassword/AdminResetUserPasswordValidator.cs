using FluentValidation;

namespace TeamFlow.Application.Features.Admin.ResetUserPassword;

public sealed class AdminResetUserPasswordValidator : AbstractValidator<AdminResetUserPasswordCommand>
{
    public AdminResetUserPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("New password must be at least 8 characters.");
    }
}
