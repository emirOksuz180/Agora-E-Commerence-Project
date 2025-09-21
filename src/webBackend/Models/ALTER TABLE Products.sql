ALTER TABLE Products
ADD Anasayfa BIT NOT NULL DEFAULT 0;


EXEC sp_rename 'Products.Anasayfa', 'AnaSayfa', 'COLUMN';
