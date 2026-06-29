-- =============================================================
-- StockFlow 시드 데이터 (테스트용)
-- 실행 순서: 01_schema.sql 먼저 실행 후 이 파일 실행
-- FK 때문에 부모(suppliers, warehouses, products) -> 자식(inventory) 순서
--
-- [설계 원칙] IDENTITY로 생긴 id(1,2,3..)는 보장된 값이 아니므로 하드코딩하지 않는다.
--            자식 행은 부모를 "비즈니스 키(code)"로 조회해서 연결한다.
-- =============================================================
USE StockFlow;
GO

-- 기존 데이터 정리 (재실행 가능하도록). 자식부터 삭제.
DELETE FROM dbo.stock_movements;
DELETE FROM dbo.inventory;
DELETE FROM dbo.products;
DELETE FROM dbo.warehouses;
DELETE FROM dbo.suppliers;
GO

-- 1) 공급처
INSERT INTO dbo.suppliers (supplier_code, supplier_name, supplier_contact)
VALUES ('SUP001', N'한빛전자', '02-1111-2222'),
       ('SUP002', N'대성물산', '031-333-4444'),
       ('SUP003', N'미래부품', NULL);          -- 연락처 미지정 케이스
GO

-- 2) 창고
INSERT INTO dbo.warehouses (warehouse_code, warehouse_name, location)
VALUES ('WH001', N'중앙창고', N'서울'),
       ('WH002', N'남부창고', N'부산');
GO

-- 3) 상품: supplier는 supplier_code로 조회해서 연결 (id 하드코딩 X)
INSERT INTO dbo.products (product_code, product_name, unit_price, safety_stock, supplier_id)
SELECT v.product_code, v.product_name, v.unit_price, v.safety_stock, s.supplier_id
FROM (VALUES
        ('PRD001', N'USB-C 케이블',  5000, 20, 'SUP001'),
        ('PRD002', N'무선 마우스',  15000, 10, 'SUP001'),
        ('PRD003', N'기계식 키보드',89000,  5, 'SUP002'),
        ('PRD004', N'노트북 거치대',25000,  8, 'SUP003')
     ) AS v(product_code, product_name, unit_price, safety_stock, supplier_code)
JOIN dbo.suppliers s ON s.supplier_code = v.supplier_code;
GO

-- 4) 현재고: product/warehouse를 code로 조회해서 연결 (id 하드코딩 X)
--    안전재고 경고 테스트를 위해 일부러 미만/품절 케이스 포함
INSERT INTO dbo.inventory (product_id, warehouse_id, qty)
SELECT p.product_id, w.warehouse_id, v.qty
FROM (VALUES
        ('PRD001', 'WH001', 50),   -- USB-C @중앙: 충분 (safety 20)
        ('PRD001', 'WH002',  8),   -- USB-C @남부: 8 < 20  => 경고
        ('PRD002', 'WH001', 30),   -- 무선마우스 @중앙: 충분 (safety 10)
        ('PRD003', 'WH001',  2),   -- 기계식키보드 @중앙: 2 < 5  => 경고
        ('PRD004', 'WH002',  0)    -- 노트북거치대 @남부: 0  => 품절(경고)
     ) AS v(product_code, warehouse_code, qty)
JOIN dbo.products   p ON p.product_code   = v.product_code
JOIN dbo.warehouses w ON w.warehouse_code = v.warehouse_code;
GO

-- 확인용 조회
SELECT * FROM dbo.suppliers;
SELECT * FROM dbo.warehouses;
SELECT * FROM dbo.products;
SELECT * FROM dbo.inventory;
GO
