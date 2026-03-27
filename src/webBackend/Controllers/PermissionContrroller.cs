using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

namespace webBackend.Controllers;

[Authorize(Policy = "Claim.View")]
public class PermissionController : Controller
{
    private readonly AgoraDbContext _context;

    public PermissionController(AgoraDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Claim.Create")]
    public IActionResult Create()
    {
        return View();
    }

    
[HttpPost]
[Authorize(Policy = "Claim.Create")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(PermissionCreateModel model) // Parametreyi değiştirdik
{
    // 1. Validasyon kontrolü
    if (!ModelState.IsValid)
    {
        return View(model); // Hata varsa ARTIK AYNI TİPİ (PermissionCreateModel) gönderiyoruz
    }

    // 2. ViewModel'den Veritabanı Nesnesine (AppPermission) Aktarım
    var entity = new AppPermission 
    {
        PermissionKey = model.PermissionKey,
        Description = model.Description,
        GroupName = model.GroupName
    };

    try
    {
        _context.AppPermissions.Add(entity);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Yeni yetki başarıyla eklendi.";
        return RedirectToAction("Index", "Role");
    }
    catch (Exception)
    {
        ModelState.AddModelError("", "Veritabanına kaydedilirken bir hata oluştu.");
        return View(model); // Hata olsa bile tipi bozmuyoruz
    }
}

    // GET: Permission/Edit/5
    [HttpGet]
    [Authorize(Policy = "Claim.Edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var permission = await _context.AppPermissions.FindAsync(id);
        if (permission == null) return NotFound();

        var model = new PermissionCreateModel
        {
            Id = permission.Id, // Hidden field için Id şart!
            PermissionKey = permission.PermissionKey,
            Description = permission.Description,
            GroupName = permission.GroupName
        };


        return View(model);
    }

    
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Claim.Edit")]
    public async Task<IActionResult> Edit(PermissionCreateModel model)
    {



        if (!ModelState.IsValid)
        {
            return View(model);
        }

        
        var entity = await _context.AppPermissions.FindAsync(model.Id);
        if (entity == null) return NotFound();

        
        entity.PermissionKey = model.PermissionKey;
        entity.Description = model.Description;
        entity.GroupName = model.GroupName;

        try
        {
            _context.AppPermissions.Update(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yetki başarıyla güncellendi.";
            return RedirectToAction("Index", "Role");
        }
        catch (Exception)
        {
            ModelState.AddModelError("", "Güncelleme sırasında bir hata oluştu.");
            return View(model);
        }
    }

    // GET: Permission/Delete/5
    [HttpGet]
    [Authorize(Policy = "Claim.Delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var permission = await _context.AppPermissions.FindAsync(id);
        if (permission == null) return NotFound();
        return View(permission);
    }

    // POST: Permission/Delete
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Claim.Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var permission = await _context.AppPermissions.FindAsync(id);
        if (permission != null)
        {
            _context.AppPermissions.Remove(permission);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", "Role");
    }
}