using Microsoft.AspNetCore.Authorization;

namespace webBackend.CustomHandlers.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    // sadece bi tane imza görevi görecek bu class


    public string Permission { get; }

    
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }


}