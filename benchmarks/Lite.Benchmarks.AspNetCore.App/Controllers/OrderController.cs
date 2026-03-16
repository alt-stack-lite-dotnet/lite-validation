using Lite.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Lite.Benchmarks.AspNetCore.App.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IValidator<CreateOrderRequest> _validator;

    public OrderController(IValidator<CreateOrderRequest> validator)
    {
        _validator = validator;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateOrderRequest request)
    {
        var result = _validator.Validate(request);
        if (!result.IsSuccess)
            return BadRequest(result.Errors);
        return Ok();
    }
}
