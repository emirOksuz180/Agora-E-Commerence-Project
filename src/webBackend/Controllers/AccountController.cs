using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using webBackend.Models.Email;

namespace webBackend.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender _emailSender;

    public AccountController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IEmailSender emailSender)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "E-posta adresiniz onaylı değil. Lütfen e-postanızı kontrol edin.");
                return View();
            }
            var result = await _signInManager.PasswordSignInAsync(user, password, true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }
        }
        ModelState.AddModelError("", "Geçersiz giriş denemesi.");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            TempData["ConfirmError"] = "Kullanıcı bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: true);
            TempData["ConfirmSuccess"] = "E-posta onaylandı, giriş yapıldı.";
            return RedirectToAction("Index", "Home");
        }

        TempData["ConfirmError"] = "Onay başarısız veya bağlantı geçersiz.";
        return RedirectToAction("Index", "Home");
    }
}


