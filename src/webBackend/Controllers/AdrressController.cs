using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

[Authorize]
public class AddressController : Controller
{
    private readonly AgoraDbContext _context;
    public AddressController(AgoraDbContext context) => _context = context;

    // Adres Listesi
    public async Task<IActionResult> Index()
    {
        // Claim'den ID'yi güvenli bir şekilde alıyoruz
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Eğer claim null ise veya int'e çevrilemiyorsa hata vermesin, login'e atsın
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            // Alternatif olarak return RedirectToAction("Login", "Account"); diyebilirsin
            return Unauthorized(); 
        }

        var addresses = await _context.UserAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault) // Önce varsayılan adres görünsün
            .ThenByDescending(a => a.CreatedAt) // Sonra en yeni eklenen
            .ToListAsync();

        return View(addresses);
}

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserAddress model)
    {
        // CS8604 hatasını önleyen güvenli ID alma
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(); // Kullanıcı login değilse veya ID geçersizse
        }

        ModelState.Remove("User"); // Navigation property'yi validasyondan çıkar

        if (ModelState.IsValid)
        {
            model.UserId = userId;
            model.CreatedAt = DateTime.Now;

            if (model.IsDefault)
            {
                var otherAddresses = await _context.UserAddresses.Where(a => a.UserId == userId).ToListAsync();
                otherAddresses.ForEach(a => a.IsDefault = false);
            }

            _context.UserAddresses.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View("Index", await _context.UserAddresses.Where(a => a.UserId == userId).ToListAsync());
    }


  
  [HttpGet]
public async Task<IActionResult> Edit(int id)
{
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        return Unauthorized();

    var address = await _context.UserAddresses
        .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

    if (address == null) return NotFound();

    return Json(address); // Verileri modalı doldurmak için JSON olarak döneceğiz
}

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(UserAddress model)
  {
      var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
          return Unauthorized();

      ModelState.Remove("User");

      if (ModelState.IsValid)
      {
          // 1. Veritabanındaki mevcut kaydı (Track edilmeden) çekiyoruz
          var existingAddress = await _context.UserAddresses
              .AsNoTracking()
              .FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);

          if (existingAddress == null) return NotFound();

          // 2. Önemli: Eski oluşturulma tarihini yeni modele aktarıyoruz
          model.CreatedAt = existingAddress.CreatedAt;
          model.UserId = userId; // Güvenlik için

          if (model.IsDefault)
          {
              var otherAddresses = await _context.UserAddresses
                  .Where(a => a.UserId == userId && a.Id != model.Id).ToListAsync();
              otherAddresses.ForEach(a => a.IsDefault = false);
          }

          _context.Update(model);
          await _context.SaveChangesAsync();
          return RedirectToAction(nameof(Index));
      }
      return RedirectToAction(nameof(Index));
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Delete(int id)
  {
      var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
      {
          return Unauthorized();
      }

      var address = await _context.UserAddresses
          .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId); // Güvenlik: Sadece kendi adresini silebilir

      if (address != null)
      {
          _context.UserAddresses.Remove(address);
          await _context.SaveChangesAsync();
      }

      return RedirectToAction(nameof(Index));
  }
}