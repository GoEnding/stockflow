using Dapper;
using StockFlow.Core.Data;
using StockFlow.Core.Dtos;
using StockFlow.Core.Models;

namespace StockFlow.Core.Services;

public class StockService
{
    private readonly DbConnectionFactory _db;

    public StockService(DbConnectionFactory db) => _db = db;

    public IEnumerable<MovementView> GetMovements()
    {
        using var conn = _db.Create();
        return conn.Query<MovementView>(@"
            SELECT w.warehouse_name, p.product_name,
                   CASE WHEN sm.movement_type = 'IN' THEN N'입고' ELSE N'출고' END AS movement_type,
                   sm.qty, sm.created_at
            FROM stock_movements sm
            JOIN warehouses w ON w.warehouse_id = sm.warehouse_id
            JOIN products   p ON p.product_id   = sm.product_id
            ORDER BY sm.created_at DESC");
    }

    public StockResult Receive(StockRequest req)
    {
        var ids = ResolveIds(req);
        if (ids.Error is not null) return ids.Error;

        using var conn = _db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            conn.Execute(
                @"INSERT INTO stock_movements (product_id, warehouse_id, movement_type, qty)
                  VALUES (@productId, @warehouseId, 'IN', @qty)",
                new { productId = ids.ProductId, warehouseId = ids.WarehouseId, req.Qty },
                transaction: tx);

            conn.Execute(
                @"UPDATE inventory SET qty = qty + @qty, updated_at = SYSDATETIME()
                  WHERE product_id = @productId AND warehouse_id = @warehouseId;
                  IF @@ROWCOUNT = 0
                      INSERT INTO inventory (product_id, warehouse_id, qty)
                      VALUES (@productId, @warehouseId, @qty);",
                new { productId = ids.ProductId, warehouseId = ids.WarehouseId, req.Qty },
                transaction: tx);

            tx.Commit();
            return new StockResult { Success = true, Message = "입고되었습니다." };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public StockResult Issue(StockRequest req)
    {
        var ids = ResolveIds(req);
        if (ids.Error is not null) return ids.Error;

        using var conn = _db.Create();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            int current = conn.QuerySingleOrDefault<int?>(
                "SELECT qty FROM inventory WHERE product_id = @productId AND warehouse_id = @warehouseId",
                new { productId = ids.ProductId, warehouseId = ids.WarehouseId },
                transaction: tx) ?? 0;

            if (current < req.Qty)
            {
                tx.Rollback();
                return new StockResult
                {
                    Success = false,
                    Message = $"재고 부족: 현재 {current}개, 요청 {req.Qty}개"
                };
            }

            conn.Execute(
                @"INSERT INTO stock_movements (product_id, warehouse_id, movement_type, qty)
                  VALUES (@productId, @warehouseId, 'OUT', @qty)",
                new { productId = ids.ProductId, warehouseId = ids.WarehouseId, req.Qty },
                transaction: tx);

            conn.Execute(
                @"UPDATE inventory SET qty = qty - @qty, updated_at = SYSDATETIME()
                  WHERE product_id = @productId AND warehouse_id = @warehouseId",
                new { productId = ids.ProductId, warehouseId = ids.WarehouseId, req.Qty },
                transaction: tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        var item = conn.QuerySingleOrDefault<InventoryView>(
            @"SELECT w.warehouse_name, p.product_name, i.qty, p.safety_stock,
                     CASE WHEN i.qty < p.safety_stock THEN N'경고' ELSE N'정상' END AS status
              FROM inventory i
              JOIN warehouses w ON w.warehouse_id = i.warehouse_id
              JOIN products   p ON p.product_id   = i.product_id
              WHERE i.product_id = @productId AND i.warehouse_id = @warehouseId",
            new { productId = ids.ProductId, warehouseId = ids.WarehouseId });

        string? warning = item is not null && item.Qty < item.SafetyStock
            ? $"⚠ {item.ProductName} 안전재고 미만! 현재 {item.Qty} < 기준 {item.SafetyStock}"
            : null;

        return new StockResult { Success = true, Message = "출고되었습니다.", Warning = warning };
    }

    private (int ProductId, int WarehouseId, StockResult? Error) ResolveIds(StockRequest req)
    {
        if (req.Qty <= 0)
            return (0, 0, new StockResult { Success = false, Message = "1 이상의 수량을 입력하세요." });

        using var conn = _db.Create();
        int? productId = conn.QuerySingleOrDefault<int?>(
            "SELECT product_id FROM products WHERE product_code = @code",
            new { code = req.ProductCode });
        if (productId is null)
            return (0, 0, new StockResult { Success = false, Message = "상품을 찾지 못했습니다." });

        int? warehouseId = conn.QuerySingleOrDefault<int?>(
            "SELECT warehouse_id FROM warehouses WHERE warehouse_code = @code",
            new { code = req.WarehouseCode });
        if (warehouseId is null)
            return (0, 0, new StockResult { Success = false, Message = "창고를 찾지 못했습니다." });

        return (productId.Value, warehouseId.Value, null);
    }
}
