using Dapper;
using StockFlow.Core.Data;
using StockFlow.Core.Dtos;
using StockFlow.Core.Models;

namespace StockFlow.Core.Services;

public class ProductService
{
    private readonly DbConnectionFactory _db;

    public ProductService(DbConnectionFactory db) => _db = db;

    public IEnumerable<Product> GetAll()
    {
        using var conn = _db.Create();
        return conn.Query<Product>(
            "SELECT product_id, product_code, product_name, unit_price, safety_stock, supplier_id FROM products");
    }

    public bool Create(CreateProductRequest req)
    {
        using var conn = _db.Create();
        int rows = conn.Execute(
            @"INSERT INTO products (product_code, product_name, unit_price, safety_stock, supplier_id) 
              VALUES (@ProductCode, @ProductName, @UnitPrice, @SafetyStock,
                      (SELECT supplier_id FROM suppliers WHERE supplier_code = @SupplierCode))",
            req);
        return rows > 0;
    }

    public bool Update(string productCode, UpdateProductRequest req)
    {
        using var conn = _db.Create();
        int rows = conn.Execute(
            @"UPDATE products SET unit_price = @UnitPrice, safety_stock = @SafetyStock
              WHERE product_code = @productCode",
            new { productCode, req.UnitPrice, req.SafetyStock });
        return rows > 0;
    }
}
