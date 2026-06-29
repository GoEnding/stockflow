namespace StockFlow.Models;

public class InventoryView {
    public string WarehouseName { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int Qty { get; set; }
    public int SafetyStock { get; set; }
    public string Status { get; set; } = "";
}