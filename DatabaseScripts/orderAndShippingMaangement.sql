-- 1. Kargo Bölgeleri
CREATE TABLE ShippingRegions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    RegionName NVARCHAR(100) NOT NULL, -- Örn: Marmara, Ege, Yurt Dýþý
    IsActive BIT DEFAULT 1
);

-- 2. Kargo Firmalarý
CREATE TABLE Carriers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CarrierName NVARCHAR(100) NOT NULL,
    IsActive BIT DEFAULT 1
);

-- 3. Fiyatlandýrma Motoru (Desi & Bölge Bazlý)
CREATE TABLE ShippingRates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CarrierId INT FOREIGN KEY REFERENCES Carriers(Id),
    RegionId INT FOREIGN KEY REFERENCES ShippingRegions(Id),
    MinDesi DECIMAL(10,2) NOT NULL,
    MaxDesi DECIMAL(10,2) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    ExtraDesiPrice DECIMAL(18,2) DEFAULT 0 -- Belirlenen MaxDesi aþýlýrsa her +1 desi için
);

-- 4. Sipariþ Durumlarý (Amazon Tipi)
CREATE TABLE OrderStatuses (
    Id INT PRIMARY KEY,
    StatusKey NVARCHAR(50) NOT NULL, -- Pending, InWarehouse, Shipped vb.
    StatusDisplayName NVARCHAR(100) NOT NULL -- Hazýrlanýyor, Kargoya Verildi vb.
);

-- 5. Ýade Talepleri
CREATE TABLE ReturnRequests (
    Id INT PRIMARY KEY IDENTITY(1,1),
    OrderId INT NOT NULL, -- Mevcut Orders tablonuza baðlý
    Reason NVARCHAR(MAX) NOT NULL, -- Ýade nedeni
    RequestDate DATETIME DEFAULT GETDATE(),
    CurrentStatusId INT FOREIGN KEY REFERENCES OrderStatuses(Id),
    IsRefunded BIT DEFAULT 0
);



INSERT INTO OrderStatuses (Id, StatusKey, StatusDisplayName) VALUES 
(1, 'Pending', 'Ödeme Bekleniyor'),
(2, 'Confirmed', 'Onaylandý'),
(3, 'InSupply', 'Tedarik Sürecinde'),
(4, 'InWarehouse', 'Hazýrlanýyor / Depoda'),
(5, 'Shipped', 'Kargoya Verildi'),
(6, 'Delivered', 'Teslim Edildi'),
(7, 'ReturnRequested', 'Ýade Talep Edildi'),
(8, 'Returned', 'Ýade Tamamlandý'),
(9, 'Cancelled', 'Ýptal Edildi');


ALTER TABLE Orders ADD 
    StatusId INT FOREIGN KEY REFERENCES OrderStatuses(Id) DEFAULT 1,
    ShippingRateId INT FOREIGN KEY REFERENCES ShippingRates(Id),
    CargoTrackingCode NVARCHAR(100);



-- null hatasý yememmek adýna --

UPDATE Orders SET StatusId = 1 WHERE StatusId IS NULL;



--SELECT * FROM Orders

--Select * From OrderStatuses


CREATE TABLE CarrierRegions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CarrierId INT NOT NULL,
    RegionId INT NOT NULL,
    FOREIGN KEY (CarrierId) REFERENCES Carriers(Id),
    FOREIGN KEY (RegionId) REFERENCES ShippingRegions(Id)
);

SELECT * FROM CarrierRegions



SELECT * FROM Carriers

SELECT * FROM ShippingRegions



-- Yurtiçi Kargo (CarrierId: 7) Yuvarlanmýþ Fiyatlandýrma Scripti
-- RegionId 6, 7, 8, 9 -> Normal
-- RegionId 10, 11, 12 -> Uzak (+10 TL Ekstra)

BEGIN TRANSACTION

-- 1. NORMAL BÖLGELER (RegionId: 6, 7, 8, 9)
INSERT INTO [AgoraDb].[dbo].[ShippingRates] ([CarrierId], [RegionId], [MinDesi], [MaxDesi], [Price], [ExtraDesiPrice])
SELECT 7, r.Id, 0, 2, 55.00, 5.00 FROM (VALUES (6),(7),(8),(9)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 3, 5, 70.00, 7.00 FROM (VALUES (6),(7),(8),(9)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 6, 10, 95.00, 10.00 FROM (VALUES (6),(7),(8),(9)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 11, 20, 140.00, 12.00 FROM (VALUES (6),(7),(8),(9)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 21, 999, 260.00, 15.00 FROM (VALUES (6),(7),(8),(9)) AS r(Id);

-- 2. UZAK/PAHALI BÖLGELER (RegionId: 10, 11, 12)
INSERT INTO [AgoraDb].[dbo].[ShippingRates] ([CarrierId], [RegionId], [MinDesi], [MaxDesi], [Price], [ExtraDesiPrice])
SELECT 7, r.Id, 0, 2, 65.00, 6.00 FROM (VALUES (10),(11),(12)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 3, 5, 85.00, 9.00 FROM (VALUES (10),(11),(12)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 6, 10, 120.00, 12.00 FROM (VALUES (10),(11),(12)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 11, 20, 175.00, 15.00 FROM (VALUES (10),(11),(12)) AS r(Id)
UNION ALL
SELECT 7, r.Id, 21, 999, 360.00, 20.00 FROM (VALUES (10),(11),(12)) AS r(Id);

COMMIT TRANSACTION;



DELETE FROM [AgoraDb].[dbo].[ShippingRates] 
WHERE [ExtraDesiPrice] > 0;


SELECT CarrierId, RegionId, MinDesi, MaxDesi, Price, ExtraDesiPrice 
FROM [AgoraDb].[dbo].[ShippingRates]
ORDER BY CarrierId, RegionId, MinDesi;


