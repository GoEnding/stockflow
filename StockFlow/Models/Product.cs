namespace StockFlow.Models;

// products 테이블 한 행을 담는 그릇. (DB 컬럼 1개 = 속성 1개)
public class Product
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int UnitPrice { get; set; }
    public int SafetyStock { get; set; }
    public int? SupplierId { get; set; }   // NULL 가능 컬럼이라 int? (물음표 = nullable)
}
