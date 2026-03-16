using FluentValidation;

namespace TeamFlow.Application.Features.Admin.TransferOrgOwnership;

public sealed class AdminTransferOrgOwnershipValidator : AbstractValidator<AdminTransferOrgOwnershipCommand>
{
    public AdminTransferOrgOwnershipValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty().WithMessage("OrgId is required.");
        RuleFor(x => x.NewOwnerUserId).NotEmpty().WithMessage("NewOwnerUserId is required.");
    }
}
