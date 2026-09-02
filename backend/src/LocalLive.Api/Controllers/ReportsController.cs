using FluentValidation;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalLive.Api.Controllers;

[Route("api/reports")]
[Authorize]
public class ReportsController : ApiControllerBase
{
    private readonly IReportService _service;
    private readonly IValidator<CreateReportRequest> _validator;

    public ReportsController(IReportService service, IValidator<CreateReportRequest> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request)
    {
        var validation = await _validator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation);
        }
        return HandleResult(await _service.CreateAsync(RequireUserId(), request));
    }
}
