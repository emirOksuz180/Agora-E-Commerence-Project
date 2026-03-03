using System.Threading.Tasks;
using webBackend.Models.Permissons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

namespace dotnet_store.Controllers;

[Authorize(Roles ="Admin")]
public class UserController : Controller
{
    private UserManager<AppUser> _userManager;
    private RoleManager<AppRole> _roleManager;
    private readonly AgoraDbContext _context;
    public UserController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager , AgoraDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<ActionResult> Index(string role)
{
    ViewBag.Roller = new SelectList(_roleManager.Roles, "Name", "Name", role);

    if (!string.IsNullOrEmpty(role))
    {
        return View(await _userManager.GetUsersInRoleAsync(role));
    }

    return View(_userManager.Users);
}

    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<ActionResult> Create(UserCreateModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new AppUser { UserName = model.Email, Email = model.Email, AdSoyad = model.AdSoyad };

            var result = await _userManager.CreateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View(model);
    }

    public async Task<ActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return RedirectToAction("Index");
        }

        var allPermissions = await _context.AppPermissions.ToListAsync();

        var userClaims = await _userManager.GetClaimsAsync(user);

        ViewBag.Roles = await _roleManager.Roles.Select(i => i.Name).ToListAsync();

        return View(
            new UserEditModel
            {   
                
                AdSoyad = user.AdSoyad,
                Email = user.Email!,
                SelectedRoles = await _userManager.GetRolesAsync(user),


                Permissions = allPermissions.Select(p => new PermissionItemViewModel
                {
                    Id = p.Id,
                    PermissionKey = p.PermissionKey,
                    Description = p.Description,
                    GroupName = p.GroupName,
                    IsSelected = userClaims.Any(c => c.Type == "Permission" && c.Value == p.PermissionKey)
                }).ToList()
            }
        );
    }

    [HttpPost]
    public async Task<ActionResult> Edit(string id, UserEditModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                user.Email = model.Email;
                user.AdSoyad = model.AdSoyad;

                var result = await _userManager.UpdateAsync(user);

                
                if (result.Succeeded && !string.IsNullOrEmpty(model.Password))
                {
                    await _userManager.RemovePasswordAsync(user);
                    await _userManager.AddPasswordAsync(user, model.Password);
                }

                if (result.Succeeded)
                {
                    
                    await _userManager.RemoveFromRolesAsync(user, await _userManager.GetRolesAsync(user));

                    if (model.SelectedRoles != null)
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    
                    // Kullanıcının mevcut "Permission" tipindeki tüm claimlerini temizliyoruz
                    var currentClaims = await _userManager.GetClaimsAsync(user);
                    var permissionClaims = currentClaims.Where(c => c.Type == "Permission").ToList();
                    await _userManager.RemoveClaimsAsync(user, permissionClaims);

                    // View'dan (formdan) gelen seçili yetkileri ekliyoruz
                    if (model.Permissions != null)
                    {
                        var selectedClaims = model.Permissions
                            .Where(p => p.IsSelected)
                            .Select(p => new System.Security.Claims.Claim("Permission", p.PermissionKey))
                            .ToList();

                        if (selectedClaims.Any())
                        {
                            await _userManager.AddClaimsAsync(user, selectedClaims);
                        }
                    }

                    return RedirectToAction("Index");
                }

                // Identity hatalarını modele ekleme
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
        }

        return View(model);
    }


    public async Task<ActionResult> Delete(string? Id)
    {
        if(Id == null)
        {
        return RedirectToAction("Index");
        }

        var entity = await _userManager.FindByIdAsync(Id);

        if(entity != null)
        {
        return View(entity); 
        }

        return RedirectToAction("Index");
    }


  [HttpPost]
  public async Task<ActionResult> DeleteConfirm(string? Id)
  {
    
    if(Id == null)
    {
      return RedirectToAction("Index");
    }

     var entity = await _userManager.FindByIdAsync(Id);

    if(entity != null)
    {
      
      var result = await _userManager.DeleteAsync(entity);

        if (result.Succeeded)
        {
            TempData["Mesaj"] = $"{entity.AdSoyad} isimli kişi silindi";
        }
    }

    return RedirectToAction("Index");

  }


}
