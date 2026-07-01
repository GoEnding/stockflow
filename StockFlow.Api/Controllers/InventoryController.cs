using Microsoft.AspNetCore.Mvc;
using StockFlow.Core.Services;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventory;

    public InventoryController(InventoryService inventory) => _inventory = inventory;

    [HttpGet]
    public IActionResult GetAll() => Ok(_inventory.GetAll());

    [HttpGet("low-stock")]
    public IActionResult GetLowStock() => Ok(_inventory.GetLowStock());
}
