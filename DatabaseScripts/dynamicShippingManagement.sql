
CREATE TABLE CarrierDistrictExclusions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    CarrierId INT NOT NULL, -- Carriers tablosu ile iliþkili
    DistrictId INT NOT NULL, -- tbl_ilce tablosu ile iliþkili
    IsActive BIT DEFAULT 1, -- 1: Gönderim Kapalý (Yasaklý), 0: Gönderim Açýk
    CONSTRAINT FK_CarrierExclusion_Carrier FOREIGN KEY (CarrierId) REFERENCES Carriers(Id),
    CONSTRAINT FK_CarrierExclusion_District FOREIGN KEY (DistrictId) REFERENCES tbl_ilce(id)
);

GO

-- 2. Lojistik Karar Mekanizmasý (Stored Procedure)
CREATE PROCEDURE sp_GetAvailableShippingOptions
    @CityId INT,
    @DistrictId INT,
    @TotalCartDesi DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    -- Seçilen þehrin bölgesini (RegionId) buluyoruz
    DECLARE @TargetRegionId INT;
    SELECT @TargetRegionId = RegionId FROM tbl_il WHERE id = @CityId;

    
    -- 1. Kargo aktif mi?
    -- 2. Kargo bu bölgeye hizmet veriyor mu?
    -- 3. Sepet desisi kargonun taþýma limitleri (Min/Max) arasýnda mý?
    -- 4. Seçilen ilçe için kargo firmasýnýn bir yasaðý (Exclusion) var mý?
    
    SELECT 
        c.Id AS CarrierId,
        c.CarrierName,
        -- Desi bazlý dinamik fiyat hesaplama:
        (CASE 
            WHEN @TotalCartDesi > sr.MaxDesi 
            THEN sr.Price + ((@TotalCartDesi - sr.MaxDesi) * sr.ExtraDesiPrice)
            ELSE sr.Price 
         END) AS CalculatedShippingPrice
    FROM Carriers c
    INNER JOIN CarrierRegions cr ON c.Id = cr.CarrierId
    INNER JOIN ShippingRates sr ON c.Id = sr.CarrierId AND sr.RegionId = @TargetRegionId
    WHERE c.IsActive = 1 
      AND cr.RegionId = @TargetRegionId
      AND @TotalCartDesi >= sr.MinDesi 
      AND NOT EXISTS (
          SELECT 1 FROM CarrierDistrictExclusions cde 
          WHERE cde.CarrierId = c.Id AND cde.DistrictId = @DistrictId AND cde.IsActive = 1
      );
END
GO






ALTER TABLE [dbo].[tbl_il]
ADD CONSTRAINT FK_tbl_il_ShippingRegions
FOREIGN KEY (RegionId) REFERENCES ShippingRegions(Id);

SELECT RegionId FROM tbl_il WHERE ilAdi = 'Ýzmir'

SELECT ilAdi, RegionId FROM tbl_il WHERE id = 34

SELECT * FROM ShippingRates WHERE RegionId = 6

SELECT * FROM ShippingRegions


EXEC sp_GetAvailableShippingOptions 
    @CityId = 34, 
    @DistrictId = 1, -- Ýlçe ID önemli deðil (eðer yasaklý deðilse)
    @TotalCartDesi = 5.00


SELECT * FROM tbl_ilce where ilceAdi = 'üsküdar'


ALTER PROCEDURE [dbo].[sp_GetAvailableShippingOptions]
    @CityId INT,
    @DistrictId INT,
    @TotalCartDesi DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Önce Þehrin Bölgesini Bulalým
    DECLARE @RegionId INT;
    SELECT @RegionId = RegionId FROM tbl_il WHERE id = @CityId;

    -- 2. Fiyatlarý Getirelim (Sadece o bölge ve o desi aralýðý için)
    SELECT 
        c.Id AS CarrierId,
        c.CarrierName,
        sr.Price AS CalculatedShippingPrice
    FROM Carriers c
    INNER JOIN ShippingRates sr ON c.Id = sr.CarrierId
    WHERE sr.RegionId = @RegionId  -- KRÝTÝK: Sadece o þehrin bölgesindeki fiyatlar
      AND @TotalCartDesi >= sr.MinDesi -- KRÝTÝK: Desi alt sýnýrý
      AND @TotalCartDesi <= sr.MaxDesi -- KRÝTÝK: Desi üst sýnýrý
      AND c.IsActive = 1
      -- 3. Ýleride buraya 'Exclusion' (Yasaklý Ýlçe) kontrolü de ekleyeceðiz
      AND NOT EXISTS (
          SELECT 1 FROM CarrierDistrictExclusions cde 
          WHERE cde.CarrierId = c.Id AND cde.DistrictId = @DistrictId
      );
END

EXEC sp_GetAvailableShippingOptions @CityId = 34, @DistrictId = 1, @TotalCartDesi = 5.00


ALTER PROCEDURE [dbo].[sp_GetAvailableShippingOptions]
    @CityId INT,
    @DistrictId INT,
    @TotalCartDesi DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Þehrin baðlý olduðu bölgeyi bul
    DECLARE @RegionId INT;
    SELECT @RegionId = RegionId FROM tbl_il WHERE id = @CityId;

    -- 2. Sadece o bölgeye ve o DESÝ ARALIÐINA uyan fiyatý getir
    SELECT 
        c.Id AS CarrierId,
        c.CarrierName,
        sr.Price AS CalculatedShippingPrice
    FROM Carriers c
    INNER JOIN ShippingRates sr ON c.Id = sr.CarrierId
    WHERE sr.RegionId = @RegionId
	  AND @TotalCartDesi > sr.MinDesi  -- Sepet desisi alt sýnýrdan BÜYÜK olmalý ( > )
	  AND @TotalCartDesi <= sr.MaxDesi -- Sepet desisi üst sýnýra EÞÝT veya KÜÇÜK olmalý ( <= )
	  AND c.IsActive = 1
      -- Ýlçe bazlý kargo engelleme kontrolü
      AND NOT EXISTS (
          SELECT 1 FROM CarrierDistrictExclusions cde 
          WHERE cde.CarrierId = c.Id AND cde.DistrictId = @DistrictId
      );
END


SELECT * FROM ShippingRates WHERE CarrierId = 6 AND RegionId = (SELECT RegionId FROM tbl_il WHERE id = 34)

EXEC sp_GetAvailableShippingOptions 
    @CityId = 34, 
    @DistrictId = 1, -- Ýlçe ID önemli deðil (eðer yasaklý deðilse)
    @TotalCartDesi = 5.00



SELECT * FROM tbl_ilce where ilceAdi = 'Beþiktaþ'


EXEC sp_GetAvailableShippingOptions 
    @CityId = 35, 
    @DistrictId = 462, -- Az önce yasakladýðýn ilçenin ID'si
    @TotalCartDesi = 5.00


EXEC sp_GetAvailableShippingOptions 
    @CityId = 35, 
    @DistrictId = 476, -- Yasaklamadýðýn bir ilçe
    @TotalCartDesi = 5.00


-- Eðer il bazlý kýsýtlama istiyorsak(tüm ilçeler) test amaçlý SP çalýþtýrsak dahi ki amacý o ilin yasaklanmasý
-- durumudur bu case'de DistricId kolonunu SP de bulundurup birden fazla ilçe kontrol ederek il kýsýtlanmýþsa 
-- kontrol etmiþ oluruz

EXEC sp_GetAvailableShippingOptions 
    @CityId = 34, 
	@DistrictId = 425,
    @TotalCartDesi = 5.00


