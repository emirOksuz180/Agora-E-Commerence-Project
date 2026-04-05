-- claimleri sisteme insert etmek


INSERT INTO AppPermissions (PermissionKey, Description, GroupName)
VALUES 
('Product.Create', 'Yeni ürün ekleme yetkisi', 'Ürün Yönetimi'),
('Product.Edit', 'Ürün bilgilerini güncelleme yetkisi', 'Ürün Yönetimi'),
('Product.Delete', 'Ürün silme yetkisi', 'Ürün Yönetimi'),
('Order.View', 'Sipariþleri görüntüleme yetkisi', 'Sipariþ Yönetimi'),
('User.Manage', 'Kullanýcý yetkilerini düzenleme yetkisi', 'Sistem Yönetimi');


Select * From AppPermissions


SELECT * FROM AspNetUserClaims