
ALTER TABLE OrderItem
ADD PriceAtOrder DECIMAL(18,2) NOT NULL DEFAULT 0,
    ProductNameSnapshot NVARCHAR(255) NULL;
GO


UPDATE OrderItem
SET OrderItem.PriceAtOrder = Products.Price, 
    OrderItem.ProductNameSnapshot = Products.ProductName 
FROM OrderItem
INNER JOIN Products ON OrderItem.UrunId = Products.ProductId;
GO