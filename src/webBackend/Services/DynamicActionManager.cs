// Services/DynamicActionManager.cs
using webBackend.Models;

public class DynamicActionManager
{
    private readonly AgoraDbContext _context;

    public DynamicActionManager(AgoraDbContext context)
    {
        _context = context;
    }

    // Edit, Create ve Delete işlemlerini tek merkezden yöneten fonksiyon
    public async Task<object> ExecuteGenericAction(string entityName, string actionType, int? id = null, object? model = null)
    {
        // 1. DB'den bu entity (Tablo) için kuralları çek
        // 2. Eğer actionType == "Edit" ise veriyi getir
        // 3. Eğer actionType == "Create" ise yeni nesne oluştur
        // 4. Eğer actionType == "Delete" ise kaydı sil (veya IsDeleted = 1 yap)
        
        // Bu kısım bizim Framework'ün "Motoru" olacak.
        return await Task.FromResult(new { Success = true }); 
    }
}