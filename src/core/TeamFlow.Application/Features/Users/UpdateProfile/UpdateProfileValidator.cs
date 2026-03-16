using FluentValidation;

namespace TeamFlow.Application.Features.Users.UpdateProfile;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048)
            .When(x => x.AvatarUrl is not null);
    }
}
