using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using System.Security.Claims;

namespace webBackend.CustomHandlers.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IMemoryCache _cache;
    private readonly AgoraDbContext _context;

    public PermissionHandler(IMemoryCache cache, AgoraDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId)) 
        {
            return;
        }

        //  CACHE / DB KONTROLÜ
        string cacheKey = $"User_Permissions_{userId}";
        if (!_cache.TryGetValue(cacheKey, out List<string>? userPermissions))
        {
            userPermissions = await GetUserPermissionsFromDb(userId);
            
            
            _cache.Set(cacheKey, userPermissions, TimeSpan.FromSeconds(10));
        }

        
        if (userPermissions != null && userPermissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else
        {
            
            context.Fail();
        }
    }

    private async Task<List<string>> GetUserPermissionsFromDb(int userId)
    {
        
        var userRoleIds = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var rolePermissions = await (from rc in _context.RoleClaims
                                     join ap in _context.AppPermissions on rc.ClaimValue equals ap.PermissionKey
                                     where userRoleIds.Contains(rc.RoleId) && rc.ClaimType == "Permission"
                                     select ap.PermissionKey).ToListAsync();

        
        var directUserPermissions = await (from uc in _context.UserClaims
                                           join ap in _context.AppPermissions on uc.ClaimValue equals ap.PermissionKey
                                           where uc.UserId == userId && uc.ClaimType == "Permission"
                                           select ap.PermissionKey).ToListAsync();

        
        return rolePermissions
            .Concat(directUserPermissions)
            .Distinct()
            .ToList();
    }
}