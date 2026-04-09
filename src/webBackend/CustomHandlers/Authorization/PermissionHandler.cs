using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using webBackend.Services;
using System.Security.Claims;

namespace webBackend.CustomHandlers.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IMemoryCache _cache;
    private readonly AgoraDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ActionPermissionService _actionService;

    public PermissionHandler(IMemoryCache cache, AgoraDbContext context, IHttpContextAccessor httpContextAccessor, ActionPermissionService actionService)
    {
        _cache = cache;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _actionService = actionService;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // 1. Temel Güvenlik Kontrolü
        if (context.User?.Identity?.IsAuthenticated != true) return;
        
        // Admin her kapıyı açar
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId)) return;

        // 2. URL Keşif (Auto-Discovery)
        var httpContext = _httpContextAccessor.HttpContext;
        var routeData = httpContext?.GetRouteData();
        var controller = routeData?.Values["controller"]?.ToString();
        var action = routeData?.Values["action"]?.ToString();

        // DB'de bu URL için bir kısıtlama (ActionPermission) var mı?
        var actionPermissionId = _actionService.GetActionPermissionId(controller, action);

        // --- STRATEJİ: URL Koruması Önceliklidir ---
        if (actionPermissionId.HasValue)
        {
            // A. Kullanıcının Rollerinden gelen yetkiyi kontrol et
            var userRoleIds = await _context.Users
                .Where(ur => ur.Id == userId)
                .Select(ur => ur.Id)
                .ToListAsync();

            var hasRolePermission = await _context.RoleActionPermissions
                .AnyAsync(rap => userRoleIds.Contains(rap.RoleId) && 
                                rap.PermissionId == actionPermissionId.Value);

            if (hasRolePermission)
            {
                context.Succeed(requirement);
                return;
            }

            // B. Kullanıcıya Şahsen (Özel) verilmiş bir izin var mı bak (IsAllowed = 1)
            var hasUserPermission = await _context.UserActionPermissions
                .AnyAsync(uap => uap.UserId == userId && 
                                uap.PermissionId == actionPermissionId.Value && 
                                uap.IsAllowed);

            if (hasUserPermission)
            {
                context.Succeed(requirement);
                return;
            }
            
            // Eğer URL kilitliyse (ActionPermissions'ta varsa) ve ne rolde ne kullanıcıda izin yoksa: ZINKKK! 
            context.Fail(); 
            return;
        }

        // 3. Klasik Yetki Sistemi (Geriye Dönük Uyumluluk)
        // URL bazlı kısıtlama yoksa, manuel yazılmış [Authorize(Policy = "X")] kontrollerine bakar.
        string cacheKey = $"User_Permissions_{userId}";
        if (!_cache.TryGetValue(cacheKey, out List<string>? userPermissions))
        {
            userPermissions = await GetUserPermissionsFromDb(userId);
            _cache.Set(cacheKey, userPermissions, TimeSpan.FromMinutes(5));
        }

        if (userPermissions != null && userPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }

    private async Task<List<string>> GetUserPermissionsFromDb(int userId)
    {
        var userRoleIds = await _context.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync();
        
        var rolePermissions = await (from rc in _context.RoleClaims
                                     join ap in _context.AppPermissions on rc.ClaimValue equals ap.PermissionKey
                                     where userRoleIds.Contains(rc.RoleId) && rc.ClaimType == "Permission"
                                     select ap.PermissionKey).ToListAsync();

        var directUserPermissions = await (from uc in _context.UserClaims
                                           join ap in _context.AppPermissions on uc.ClaimValue equals ap.PermissionKey
                                           where uc.UserId == userId && uc.ClaimType == "Permission"
                                           select ap.PermissionKey).ToListAsync();

        return rolePermissions.Concat(directUserPermissions).Distinct().ToList();
    }
}