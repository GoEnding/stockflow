namespace StockFlow.Models;

public class MovementView {
    public string ProductName { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string MovementType { get; set; } = "";
    public int Qty { get; set; }
    public DateTime CreatedAt { get; set; }
}