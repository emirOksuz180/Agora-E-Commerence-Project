using System.Threading.Tasks;
using webBackend.Models.Permissons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using System.Security.Claims;

namespace dotnet_store.Controllers;

[Authorize(Policy = "User.View")]
public class UserController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly AgoraDbContext _context;

    public UserController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, AgoraDbContext context)
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

        return View(await _userManager.Users.ToListAsync());
    }

    [HttpGet]
    [Authorize(Policy = "User.Create")]
    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [Authorize(Policy = "User.Create")]
    [ValidateAntiForgeryToken]
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

    [HttpGet]
    [Authorize(Policy = "User.Edit")]
    public async Task<ActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return RedirectToAction("Index");

        var allPermissions = await _context.AppPermissions.ToListAsync();
        var userClaims = await _userManager.GetClaimsAsync(user);
        var directPermissionValues = userClaims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToList();

        var userRoles = await _userManager.GetRolesAsync(user);
        var rolePermissionValues = new List<string>();

        foreach (var roleName in userRoles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                var rClaims = await _roleManager.GetClaimsAsync(role);
                rolePermissionValues.AddRange(rClaims.Where(c => c.Type == "Permission").Select(c => c.Value));
            }
        }

        var allAssignedPermissions = directPermissionValues.Concat(rolePermissionValues).Distinct().ToList();
        ViewBag.Roles = await _roleManager.Roles.Select(i => i.Name).ToListAsync();

        var model = new UserEditModel
        {
            AdSoyad = user.AdSoyad,
            Email = user.Email!,
            SelectedRoles = userRoles,
            Permissions = allPermissions.Select(p => new PermissionItemViewModel
            {
                Id = p.Id,
                PermissionKey = p.PermissionKey,
                Description = p.Description,
                GroupName = p.GroupName,
                IsSelected = allAssignedPermissions.Contains(p.PermissionKey)
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = "User.Edit")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(string id, UserEditModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = await _roleManager.Roles.Select(i => i.Name).ToListAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        user.Email = model.Email;
        user.AdSoyad = model.AdSoyad;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.Password))
            {
                await _userManager.RemovePasswordAsync(user);
                await _userManager.AddPasswordAsync(user, model.Password);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (model.SelectedRoles != null)
            {
                await _userManager.AddToRolesAsync(user, model.SelectedRoles);
            }

            var currentClaims = await _userManager.GetClaimsAsync(user);
            var permissionClaims = currentClaims.Where(c => c.Type == "Permission").ToList();
            await _userManager.RemoveClaimsAsync(user, permissionClaims);

            if (model.Permissions != null)
            {
                var selectedClaims = model.Permissions
                    .Where(p => p.IsSelected)
                    .Select(p => new Claim("Permission", p.PermissionKey))
                    .ToList();

                if (selectedClaims.Any())
                {
                    await _userManager.AddClaimsAsync(user, selectedClaims);
                }
            }

            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        ViewBag.Roles = await _roleManager.Roles.Select(i => i.Name).ToListAsync();
        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = "User.Delete")]
    public async Task<ActionResult> Delete(string? Id)
    {
        if (Id == null) return RedirectToAction("Index");

        var entity = await _userManager.FindByIdAsync(Id);
        if (entity == null) return RedirectToAction("Index");

        return View(entity);
    }

    [HttpPost]
    [Authorize(Policy = "User.Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteConfirm(string? Id)
    {
        if (Id == null) return RedirectToAction("Index");

        var entity = await _userManager.FindByIdAsync(Id);
        if (entity != null)
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