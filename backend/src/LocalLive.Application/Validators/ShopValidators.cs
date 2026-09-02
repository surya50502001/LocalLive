using FluentValidation;
using LocalLive.Application.Features.Shops;

namespace LocalLive.Application.Validators;

public class CreateShopRequestValidator : AbstractValidator<CreateShopRequest>
{
    public CreateShopRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Address).NotEmpty().MinimumLength(5).MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.CategoryIds).NotEmpty().Must(x => x.Count <= 10)
            .WithMessage("A shop can have at most 10 categories.");
    }
}

public class UpdateShopRequestValidator : AbstractValidator<UpdateShopRequest>
{
    public UpdateShopRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Address).NotEmpty().MinimumLength(5).MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.CategoryIds).NotEmpty().Must(x => x.Count <= 10);
    }
}
