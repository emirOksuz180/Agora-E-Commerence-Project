
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using webBackend.Models;


namespace webBackend.Controllers;

public class HomeController : Controller
{
    private readonly AgoraDbContext _context;
    
    

     public HomeController(AgoraDbContext context )
    {
        _context = context;
        

    }


    public ActionResult Index()
    {
        var urunler = _context.Products.Where(product => product.IsActive && product.AnaSayfa).ToList();
        ViewData["Kategoriler"] = _context.Categories.ToList();
        return View(urunler);
    }

    // [HttpPost]
    // public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    // {
    //     var user = await _userManager.FindByEmailAsync(email);
    //     if (user is not null)
    //     {
    //         var result = await _signInManager.PasswordSignInAsync(user, password, true, lockoutOnFailure: false);
    //         if (result.Succeeded)
    //         {
    //             if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
    //             {
    //                 return Redirect(returnUrl);
    //             }
    //             return RedirectToAction("Index");
    //         }
    //     }
    //     TempData["LoginError"] = "Geçersiz email veya şifre.";
    //     return RedirectToAction("Index");
    // }

    // [HttpPost]
    // public async Task<IActionResult> Register(string email, string password)
    // {
    //     var existing = await _userManager.FindByEmailAsync(email);
    //     if (existing is not null)
    //     {
    //         TempData["RegisterError"] = "Bu email zaten kayıtlı.";
    //         return RedirectToAction("Index");
    //     }

    //     var newUser = new IdentityUser { UserName = email, Email = email, EmailConfirmed = false };
    //     var result = await _userManager.CreateAsync(newUser, password);
    //     if (result.Succeeded)
    //     {
    //         await _userManager.AddToRoleAsync(newUser, "User");

    //         // Email confirm token
    //         var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);
    //         var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = newUser.Id, token = token }, Request.Scheme)!;

    //         // Basit e-posta içeriği (SMTP ayarları ile gönderilecek)
    //         var subject = "E-posta Onayı";
    //         var body = $"<p>Merhaba,</p><p>Hesabınızı onaylamak için aşağıdaki butona tıklayın:</p><p><a href=\"{callbackUrl}\" style=\"display:inline-block;padding:10px 16px;background:#0d6efd;color:#fff;text-decoration:none;border-radius:6px\">Hesabı Onayla</a></p><p>Bu bağlantı tek kullanımlıktır.</p>";
    //         await _emailSender.SendAsync(email, subject, body);
    //         TempData["RegisterInfo"] = "Lütfen e-posta adresinize gönderilen bağlantıyı onaylayın.";
    //         return RedirectToAction("Index");
    //     }

    //     TempData["RegisterError"] = string.Join(" ", result.Errors.Select(e => e.Description));
    //     return RedirectToAction("Index");
    // }
}
