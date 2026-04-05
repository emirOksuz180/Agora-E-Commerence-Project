ALTER TABLE Categories
ADD CONSTRAINT CategoryIndex UNIQUE (Url);

SELECT name, is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID('Categories');


INSERT INTO Categories Values('telefon', 0 , 'telefon')