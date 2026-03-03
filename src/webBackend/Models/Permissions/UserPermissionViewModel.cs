namespace webBackend.Models.Permissons;

public class UserPermissionViewModel
{
    // Yetki ataması yapılan kullanıcının Id'si (AppUser tablosundan gelir)
    public int UserId { get; set; } 
    public string UserName { get; set; } = null!;

    // Aşağıdaki liste, checkbox olarak ekranda listelenecek yetkilerdir
    public List<PermissionItemViewModel> Permissions { get; set; } = new();
}


public class PermissionItemViewModel
{
    // DB'deki AppPermission tablosundaki 'Id' kolonu ile birebir eşleşir
    public int Id { get; set; } 

    // DB'deki 'PermissionKey' (Örn: Product.Delete)
    public string PermissionKey { get; set; } = null!;

    public string? Description { get; set; }
    public string? GroupName { get; set; }

    // Ekranda checkbox'ın durumunu (işaretli/işaretli değil) tutar
    public bool IsSelected { get; set; } 
}