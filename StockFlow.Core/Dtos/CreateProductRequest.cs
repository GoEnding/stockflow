namespace StockFlow.Core.Dtos;

public class CreateProductRequest
{
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int UnitPrice { get; set; }
    public int SafetyStock { get; set; }
    public string? SupplierCode { get; set; }
}
