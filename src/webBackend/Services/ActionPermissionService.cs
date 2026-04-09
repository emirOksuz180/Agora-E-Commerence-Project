// Services/ActionPermissionService.cs
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

namespace webBackend.Services;

public class ActionPermissionService
{
    private readonly AgoraDbContext _context;
    private readonly IMemoryCache _cache;

    public ActionPermissionService(AgoraDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }

    /// <summary>
    /// URL bilgisini alır ve bu URL'e bağlanmış olan ActionPermission ID'sini döner.
    /// </summary>
    public int? GetActionPermissionId(string? controller, string? action)
    {
        if (string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action)) return null;

        // Bütün URL-ID haritasını cache'e alıyoruz (Performans için)
        var actionMap = _cache.GetOrCreate("Action_Permission_Id_Map", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
            
            return _context.ActionPermissions
                .Select(x => new { 
                    Key = x.ControllerName + "_" + x.ActionName, 
                    Id = x.Id 
                })
                .ToDictionary(k => k.Key, v => v.Id);
        });

        string currentKey = $"{controller}_{action}";
        return actionMap!.TryGetValue(currentKey, out var id) ? id : (int?)null;
    }
}