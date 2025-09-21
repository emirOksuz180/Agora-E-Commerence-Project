SELECT * FROM Categories;

INSERT INTO [dbo].[Categories] (Name)
VALUES (N'Smartwatches');


SELECT * FROM [dbo].[Categories];




INSERT INTO [dbo].[Products] 
    (ProductName, ProductDescription, Price, Stock, CategoryId, ImageUrl, IsActive, CreatedAt)
VALUES
    (
        N'Apple Watch Series 9',
        N'Apple Watch Series 9 GPS 45mm',
        19999.99,   -- fiyat
        50,         -- stok
        1,          -- kategori: Smartwatches
        N'https://example.com/images/applewatch9.png',
        1,          -- aktif
        GETDATE()   -- oluşturulma zamanı
    );



INSERT INTO [dbo].[Products] 
    (ProductName, ProductDescription, Price, Stock, CategoryId, ImageUrl, IsActive, CreatedAt)
VALUES
    (N'Apple Watch Series 9', N'Apple Watch Series 9 GPS 45mm', 19999.99, 50, 1, N'https://example.com/images/applewatch9.png', 1, GETDATE()),
    (N'Apple Watch Series 8', N'Apple Watch Series 8 GPS 45mm', 17999.99, 40, 1, N'https://example.com/images/applewatch8.png', 1, GETDATE()),
    (N'Apple Watch Ultra 2', N'Apple Watch Ultra 2 Titanium 49mm', 28999.99, 30, 1, N'https://example.com/images/applewatchultra2.png', 1, GETDATE()),
    (N'Apple Watch SE (2nd Gen)', N'Apple Watch SE 2nd Gen 44mm', 11999.99, 60, 1, N'https://example.com/images/applewatchse2.png', 1, GETDATE());

SELECT * FROM Users;


SELECT * FROM Products;