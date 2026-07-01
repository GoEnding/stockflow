using Microsoft.AspNetCore.Mvc;
using StockFlow.Core.Dtos;
using StockFlow.Core.Services;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _products;

    public ProductsController(ProductService products) => _products = products;

    [HttpGet]
    public IActionResult GetAll() => Ok(_products.GetAll());

    [HttpPost]
    public IActionResult Create([FromBody] CreateProductRequest req)
    {
        if (!_products.Create(req))
            return BadRequest(new { message = "등록에 실패했습니다. 공급처 코드를 확인하세요." });
        return Ok(new { message = "등록되었습니다." });
    }

    [HttpPut("{code}")]
    public IActionResult Update(string code, [FromBody] UpdateProductRequest req)
    {
        if (!_products.Update(code, req))
            return NotFound(new { message = "상품을 찾지 못했습니다." });
        return Ok(new { message = "수정되었습니다." });
    }
}
