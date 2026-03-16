using FluentValidation;

namespace TeamFlow.Application.Features.Notifications.UpdatePreferences;

public sealed class UpdatePreferencesValidator : AbstractValidator<UpdatePreferencesCommand>
{
    public UpdatePreferencesValidator()
    {
        RuleFor(x => x.Preferences).NotNull().NotEmpty();
        RuleForEach(x => x.Preferences).ChildRules(pref =>
        {
            pref.RuleFor(p => p.NotificationType).NotEmpty();
        });
    }
}
