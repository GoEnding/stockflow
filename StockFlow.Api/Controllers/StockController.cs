using Microsoft.AspNetCore.Mvc;
using StockFlow.Core.Dtos;
using StockFlow.Core.Services;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly StockService _stock;

    public StockController(StockService stock) => _stock = stock;

    [HttpGet("movements")]
    public IActionResult GetMovements() => Ok(_stock.GetMovements());

    [HttpPost("receive")]
    public IActionResult Receive([FromBody] StockRequest req)
    {
        var result = _stock.Receive(req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("issue")]
    public IActionResult Issue([FromBody] StockRequest req)
    {
        var result = _stock.Issue(req);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}
