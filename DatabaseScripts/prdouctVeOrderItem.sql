SELECT TOP (1000) [Id]
      ,[OrderId]
      ,[UrunId]
      ,[Fiyat]
      ,[Miktar]
      ,[PriceAtOrder]
      ,[ProductNameSnapshot]
      ,[ProductImageSnapshot]
      ,[ProductCodeSnapshot]
  FROM [AgoraDb].[dbo].[OrderItem]


  
ALTER TABLE [dbo].[OrderItem] DROP CONSTRAINT [FK_OrderItem_Products_UrunId];


ALTER TABLE [dbo].[OrderItem] 
ADD CONSTRAINT [FK_OrderItem_Products_UrunId] 
FOREIGN KEY ([UrunId]) REFERENCES [dbo].[Products] ([ProductId]) 
ON DELETE NO ACTION; 


ALTER TABLE Products 
ADD IsDeleted BIT NOT NULL DEFAULT 0;

SELECT * FROM Products