-- =============================================================
-- StockFlow DB 스키마 (MS SQL Server / T-SQL)
-- ERP 재고관리: 입고(IN) -> 재고(inventory) -> 출고(OUT)
-- 생성 순서: 부모(suppliers, warehouses) -> products -> 자식(inventory, stock_movements)
-- SSMS에서 이 파일을 열고 F5로 실행
-- =============================================================

IF DB_ID(N'StockFlow') IS NULL
    CREATE DATABASE StockFlow;
GO

USE StockFlow;
GO

-- 1) 공급처 (마스터)
CREATE TABLE dbo.suppliers (
    supplier_id      INT          IDENTITY(1,1),
    supplier_code    VARCHAR(100) NOT NULL,
    supplier_name    NVARCHAR(30) NOT NULL,
    supplier_contact VARCHAR(50)  NULL,
    CONSTRAINT pk_suppliers_id   PRIMARY KEY (supplier_id),
    CONSTRAINT uk_suppliers_code UNIQUE (supplier_code)
);
GO

-- 2) 창고 (마스터)
CREATE TABLE dbo.warehouses (
    warehouse_id   INT           IDENTITY(1,1),
    warehouse_code VARCHAR(20)   NOT NULL,
    warehouse_name NVARCHAR(100) NOT NULL,
    location       NVARCHAR(100) NULL,
    CONSTRAINT pk_warehouses        PRIMARY KEY (warehouse_id),
    CONSTRAINT uk_warehouses_code   UNIQUE (warehouse_code)
);
GO

-- 3) 상품 (마스터). 공급처 참조(FK). 공급처 삭제 시 supplier_id는 NULL 처리.
CREATE TABLE dbo.products (
    product_id    INT          IDENTITY(1,1),
    product_code  VARCHAR(50)  NOT NULL,
    product_name  NVARCHAR(50) NOT NULL,
    unit_price    INT          NOT NULL DEFAULT 0,
    safety_stock  INT          NOT NULL DEFAULT 0,
    supplier_id   INT          NULL,
    CONSTRAINT pk_products          PRIMARY KEY (product_id),
    CONSTRAINT uk_products_code     UNIQUE (product_code),
    CONSTRAINT fk_products_supplier
        FOREIGN KEY (supplier_id) REFERENCES dbo.suppliers (supplier_id)
        ON DELETE SET NULL
);
GO

-- 4) 현재고 스냅샷. (product_id + warehouse_id) 조합당 1행(UNIQUE). qty는 음수 불가.
CREATE TABLE dbo.inventory (
    inventory_id INT       IDENTITY(1,1),
    product_id   INT       NOT NULL,
    warehouse_id INT       NOT NULL,
    qty          INT       NOT NULL DEFAULT 0,
    updated_at   DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT pk_inventory             PRIMARY KEY (inventory_id),
    CONSTRAINT uk_inventory_product_wh  UNIQUE (product_id, warehouse_id),
    CONSTRAINT ck_inventory_qty         CHECK (qty >= 0),
    CONSTRAINT fk_inventory_product
        FOREIGN KEY (product_id)   REFERENCES dbo.products (product_id),
    CONSTRAINT fk_inventory_warehouse
        FOREIGN KEY (warehouse_id) REFERENCES dbo.warehouses (warehouse_id)
);
GO

-- 5) 입출고 원장(이력). append-only. 'IN'/'OUT'만 허용, 수량은 항상 양수.
CREATE TABLE dbo.stock_movements (
    movement_id   INT        IDENTITY(1,1),
    product_id    INT        NOT NULL,
    warehouse_id  INT        NOT NULL,
    movement_type VARCHAR(3) NOT NULL,
    qty           INT        NOT NULL,
    created_at    DATETIME2  NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT pk_stock_movements   PRIMARY KEY (movement_id),
    CONSTRAINT ck_movements_type    CHECK (movement_type IN ('IN','OUT')),
    CONSTRAINT ck_movements_qty     CHECK (qty > 0),
    CONSTRAINT fk_movements_product
        FOREIGN KEY (product_id)   REFERENCES dbo.products (product_id),
    CONSTRAINT fk_movements_warehouse
        FOREIGN KEY (warehouse_id) REFERENCES dbo.warehouses (warehouse_id)
);
GO
