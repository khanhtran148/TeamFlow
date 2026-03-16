using FluentValidation;

namespace TeamFlow.Application.Features.Invitations.Create;

public sealed class CreateInvitationValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => x.Email is not null)
            .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Role must be a valid OrgRole.");
    }
}
