using FluentValidation;
using LocalLive.Application.Features.Requests;
using LocalLive.Application.Features.Reports;

namespace LocalLive.Application.Validators;

public class CreateRequestRequestValidator : AbstractValidator<CreateRequestRequest>
{
    public CreateRequestRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.TtlMinutes).InclusiveBetween(5, 120);
    }
}

public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MinimumLength(5).MaximumLength(1000);
    }
}
