CREATE TABLE [dbo].[ActionPermissions] (
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ControllerName] [nvarchar](100) NOT NULL, -- Örn: 'Order'
        [ActionName] [nvarchar](100) NOT NULL,     -- Örn: 'Delete'
        [Description] [nvarchar](250) NULL,        -- Örn: 'Sipariþ Silme Yetkisi'
        [Category] [nvarchar](50) NULL             -- Örn: 'Sales', 'Admin', 'Inventory'
    );





CREATE TABLE [dbo].[RoleActionPermissions] (
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    
    -- KRÝTÝK DEÐÝÞÝKLÝK: AspNetRoles.Id int olduðu için burasý da int!
    [RoleId] [int] NOT NULL, 
    
    [PermissionId] [int] NOT NULL,

    CONSTRAINT [FK_RoleActionPermissions_Roles] FOREIGN KEY ([RoleId]) 
        REFERENCES [dbo].[AspNetRoles] ([Id]) ON DELETE CASCADE,
        
    CONSTRAINT [FK_RoleActionPermissions_Permissions] FOREIGN KEY ([PermissionId]) 
        REFERENCES [dbo].[ActionPermissions] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_RolePermission_Unique] ON [dbo].[RoleActionPermissions] ([RoleId], [PermissionId]);







CREATE TABLE [dbo].[UserActionPermissions] (
    [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
    
    -- KRÝTÝK DEÐÝÞÝKLÝK: AspNetUsers.Id int olduðu için burasý da int!
    [UserId] [int] NOT NULL, 
    
    [PermissionId] [int] NOT NULL,
    [IsAllowed] [bit] NOT NULL DEFAULT 1,

    CONSTRAINT [FK_UserActionPermissions_Users] FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE,
        
    CONSTRAINT [FK_UserActionPermissions_Permissions] FOREIGN KEY ([PermissionId]) 
        REFERENCES [dbo].[ActionPermissions] ([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_UserPermission_Unique] ON [dbo].[UserActionPermissions] ([UserId], [PermissionId]);




SELECT * FROM AppPermissions


SELECT 
    SUSER_SNAME() AS [LoginName],          -- SQL'e giriþ yaptýðýn isim
    USER_NAME() AS [DatabaseUserName],     -- Veritabaný içindeki kullanýcý adýn
    DB_NAME() AS [CurrentDatabase],         -- Baðlý olduðun DB
    ORIGINAL_LOGIN() AS [OriginalLogin]



	SELECT 
    @@SERVERNAME AS [FullServerName],       -- Tam sunucu adý (Instance dahil)
    SERVERPROPERTY('MachineName') AS [MachineName], -- Sadece bilgisayar adý
    SERVERPROPERTY('InstanceName') AS [InstanceName], -- Varsa Instance adý (SQLEXPRESS vb.)
    SERVERPROPERTY('ServerName') AS [ServerProperty_Name];