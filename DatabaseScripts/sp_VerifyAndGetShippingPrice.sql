CREATE PROCEDURE [dbo].[sp_VerifyAndGetShippingPrice]
    @CartId INT,
    @CityId INT,
    @DistrictId INT,
    @SelectedCarrierId INT,
    @FinalShippingPrice DECIMAL(18,2) OUTPUT -- kod tarafýna fiyatý dönecek
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalDesi DECIMAL(18,2);
    DECLARE @CalculatedPrice DECIMAL(18,2);

    -- 1. Sepet Desisini Hesapla
    SELECT @TotalDesi = SUM(p.Desi * ci.Quantity)
    FROM CartItems ci
    JOIN Products p ON ci.ProductId = p.ProductId
    WHERE ci.CartId = @CartId;

    -- 2. Seçilen Kargo Firmasýnýn O Bölge ve Desi Ýçin Fiyatýný Bul
    SELECT @CalculatedPrice = sr.Price
    FROM ShippingRates sr
    INNER JOIN tbl_il il ON il.RegionId = sr.RegionId
    WHERE il.id = @CityId
      AND sr.CarrierId = @SelectedCarrierId
      AND @TotalDesi > sr.MinDesi 
      AND @TotalDesi <= sr.MaxDesi;

    -- 3. Yasaklý Bölge Kontrolü (Ekstra Güvenlik)
    IF EXISTS (
        SELECT 1 FROM CarrierDistrictExclusions 
        WHERE CarrierId = @SelectedCarrierId 
        AND (DistrictId = @DistrictId OR (DistrictId IS NULL AND CityId = @CityId))
    )
    BEGIN
        SET @FinalShippingPrice = -1; -- Yasaklý bölge kodu
        RETURN;
    END

    SET @FinalShippingPrice = ISNULL(@CalculatedPrice, 0);
END