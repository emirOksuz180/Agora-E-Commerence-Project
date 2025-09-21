-- Önce eski foreign key'i drop et (varsa)
ALTER TABLE Products
DROP CONSTRAINT IF EXISTS FK_Products_Categories;

-- Yeni foreign key ekle
ALTER TABLE Products
ADD CONSTRAINT FK_Products_Categories
FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
ON DELETE CASCADE
ON UPDATE CASCADE;
