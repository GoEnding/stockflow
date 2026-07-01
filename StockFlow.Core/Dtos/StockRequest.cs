namespace StockFlow.Core.Dtos;

public class StockRequest
{
    public string ProductCode { get; set; } = "";
    public string WarehouseCode { get; set; } = "";
    public int Qty { get; set; }
}
