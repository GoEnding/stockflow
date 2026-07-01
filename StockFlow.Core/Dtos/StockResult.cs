namespace StockFlow.Core.Dtos;

public class StockResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Warning { get; set; }
}
