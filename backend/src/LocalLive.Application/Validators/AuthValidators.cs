using FluentValidation;
using LocalLive.Application.Features.Auth;

namespace LocalLive.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2).MaximumLength(120);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.RegisterAs)
            .Must(v => v is null or "customer" or "customer" or "shop" or "shop_owner")
            .WithMessage("RegisterAs must be 'customer' or 'shop'.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
