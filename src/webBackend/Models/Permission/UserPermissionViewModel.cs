namespace webBackend.Models.Permissons;

public class UserPermissionViewModel
{
    
    public int UserId { get; set; } 
    public string UserName { get; set; } = null!;

    
    public List<PermissionItemViewModel> Permissions { get; set; } = new();
}


public class PermissionItemViewModel
{
    
    public int Id { get; set; } 

    // DBdeki 'PermissionKey' (Örn: Product.Delete)
    public string PermissionKey { get; set; } = null!;

    public string? Description { get; set; }
    public string? GroupName { get; set; }

    
    public bool IsSelected { get; set; } 
}