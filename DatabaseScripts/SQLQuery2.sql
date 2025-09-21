CREATE TRIGGER trg_HashPassword
ON Users
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Users
    SET HashedPassword = CONVERT(VARBINARY(64), HASHBYTES('SHA2_256', i.HashedPassword))
    FROM Users u
    INNER JOIN inserted i ON u.UserId = i.UserId;
END;








CREATE OR ALTER TRIGGER trg_HashSuperAdminPassword
ON SuperAdmin
INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO SuperAdmin (Username, PasswordHash)
    SELECT
        Username,
        HASHBYTES('SHA2_256', CONVERT(VARBINARY(MAX), PasswordHash))
    FROM inserted;
END
