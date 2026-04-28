SELECT * FROM ShippingRegions


ALTER TABLE [AgoraDb].[dbo].[tbl_il]
ADD RegionId INT;



BEGIN TRANSACTION;

-- 1. Marmara Bölgesi (RegionId: 6)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 6 
WHERE ilAdi IN ('ÝSTANBUL', 'BURSA', 'KOCAELÝ', 'TEKÝRDAÐ', 'BALIKESÝR', 'ÇANAKKALE', 'SAKARYA', 'EDÝRNE', 'KIRKLARELÝ', 'BÝLECÝK', 'YALOVA');

-- 2. Ege Bölgesi (RegionId: 7)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 7 
WHERE ilAdi IN ('ÝZMÝR', 'MANÝSA', 'AYDIN', 'DENÝZLÝ', 'MUÐLA', 'AFYONKARAHÝSAR', 'KÜTAHYA', 'UÞAK');

-- 3. Ýç Anadolu Bölgesi (RegionId: 8)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 8 
WHERE ilAdi IN ('ANKARA', 'KONYA', 'KAYSERÝ', 'ESKÝÞEHÝR', 'SÝVAS', 'KIRIKKALE', 'AKSARAY', 'KARAMAN', 'KIRÞEHÝR', 'NÝÐDE', 'NEVÞEHÝR', 'YOZGAT', 'ÇANKIRI');

-- 4. Akdeniz Bölgesi (RegionId: 9)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 9 
WHERE ilAdi IN ('ANTALYA', 'ADANA', 'MERSÝN', 'HATAY', 'KAHRAMANMARAÞ', 'OSMANÝYE', 'ISPARTA', 'BURDUR');

-- 5. Karadeniz Bölgesi (RegionId: 10)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 10 
WHERE ilAdi IN ('SAMSUN', 'TRABZON', 'ORDU', 'GÝRESUN', 'RÝZE', 'ARTVÝN', 'GÜMÜÞHANE', 'BAYBURT', 'AMASYA', 'TOKAT', 'ÇORUM', 'SÝNOP', 'KASTAMONU', 'BARTIN', 'ZONGULDAK', 'KARABÜK', 'DÜZCE', 'BOLU');

-- 6. Güneydoðu Anadolu (RegionId: 11)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 11 
WHERE ilAdi IN ('GAZÝANTEP', 'DÝYARBAKIR', 'ÞANLIURFA', 'MARDÝN', 'ADIYAMAN', 'BATMAN', 'SÝÝRT', 'ÞIRNAK', 'KÝLÝS');

-- 7. Doðu Anadolu (RegionId: 12)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 12 
WHERE ilAdi IN ('ERZURUM', 'MALATYA', 'ELAZIÐ', 'VAN', 'AÐRI', 'KARS', 'IÐDIR', 'ARDAHAN', 'ERZÝNCAN', 'BÝNGÖL', 'MUÞ', 'BÝTLÝS', 'TUNCELÝ', 'HAKKARÝ');

COMMIT TRANSACTION;

-- Kontrol Sorgusu: Boþta kalan (RegionId atanmamýþ) il var mý?
SELECT * FROM [AgoraDb].[dbo].[tbl_il] WHERE RegionId IS NULL;






BEGIN TRANSACTION;

-- 1. Ýsimlerdeki hatalarý ve 'tbl_' takýlarýný temizleyelim
UPDATE [AgoraDb].[dbo].[tbl_il] 
SET ilAdi = 'BÝLECÝK' WHERE id = 11;

UPDATE [AgoraDb].[dbo].[tbl_il] 
SET ilAdi = 'MERSÝN' WHERE id = 33;

UPDATE [AgoraDb].[dbo].[tbl_il] 
SET ilAdi = 'KÝLÝS' WHERE id = 79;

-- 2. Bu düzeltilen illerin RegionId mapping iþlemini yapalým
-- Bilecik -> Marmara (6)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 6 WHERE id = 11;

-- Mersin -> Akdeniz (9)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 9 WHERE id = 33;

-- Kilis -> Güneydoðu Anadolu (11)
UPDATE [AgoraDb].[dbo].[tbl_il] SET RegionId = 11 WHERE id = 79;

COMMIT TRANSACTION;

-- Son kontrol: 81 ilin tamamý doldu mu?
SELECT count(*) as ToplamIl, RegionId 
FROM [AgoraDb].[dbo].[tbl_il] 
GROUP BY RegionId;



-- Hangi ürün hangi kargo firmasýyla taþýnacak--
CREATE TABLE [AgoraDb].[dbo].[ProductCarriers] (
    [ProductId] INT NOT NULL,
    [CarrierId] INT NOT NULL,
    CONSTRAINT PK_ProductCarriers PRIMARY KEY ([ProductId], [CarrierId]),
    
    -- Products tablosundaki ProductId kolonuna referans veriyoruz
    CONSTRAINT FK_ProductCarriers_Products FOREIGN KEY ([ProductId]) 
        REFERENCES [AgoraDb].[dbo].[Products]([ProductId]), 
    
    -- Carriers tablosundaki Id kolonuna referans veriyoruz
    CONSTRAINT FK_ProductCarriers_Carriers FOREIGN KEY ([CarrierId]) 
        REFERENCES [AgoraDb].[dbo].[Carriers]([Id])
);