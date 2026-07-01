namespace StockFlow.Core.Models;

public class Product
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int UnitPrice { get; set; }
    public int SafetyStock { get; set; }
    public int? SupplierId { get; set; }
}
