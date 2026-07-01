namespace StockFlow.Core.Models;

public class MovementView
{
    public string WarehouseName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string MovementType { get; set; } = "";
    public int Qty { get; set; }
    public DateTime CreatedAt { get; set; }
}
