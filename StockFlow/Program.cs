using Microsoft.Data.SqlClient;
using Dapper;
using StockFlow.Models;

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// DB 접속 정보. (.\SQLEXPRESS = 로컬 SQL Server Express, Windows 인증)
const string connectionString =
    @"Server=.\SQLEXPRESS;Database=StockFlow;Trusted_Connection=True;TrustServerCertificate=True;";

Console.WriteLine("=== StockFlow 재고관리 ===");

while (true)
{
    Console.WriteLine();
    Console.WriteLine("1. 상품 조회");
    Console.WriteLine("2. 상품 등록");
    Console.WriteLine("3. 상품 수정");
    Console.WriteLine("4. 재고 조회");
    Console.WriteLine("5. 입고");
    Console.WriteLine("6. 출고");
    Console.WriteLine("7. 낮은 재고 조회");
    Console.WriteLine("8. 입출고 내역 조회");
    Console.WriteLine("0. 종료");
    Console.Write("메뉴를 선택하세요: ");
    string choice = Console.ReadLine() ?? "";

    // 각 기능을 try로 감싸서, 한 기능에서 에러가 나도 프로그램이 죽지 않고 메뉴로 복귀
    try
    {
        switch (choice)
        {
            case "1": ListProducts(); break;
            case "2": AddProduct(); break;
            case "3": UpdateProduct(); break;
            case "4": ListInventory(); break;
            case "5": ReceiveStock(); break;
            case "6": IssueStock(); break;
            case "7": ListLowStock(); break;
            case "8": ListMovements(); break;
            case "0": return;
            default:
                Console.WriteLine("잘못된 선택입니다.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"오류가 발생했습니다: {ex.Message}");
    }
}

void ListProducts()
{
    using var conn = new SqlConnection(connectionString);
    var products = conn.Query<Product>(
        "SELECT product_id, product_code, product_name, unit_price, safety_stock, supplier_id FROM products");

    foreach (var p in products)
    {
        Console.WriteLine($"{p.ProductId} {p.ProductCode} {p.ProductName} {p.UnitPrice}원 | 안전재고 {p.SafetyStock}");
    }
}

void AddProduct()
{
    using var conn = new SqlConnection(connectionString);
    Console.Write("상품 코드를 입력하세요: ");
    string productCode = Console.ReadLine() ?? "";
    Console.Write("상품 이름을 입력하세요: ");
    string productName = Console.ReadLine() ?? "";

    Console.Write("상품 가격을 입력하세요: ");
    if (!int.TryParse(Console.ReadLine(), out int unitPrice))
    {
        Console.WriteLine("숫자만 입력하세요.");
        return;
    }

    Console.Write("안전재고를 입력하세요: ");
    if (!int.TryParse(Console.ReadLine(), out int safetyStock))
    {
        Console.WriteLine("숫자만 입력하세요.");
        return;
    }

    Console.Write("공급처 코드를 입력하세요: ");
    string supplierCode = Console.ReadLine() ?? "";

    int rows = conn.Execute(
        @"INSERT INTO products (product_code, product_name, unit_price, safety_stock, supplier_id) 
        VALUES (@productCode, @productName, @unitPrice, @safetyStock, (SELECT supplier_id FROM suppliers WHERE supplier_code = @supplierCode))",
        new { productCode, productName, unitPrice, safetyStock, supplierCode });

    Console.WriteLine(rows > 0 ? "완료되었습니다." : "대상을 찾지 못했습니다.");
}

void UpdateProduct()
{
    using var conn = new SqlConnection(connectionString);
    Console.Write("수정할 상품의 코드를 입력하세요: ");
    string updateProductCode = Console.ReadLine() ?? "";

    Console.Write("수정할 단가의 새로운 값을 입력하세요: ");
    if (!int.TryParse(Console.ReadLine(), out int updateUnitPrice))
    {
        Console.WriteLine("숫자만 입력하세요.");
        return;
    }

    Console.Write("수정할 안전재고의 새로운 값을 입력하세요: ");
    if (!int.TryParse(Console.ReadLine(), out int updateSafetyStock))
    {
        Console.WriteLine("숫자만 입력하세요.");
        return;
    }

    int rows = conn.Execute(
        @"UPDATE products SET unit_price = @updateUnitPrice,
         safety_stock = @updateSafetyStock WHERE product_code = @updateProductCode",
        new { updateUnitPrice, updateSafetyStock, updateProductCode });

    Console.WriteLine(rows > 0 ? "완료되었습니다." : "대상을 찾지 못했습니다.");
}

void ListInventory()
{
    using var conn = new SqlConnection(connectionString);
    var inventory = conn.Query<InventoryView>(
        @"SELECT w.warehouse_name, p.product_name, i.qty, p.safety_stock,
            CASE WHEN i.qty < p.safety_stock THEN N'경고' ELSE N'정상' END AS status
        FROM inventory i
        JOIN warehouses w ON w.warehouse_id = i.warehouse_id
        JOIN products   p ON p.product_id   = i.product_id");

    Console.WriteLine("창고\t상품\t재고\t안전재고\t상태");
    Console.WriteLine("---------------------------------------");
    foreach (var i in inventory)
    {
        Console.WriteLine($"{i.WarehouseName}\t{i.ProductName}\t{i.Qty}\t{i.SafetyStock}\t{i.Status}");
    }
    Console.WriteLine("---------------------------------------");
}

void ReceiveStock()
{
    using var conn = new SqlConnection(connectionString);

    Console.Write("입고할 상품의 코드를 입력하세요: ");
    int? productId = conn.QuerySingleOrDefault<int?>(
        "SELECT product_id FROM products WHERE product_code = @productCode",
        new { productCode = Console.ReadLine() ?? "" });
    if (productId is null)
    {
        Console.WriteLine("상품을 찾지 못했습니다.");
        return;
    }

    Console.Write("입고할 창고의 코드를 입력하세요: ");
    int? warehouseId = conn.QuerySingleOrDefault<int?>(
        "SELECT warehouse_id FROM warehouses WHERE warehouse_code = @warehouseCode",
        new { warehouseCode = Console.ReadLine() ?? "" });
    if (warehouseId is null)
    {
        Console.WriteLine("창고를 찾지 못했습니다.");
        return;
    }

    Console.Write("입고할 수량을 입력하세요: ");
    if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
    {
        Console.WriteLine("1 이상의 숫자를 입력하세요.");
        return;
    }

    conn.Open();
    using var tx = conn.BeginTransaction();
    try
    {
        // 원장(이력) 기록
        conn.Execute(
            @"INSERT INTO stock_movements (product_id, warehouse_id, movement_type, qty) 
            VALUES (@productId, @warehouseId, 'IN', @qty)",
            new { productId, warehouseId, qty }, transaction: tx);

        // 현재고 반영 (있으면 더하기, 없으면 새로 만들기 = UPSERT)
        conn.Execute(
            @"UPDATE inventory
                SET qty = qty + @qty, updated_at = SYSDATETIME()
              WHERE product_id = @productId AND warehouse_id = @warehouseId;

            IF @@ROWCOUNT = 0
                INSERT INTO inventory (product_id, warehouse_id, qty)
                VALUES (@productId, @warehouseId, @qty);",
            new { productId, warehouseId, qty }, transaction: tx);

        tx.Commit();
        Console.WriteLine("입고되었습니다.");
    }
    catch
    {
        tx.Rollback();
        throw;
    }
}

void IssueStock()
{
    using var conn = new SqlConnection(connectionString);

    Console.Write("출고할 상품의 코드를 입력하세요: ");
    int? productId = conn.QuerySingleOrDefault<int?>(
        "SELECT product_id FROM products WHERE product_code = @productCode",
        new { productCode = Console.ReadLine() ?? "" });
    if (productId is null)
    {
        Console.WriteLine("상품을 찾지 못했습니다.");
        return;
    }

    Console.Write("출고할 창고의 코드를 입력하세요: ");
    int? warehouseId = conn.QuerySingleOrDefault<int?>(
        "SELECT warehouse_id FROM warehouses WHERE warehouse_code = @warehouseCode",
        new { warehouseCode = Console.ReadLine() ?? "" });
    if (warehouseId is null)
    {
        Console.WriteLine("창고를 찾지 못했습니다.");
        return;
    }

    Console.Write("출고할 수량을 입력하세요: ");
    if (!int.TryParse(Console.ReadLine(), out int qty) || qty <= 0)
    {
        Console.WriteLine("1 이상의 숫자를 입력하세요.");
        return;
    }

    conn.Open();
    using var tx = conn.BeginTransaction();
    try
    {
        // 출고 전 재고 확인 (트랜잭션 안에서)
        int current = conn.QuerySingleOrDefault<int?>(
            "SELECT qty FROM inventory WHERE product_id = @productId AND warehouse_id = @warehouseId",
            new { productId, warehouseId }, transaction: tx) ?? 0;

        if (current < qty)
        {
            Console.WriteLine($"재고 부족: 현재 {current}개, 요청 {qty}개");
            tx.Rollback();
            return;
        }

        // 원장(이력) 기록 + 현재고 차감 (원자적)
        conn.Execute(
            @"INSERT INTO stock_movements (product_id, warehouse_id, movement_type, qty) 
            VALUES (@productId, @warehouseId, 'OUT', @qty)",
            new { productId, warehouseId, qty }, transaction: tx);

        conn.Execute(
            @"UPDATE inventory 
                SET qty = qty - @qty, updated_at = SYSDATETIME()
              WHERE product_id = @productId AND warehouse_id = @warehouseId",
            new { productId, warehouseId, qty }, transaction: tx);

        tx.Commit();
        Console.WriteLine("출고되었습니다.");
    }
    catch
    {
        tx.Rollback();
        throw;
    }

    // 커밋 이후 조회는 트랜잭션 밖에서 (이미 확정된 최종 재고 확인)
    var item = conn.QuerySingleOrDefault<InventoryView>(
        @"SELECT w.warehouse_name, p.product_name, i.qty, p.safety_stock,
            CASE WHEN i.qty < p.safety_stock THEN N'경고' ELSE N'정상' END AS status
        FROM inventory i
        JOIN warehouses w ON w.warehouse_id = i.warehouse_id
        JOIN products   p ON p.product_id   = i.product_id
        WHERE i.product_id = @productId AND i.warehouse_id = @warehouseId",
        new { productId, warehouseId });

    if (item is not null && item.Qty < item.SafetyStock)
    {
        Console.WriteLine($"⚠ 경고: {item.ProductName} 안전재고 미만! 현재 {item.Qty} < 기준 {item.SafetyStock}");
    }
}

void ListLowStock()
{
    using var conn = new SqlConnection(connectionString);
    var lowStock = conn.Query<InventoryView>(
        @"SELECT w.warehouse_name, p.product_name, i.qty, p.safety_stock,
            CASE WHEN i.qty < p.safety_stock THEN N'경고' ELSE N'정상' END AS status
        FROM inventory i
        JOIN warehouses w ON w.warehouse_id = i.warehouse_id
        JOIN products   p ON p.product_id   = i.product_id
        WHERE i.qty < p.safety_stock").ToList();

    if (lowStock.Count == 0)
    {
        Console.WriteLine("안전재고 미만 품목이 없습니다.");
        return;
    }

    Console.WriteLine("=== 안전재고 미만 품목 ===");
    Console.WriteLine("창고\t상품\t재고\t안전재고\t상태");
    Console.WriteLine("---------------------------------------");
    foreach (var i in lowStock)
    {
        Console.WriteLine($"{i.WarehouseName}\t{i.ProductName}\t{i.Qty}\t{i.SafetyStock}\t{i.Status}");
    }
    Console.WriteLine("---------------------------------------");
}

void ListMovements()
{
    using var conn = new SqlConnection(connectionString);
    var movements = conn.Query<MovementView>(
        @"SELECT w.warehouse_name, 
            p.product_name,
            CASE WHEN sm.movement_type = 'IN' THEN N'입고' ELSE N'출고' END AS movement_type, 
            sm.qty, 
            sm.created_at
        FROM stock_movements sm
        JOIN warehouses w ON w.warehouse_id = sm.warehouse_id
        JOIN products   p ON p.product_id   = sm.product_id
        ORDER BY sm.created_at DESC");

    Console.WriteLine("창고\t상품\t입출고\t수량\t일시");
    Console.WriteLine("---------------------------------------");
    foreach (var m in movements)
    {
        Console.WriteLine($"{m.WarehouseName}\t{m.ProductName}\t{m.MovementType}\t{m.Qty}\t{m.CreatedAt:yyyy-MM-dd HH:mm}");
    }
    Console.WriteLine("---------------------------------------");
}
