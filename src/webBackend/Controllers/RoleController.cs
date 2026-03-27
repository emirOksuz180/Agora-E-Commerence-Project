using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using webBackend.Models.Permissons;
using System.Security.Claims;

namespace webBackend.Controllers;



[Authorize(Policy = "Role.View")]
public class RoleController : Controller
{
  private RoleManager<AppRole> _rolemanager;
  private UserManager<AppUser> _usermanager;

  private readonly AgoraDbContext _context;

  public RoleController(RoleManager<AppRole> roleManager , UserManager<AppUser> usermanager , AgoraDbContext context)
  {
    
    _rolemanager = roleManager;
    _usermanager = usermanager;
    _context = context;

  }

  [Authorize(Policy = "Role.View")]  
  public async Task<IActionResult> Index()
  {

    var model = new RolePermissionViewModel
    {
      Roles = _rolemanager.Roles.ToList(),

      Permissions = await _context.AppPermissions
      .Select(p => new PermissionItemViewModel
      {
        
        Id = p.Id,
        PermissionKey = p.PermissionKey,
        Description = p.Description,
        GroupName = p.GroupName

      }).ToListAsync() 

    };
    return View(model);
    
  }

  [HttpGet]
  [Authorize(Policy = "Role.Create")]
  public async Task<IActionResult> Create()
  { 


    var allPermissions = await _context.AppPermissions.ToListAsync();

    var model = new RolePermissionViewModel 
    {
        // ÖNEMLİ: Listeyi burada dolduruyoruz
        Permissions = allPermissions.Select(p => new PermissionItemViewModel
        {
            Id = p.Id,
            PermissionKey = p.PermissionKey,
            GroupName = p.GroupName,
            Description = p.Description,
            IsSelected = false 
        }).ToList()
    };  



    return View(model);
  }

  [HttpPost]
  [Authorize(Policy = "Role.Create")]
public async Task<ActionResult> Create(RolePermissionViewModel model) // Modeli güncelledik
{
    if (ModelState.IsValid)
    {
        // 1. Önce Rolü Oluşturuyoruz
        var role = new AppRole { Name = model.RoleAdi };
        var result = await _rolemanager.CreateAsync(role);

        if (result.Succeeded)
        {
            
            if (model.Permissions != null)
            {
                var selectedPermissions = model.Permissions.Where(x => x.IsSelected).ToList();
                foreach (var perm in selectedPermissions)
                {
                    
                    await _rolemanager.AddClaimAsync(role, new Claim("Permission", perm.PermissionKey));
                }
            }

            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("hata", error.Description);
        }
    }

    
    return View(model);
}

[HttpGet]
[Authorize(Policy = "Role.Edit")]
public async Task<IActionResult> Edit(int id)
{
    
    var role = await _rolemanager.FindByIdAsync(id.ToString());
    
    if (role == null)
    {
        return NotFound();
    }

    
    var allPermissions = await _context.AppPermissions.ToListAsync();

    
    var roleClaims = await _rolemanager.GetClaimsAsync(role);
    var authorizedPermissions = roleClaims
        .Where(c => c.Type == "Permission")
        .Select(c => c.Value)
        .ToList();

    var model = new RolePermissionViewModel
    {
        RoleId = role.Id, 
        RoleAdi = role.Name!,
        Permissions = allPermissions.Select(p => new PermissionItemViewModel
        {
            Id = p.Id,
            PermissionKey = p.PermissionKey,
            GroupName = p.GroupName,
            Description = p.Description,
            
            IsSelected = authorizedPermissions.Contains(p.PermissionKey)
        }).ToList()
    };

    return View(model);
}

[HttpPost]
[Authorize(Policy = "Role.Edit")]
public async Task<IActionResult> Edit(int id, RolePermissionViewModel model)
{

    if (id != model.RoleId)
    {
         model.RoleId = id; 
    }
    
    if (!ModelState.IsValid)
    {
        
        var allPermissions = await _context.AppPermissions.ToListAsync();
        model.Permissions = allPermissions.Select(p => new PermissionItemViewModel {
            
        }).ToList();
        return View(model);
    }

    var role = await _rolemanager.FindByIdAsync(model.RoleId.ToString());
    if (role == null)
    {
        return NotFound();
    }

    
    role.Name = model.RoleAdi;
    var updateResult = await _rolemanager.UpdateAsync(role);

    if (updateResult.Succeeded)
    {
        
        var existingClaims = await _rolemanager.GetClaimsAsync(role);
        foreach (var claim in existingClaims.Where(c => c.Type == "Permission"))
        {
            await _rolemanager.RemoveClaimAsync(role, claim);
        }

        
        if (model.Permissions != null)
        {
            var selectedPermissions = model.Permissions.Where(x => x.IsSelected).ToList();
            foreach (var perm in selectedPermissions)
            {
                await _rolemanager.AddClaimAsync(role, new Claim("Permission", perm.PermissionKey));
            }
        }

        return RedirectToAction("Index");
    }

    foreach (var error in updateResult.Errors)
    {
        ModelState.AddModelError("", error.Description);
    }

    return View(model);
}

  
  [Authorize(Policy = "Role.Delete")]
  public async Task<ActionResult> Delete(string? id)
  {
    if(id == null)
    {
      RedirectToAction("Index");
    }

    var entity = await _rolemanager.FindByIdAsync(id);

    if(entity != null)
    {
      ViewBag.Users = await _usermanager.GetUsersInRoleAsync(entity.Name!);
      return View(entity);
    }

    return RedirectToAction("Index");

  }

  [Authorize(Policy = "Role.Delete")]        
  public async Task<ActionResult> DeleteConfirm(string? id)
  {
    
    if (id == null)
    {
      return RedirectToAction("Index");
    }

    var entity = await _rolemanager.FindByIdAsync(id);

    if (entity != null)
    {
        await _rolemanager.DeleteAsync(entity);

        TempData["Mesaj"] = $"{entity.Name} rolü silindi";
    }

    return RedirectToAction("Index");

  }
}