using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using System.Security.Claims;
using webBackend.Models.Permissons;


namespace webBackend.Controllers;

public class UserPermissionController : Controller
{
    private readonly AgoraDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    // Context ve UserManager'ı içeri alıyoruz (Constructor Injection)
    public UserPermissionController(AgoraDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Manage(int userId)
    {
        // 1. Yetki verilecek kullanıcıyı UserManager ile buluyoruz
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound("Sistemde böyle bir kullanıcı bulunamadı.");
        }

        // 2. Veritabanındaki (AppPermissions tablosu) tanımlı tüm yetkileri çekiyoruz
        var allPermissions = await _context.AppPermissions.ToListAsync();

        // 3. Kullanıcının halihazırda sahip olduğu Claim'leri alıyoruz
        // Sadece "Permission" tipindekileri süzüyoruz
        var userClaims = await _userManager.GetClaimsAsync(user);
        var authorizedKeys = userClaims
            .Where(c => c.Type == "Permission")
            .Select(c => c.Value)
            .ToList();

        // 4. Senin refactor ettiğin ViewModel'i dolduruyoruz
        var model = new UserPermissionViewModel
        {
            UserId = user.Id,
            UserName = user.UserName!,
            Permissions = allPermissions.Select(p => new PermissionItemViewModel
            {
                Id = p.Id, // DB'deki Id
                PermissionKey = p.PermissionKey,
                Description = p.Description,
                GroupName = p.GroupName,
                IsSelected = authorizedKeys.Contains(p.PermissionKey) // Kullanıcıda varsa true döner
            }).ToList()
        };

        return View(model);
    }



    [HttpPost]
    [ValidateAntiForgeryToken] // Güvenlik için şart!
    public async Task<IActionResult> Manage(UserPermissionViewModel model)
    {
        // 1. Yetki verilecek kullanıcıyı bul
        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user == null)
        {
            return NotFound("Kullanıcı bulunamadı.");
        }

        // 2. Kullanıcının mevcut tüm "Permission" tipindeki Claim'lerini çek
        var claims = await _userManager.GetClaimsAsync(user);
        var permissionClaims = claims.Where(x => x.Type == "Permission").ToList();

        // 3. Eski yetkilerin tamamını sil (Güncelleme mantığı: Önce sil, sonra yenisini ekle)
        var removeResult = await _userManager.RemoveClaimsAsync(user, permissionClaims);
        if (!removeResult.Succeeded)
        {
            ModelState.AddModelError("", "Eski yetkiler temizlenirken bir hata oluştu.");
            return View(model);
        }

        // 4. Formdan gelen ve seçili olan (IsSelected) yetkileri Claim olarak ekle
        var selectedPermissions = model.Permissions
            .Where(x => x.IsSelected)
            .Select(x => new Claim("Permission", x.PermissionKey))
            .ToList();

        if (selectedPermissions.Any())
        {
            var addResult = await _userManager.AddClaimsAsync(user, selectedPermissions);
            if (!addResult.Succeeded)
            {
                ModelState.AddModelError("", "Yeni yetkiler eklenirken bir hata oluştu.");
                return View(model);
            }
        }

        // 5. İşlem başarılı! Kullanıcı listesine veya uygun bir yere yönlendir
        TempData["SuccessMessage"] = $"{user.UserName} için yetkiler başarıyla güncellendi.";
        return RedirectToAction("UserList", "Admin"); // Admin controller'ındaki listeye dön
    }

}