using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace webBackend.CustomHandlers.Authorization;


/// <summary>
/// program.cs dosyasinda tek tek claim tanımlamadan bu class tna claim tanımlayacaz
/// </summary>

public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    
    public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
    //  politikaları kontrol et 
    var policy = await FallbackPolicyProvider.GetPolicyAsync(policyName);
    if (policy != null) return policy;

    
    var newPolicy = new AuthorizationPolicyBuilder();
    newPolicy.AddRequirements(new PermissionRequirement(policyName));
    
    return newPolicy.Build();
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();
}