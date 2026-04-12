-- 1. Aksiyon (URL) Tanýmlarý Tablosu
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ActionPermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ActionPermissions] (
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ControllerName] [nvarchar](100) NOT NULL, 
        [ActionName] [nvarchar](100) NOT NULL,     
        [Description] [nvarchar](250) NULL,        
        [Category] [nvarchar](50) NULL             
    );
END

-- 2. Kullanýcý Bazlý Özel Ýzinler Tablosu
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserActionPermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserActionPermissions] (
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] [int] NOT NULL, 
        [PermissionId] [int] NOT NULL,
        [IsAllowed] [bit] NOT NULL DEFAULT 1,

        CONSTRAINT [FK_UserActionPermissions_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
            
        CONSTRAINT [FK_UserActionPermissions_Permissions] FOREIGN KEY ([PermissionId]) 
            REFERENCES [dbo].[ActionPermissions] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_UserPermission_Unique] ON [dbo].[UserActionPermissions] ([UserId], [PermissionId]);
END



IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RoleActionPermissions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RoleActionPermissions] (
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [RoleId] [int] NOT NULL, 
        [PermissionId] [int] NOT NULL,

        CONSTRAINT [FK_RoleActionPermissions_Roles] FOREIGN KEY ([RoleId]) 
            REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
            
        CONSTRAINT [FK_RoleActionPermissions_Permissions] FOREIGN KEY ([PermissionId]) 
            REFERENCES [dbo].[ActionPermissions] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_RolePermission_Unique] ON [dbo].[RoleActionPermissions] ([RoleId], [PermissionId]);
END



select * from Orders


SELECT * FROM Carts

SELECT * FROM CartItem






SELECT * FROM AppPermissions


SELECT * FROM RoleActionPermissions


SELECT * FROM AspNetUsers


SELECT * FROM AspNetRoleClaims WHERE RoleId = '2' -- Admin Rol ID'si


SELECT * FROM AspNetRoleClaims

SELECT * FROM AspNetUserClaims

SELECT * FROM AspNetRoles

SELECT * FROM AspNetUserRoles

SELECT * FROM AspNetRoleClaims WHERE RoleId = '1';


DELETE FROM AspNetRoleClaims 
WHERE RoleId = '1' AND ClaimValue = 'Product.Edit';

;

INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
VALUES ('1', 'Permission', 'Product.Edit');

ALTER TABLE AspNetRoleClaims ALTER COLUMN ClaimType NVARCHAR(256);
ALTER TABLE AspNetRoleClaims ALTER COLUMN ClaimValue NVARCHAR(256);

ALTER TABLE AspNetRoleClaims
ADD CONSTRAINT UQ_Role_Claim_Unique UNIQUE (RoleId, ClaimType, ClaimValue);


SELECT * FROM tbl_ils

SELECT * FROM OrderItem


ALTER TABLE Products
ADD 
    Weight DECIMAL(10,2) NULL,  -- kg
    Width DECIMAL(10,2) NULL,   -- cm
    Height DECIMAL(10,2) NULL,
    Length DECIMAL(10,2) NULL;



ALTER TABLE Products
ADD Desi AS (
    CASE 
        WHEN Width IS NULL OR Height IS NULL OR Length IS NULL THEN 1
        ELSE 
            CASE 
                WHEN CEILING((Width * Height * Length) / 3000.0) < 1 THEN 1
                ELSE CEILING((Width * Height * Length) / 3000.0)
            END
    END
) PERSISTED;


ALTER TABLE Products
ADD IsPhysical BIT DEFAULT 1;


SELECT ProductName, Width, Height, Length, Desi
FROM Products


SELECT * FROM AspNetUserLogins



SELECT 
    COLUMN_NAME AS 'Kolon Adý',
    DATA_TYPE AS 'Veri Tipi',
    IS_NULLABLE AS 'Boþ Geçilebilir mi?',
    COLUMN_DEFAULT AS 'Varsayýlan Deðer',
    (SELECT is_computed FROM sys.columns 
     WHERE object_id = OBJECT_ID('Products') 
     AND name = c.COLUMN_NAME) AS 'Hesaplanmýþ Kolon mu?',
    (SELECT definition FROM sys.computed_columns 
     WHERE object_id = OBJECT_ID('Products') 
     AND name = c.COLUMN_NAME) AS 'Hesaplama Formülü'
FROM 
    INFORMATION_SCHEMA.COLUMNS c
WHERE 
    TABLE_NAME = 'Products'
ORDER BY 
    ORDINAL_POSITION;


	INSERT INTO Products (ProductName, Price, Stock, CategoryId, Width, Height, Length, IsActive, IsPhysical)
VALUES ('Desi Test Urunu', 150.00, 10, 1, 30.00, 20.00, 10.00, 1, 1);

ALTER TABLE Products 
ADD CONSTRAINT DF_Products_AnaSayfa DEFAULT 0 FOR AnaSayfa;


SELECT ProductId, ProductName, Width, Height, Length, Desi 
FROM Products 
WHERE ProductName = 'Desi Test Urunu';





EXEC sp_help 'AspNetUsers';


-- Adresler için SQL Tablosu
CREATE TABLE UserAddresses (
    Id INT PRIMARY KEY IDENTITY(1,1),
    -- AspNetUsers tablosundaki Id INT olduðu için burayý da INT yapýyoruz
    UserId INT NOT NULL, 
    
    AddressTitle NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    District NVARCHAR(100) NOT NULL,
    AddressDetail NVARCHAR(MAX) NOT NULL,
    IsDefault BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),

    -- BAÐLANTI:
    CONSTRAINT FK_UserAddresses_AspNetUsers FOREIGN KEY (UserId) 
    REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- Favoriler için SQL Tablosu
CREATE TABLE Favorites (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL, 
    ProductId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Favorites_AspNetUsers FOREIGN KEY (UserId) 
    REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    
    -- BURASI DÜZELDÝ: Ürün baðlantýsýný ProductId üzerinden kuruyoruz
    CONSTRAINT FK_Favorites_Products FOREIGN KEY (ProductId) 
    REFERENCES Products(ProductId) ON DELETE CASCADE
);


SELECT TABLE_NAME, COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE COLUMN_NAME LIKE '%Id%' AND TABLE_NAME NOT LIKE 'AspNet%';