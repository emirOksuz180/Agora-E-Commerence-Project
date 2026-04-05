CREATE TABLE [dbo].[AppPermissions] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [PermissionKey] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(250) NULL,
    [GroupName] NVARCHAR(50) NULL,
    
    -- Primary Key Tanýmý
    CONSTRAINT [PK_AppPermissions] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


ALTER TABLE [dbo].[AppPermissions]
ADD CONSTRAINT [UQ_AppPermissions_PermissionKey] UNIQUE ([PermissionKey]);
GO