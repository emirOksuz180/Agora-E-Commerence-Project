CREATE TABLE [dbo].[OrderShippingDetails] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [OrderId] INT NOT NULL, 
    [CarrierId] INT NOT NULL,
    [ShippingPrice] DECIMAL(18, 2) NOT NULL,
    [TrackingNumber] NVARCHAR(100) NULL, 
    [CreatedAt] DATETIME DEFAULT GETDATE(),
    
    CONSTRAINT [FK_OrderShippingDetails_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders]([Id])
);



ALTER PROCEDURE [dbo].[sp_VerifyAndGetShippingPrice]
    @CartId INT,
    @CityId INT,
    @DistrictId INT,
    @SelectedCarrierId INT,
    @FinalShippingPrice DECIMAL(18,2) OUTPUT
AS
BEGIN
    DECLARE @TotalDesi DECIMAL(18,2) = 0;

   
    SELECT @TotalDesi = SUM(p.Desi * c.Miktar)
    FROM CartItem c 
    JOIN Products p ON c.UrunId = p.ProductId
    WHERE c.CartId = @CartId;
END


-- Manuel Test Sorgusu
DECLARE @Price DECIMAL(18,2);
EXEC [dbo].[sp_VerifyAndGetShippingPrice] 
    @CartId = 1, -- Gerçek bir sepet ID yaz
    @CityId = 34, -- Ýstanbul gibi gerçek bir ID yaz
    @DistrictId = NULL, 
    @SelectedCarrierId = 1, -- Gerçek bir kargo ID yaz
    @FinalShippingPrice = @Price OUTPUT;

SELECT @Price; -- Eðer bu NULL geliyorsa aþaðýdaki tabloyu kontrol etmeliyiz.