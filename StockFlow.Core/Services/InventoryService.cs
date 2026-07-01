using Dapper;
using StockFlow.Core.Data;
using StockFlow.Core.Models;

namespace StockFlow.Core.Services;

public class InventoryService
{
    private readonly DbConnectionFactory _db;

    private const string InventorySql = @"
        SELECT w.warehouse_name, p.product_name, i.qty, p.safety_stock,
               CASE WHEN i.qty < p.safety_stock THEN N'경고' ELSE N'정상' END AS status
        FROM inventory i
        JOIN warehouses w ON w.warehouse_id = i.warehouse_id
        JOIN products   p ON p.product_id   = i.product_id";

    public InventoryService(DbConnectionFactory db) => _db = db;

    public IEnumerable<InventoryView> GetAll()
    {
        using var conn = _db.Create();
        return conn.Query<InventoryView>(InventorySql);
    }

    public IEnumerable<InventoryView> GetLowStock()
    {
        using var conn = _db.Create();
        return conn.Query<InventoryView>(InventorySql + " WHERE i.qty < p.safety_stock");
    }
}
