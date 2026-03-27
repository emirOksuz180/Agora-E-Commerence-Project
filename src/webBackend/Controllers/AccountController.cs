using System.Threading.Tasks;
using webBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using webBackend.Services;


namespace webBackend.Controllers;

public class AccountController : Controller
{
    private UserManager<AppUser> _userManager;
    private SignInManager<AppUser> _signInManager;

    private IEmailService _emailService;

    private readonly AgoraDbContext _context;

    private readonly ICartService _cartService;

    public AccountController(UserManager<AppUser> userManager , SignInManager<AppUser> signInManager , IEmailService emailService , AgoraDbContext context , ICartService cartService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _context = context;
        _cartService = cartService;
    }
    public ActionResult Create()
    {
        return View();
    }

[HttpPost]
public async Task<ActionResult> Create(RegisterViewModel model)
{
    if (ModelState.IsValid)
    {
        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            AdSoyad = model.AdSoyad
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var url = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, token },
                protocol: HttpContext.Request.Scheme
            );

      
            var mailBody = $@"
                Merhaba, {user.AdSoyad} <br/><br/>

                Hesabınızı aktif hale getirmek için aşağıdaki bağlantıya tıklamanız gerekmektedir.<br/><br/>

                <a href='{url}'>Email adresimi doğrula</a><br/><br/>

                Bu işlemi siz başlatmadıysanız, bu maili dikkate almayınız.<br/><br/>

                İyi günler dileriz.
            ";

            await _emailService.SendEmailAsync(
                user.Email!,
                "Email Doğrulama",
                mailBody
            );

            TempData["Mesaj"] =
                "Kayıt işlemi tamamlandı. Email adresinize gönderilen bağlantı ile hesabınızı doğrulayınız.";

            
            return RedirectToAction("Login");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }
    }

    return View(model);
}



    [HttpGet]
    public ActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Login(AccountLoginModel model, string? returnUrl)
{
    if (ModelState.IsValid)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user != null)
        {
            // Güvenlik için önceki oturum kalıntılarını temizle
            await _signInManager.SignOutAsync();

            
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.BeniHatirla, true);

            // birinci durum : 2 faktorlu doğrulama gerekiyor 
            if (result.RequiresTwoFactor)
            {
                
                var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

                
                await _emailService.SendEmailAsync(user.Email!, "Giriş Doğrulama Kodu", 
                    $"<div style='font-family:Arial; padding:20px; border:1px solid #eee; border-radius:10px;'>" +
                    $"<h2 style='color:#6610f2;'>Güvenli Giriş</h2>" +
                    $"<p>Sisteme erişmek için doğrulama kodunuz:</p>" +
                    $"<h1 style='letter-spacing:5px; color:#333;'>{token}</h1>" +
                    $"<p style='color:#888; font-size:12px;'>Bu kod 3 dakika boyunca geçerlidir.</p></div>");

                
                return RedirectToAction("TwoFactorVerify", new { ReturnUrl = returnUrl, RememberMe = model.BeniHatirla });
            }

            //  ikinci durum: giriş basarılı iki faktorlu dogrulama kapalıysa
            if (result.Succeeded)
            {
                await _cartService.TransferCartToUser(user.UserName!);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            
            if (result.IsLockedOut)
            {
                var lockoutDate = await _userManager.GetLockoutEndDateAsync(user);
                var timeleft = lockoutDate!.Value - DateTime.UtcNow;
                ModelState.AddModelError("", $"Çok fazla hatalı deneme! Lütfen {timeleft.Minutes + 1} dakika sonra tekrar deneyin.");
            }
            
            
            else
            {
                ModelState.AddModelError("", "E-posta veya şifre hatalı.");
            }
        }
        else
        {
            ModelState.AddModelError("", "Kullanıcı bulunamadı.");
        }
    }
    
    
    return View(model);
}

    
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> TwoFactorVerify(bool rememberMe, string? returnUrl = null)
    {
        
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        
        if (user == null)
        {
            
            return RedirectToAction(nameof(Login));
        }

        
        var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

        
        await _emailService.SendEmailAsync(user.Email!, "Giriş Doğrulama Kodu", 
            $"<div style='font-family:Arial; padding:20px; border:1px solid #eee;'> " +
            $"<h2>Güvenlik Doğrulaması</h2>" +
            $"<p>Sisteme giriş yapabilmek için kullanmanız gereken doğrulama kodunuz:</p>" +
            $"<h1 style='color:#2c3e50; letter-spacing:5px;'>{token}</h1>" +
            $"<p>Bu kod 3 dakika boyunca geçerlidir.</p></div>");

        
        string email = user.Email!;
        string maskedEmail = email.Substring(0, 2) + "****@" + email.Split('@')[1];

        
        var viewModel = new TwoFactorVerifyViewModel
        {
            RememberMe = rememberMe,
            ReturnUrl = returnUrl,
            MaskedEmail = maskedEmail
        };

        return View(viewModel);
    }



    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken] 
    public async Task<IActionResult> TwoFactorVerify(TwoFactorVerifyViewModel model)
    {
        
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        
        var result = await _signInManager.TwoFactorSignInAsync("Email", model.TwoFactorCode, model.RememberMe, false);

        if (result.Succeeded)
        {
            
            return LocalRedirect(model.ReturnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            // Çok fazla hatalı deneme yapıldıysa
            ModelState.AddModelError(string.Empty, "Çok fazla hatalı deneme yaptınız. Hesabınız geçici olarak kilitlendi.");
            return View(model);
        }

        // Kod yanlışsa veya süresi dolmuşsa
        ModelState.AddModelError("TwoFactorCode", "Girdiğiniz doğrulama kodu geçersiz.");
        return View(model);
    }


    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SecuritySettings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login"); 

        var model = new SecuritySettingsViewModel
        {
            IsTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user)
        };

        return View(model); 
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        var currentStatus = await _userManager.GetTwoFactorEnabledAsync(user);
        var result = await _userManager.SetTwoFactorEnabledAsync(user, !currentStatus);

        if (result.Succeeded)
        {
            
            await _signInManager.RefreshSignInAsync(user); 
            TempData["Mesaj"] = !currentStatus ? "2FA Aktif" : "2FA Kapalı";
        }

        return RedirectToAction(nameof(SecuritySettings));
    }



    [Authorize]
    public async Task<ActionResult> LogOut()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login" , "Account");
    }


    [Authorize]
     public ActionResult Settings()
    {
        return View();
    }
    
    [Authorize]
    public async Task<ActionResult> EditUser()
    {
         var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
         var user = await _userManager.FindByIdAsync(userId!);

         if(user == null)
        {
            return RedirectToAction("Login" , "Account");
        }
        return View(new AccountEditUserModel
        {
            AdSoyad = user.AdSoyad,
            Email = user.Email!
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult> EditUser(AccountEditUserModel model)
    {
        if(ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if(user != null)
            {
                user.Email = model.Email;
                user.AdSoyad = model.AdSoyad;

                var result = await _userManager.UpdateAsync(user);

                if(result.Succeeded)
                {
                    TempData["Mesaj"] = "Bilgileriniz Güncellendi";
                }

                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("" , error.Description);
                }
            }
        }

        return View(model);

         
    }



    [Authorize]
    [HttpGet]
    public ActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult> ChangePassword(AccountChangePasswordModel model)
    {

        if (ModelState.IsValid)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user != null)
            {
                var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.Password);

                if (result.Succeeded)
                {
                    TempData["Mesaj"] = "Parolanız güncellendi";
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
        }


        return View();
    }


    public ActionResult AccessDenied()
    {
        return View();
    }


    public ActionResult ForgotPassword()
    {
        return View();
    }

    
    [HttpPost]
    public async Task<ActionResult> ForgotPassword(string email)
    {   
        if(string.IsNullOrEmpty(email))
        {
            TempData["Mesaj"] = "Email adresinizi";
            return View();
        }

        var user = await _userManager.FindByEmailAsync(email);

        if(user == null)
        {
            TempData["Mesaj"] = "Email Hatalı ya da eksik";
            return View();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var url = Url.Action("ResetPassword" , "Account" , new {userId = user.Id , token});

        var link = $"<a href='http://localhost:5167{url}'>Şifre Yenile</a>";
        await _emailService.SendEmailAsync(user.Email! , "Parola Sıfırlama" , link);

        TempData["Mesaj"] = "Email adresine gönderilen link ile şifreni sıfırlayabilirsin";

        return RedirectToAction("Login");

    }


    public async Task<ActionResult> ResetPassword(string userId , string token)
    {
        if(userId == null || token == null)
        {
            return RedirectToAction("Login");

        }

        var user = await _userManager.FindByIdAsync(userId);

        if(user == null)
        {
            return RedirectToAction("Login");
        }


        var model = new AccountResetPasswordModel
        {
            Token = token,
            Email = user.Email!
        };

        return View(model);
    }


    [HttpPost]
    public async Task<ActionResult> ResetPassword(AccountResetPasswordModel model)
    {
        
        if(ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if(user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user , model.Token , model.Password);


            if(result.Succeeded)
            {
                TempData["Mesaj"] = "Şifreniz Güncellendi";
                return RedirectToAction("Login");
            }


            foreach(var error in result.Errors)
            {
                ModelState.AddModelError("" , error.Description);
            }

        }

        return View(model);

    }


[HttpGet]
public async Task<IActionResult> ConfirmEmail(string userId, string token)
{
    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
    {
        TempData["Mesaj"] = "Geçersiz doğrulama isteği.";
        return RedirectToAction("Login");
    }

    var user = await _userManager.FindByIdAsync(userId);

    if (user == null)
    {
        TempData["Mesaj"] = "Kullanıcı bulunamadı.";
        return RedirectToAction("Login");
    }

    //  replay engeli
    if (user.EmailConfirmed)
    {
        TempData["Mesaj"] = "Bu email adresi zaten doğrulanmış.";
        return RedirectToAction("Index", "Home");
    }

    var result = await _userManager.ConfirmEmailAsync(user, token);

    if (!result.Succeeded)
    {
        TempData["Mesaj"] = "Email doğrulama başarısız veya link süresi dolmuş.";
        return RedirectToAction("Login");
    }

    // güvenlik önlemi
    await _userManager.UpdateSecurityStampAsync(user);

    TempData["Mesaj"] = "Email adresiniz başarıyla doğrulandı. Giriş yapabilirsiniz.";
    return RedirectToAction("Login");
}


    



}

internal class SecuritySettingsViewModel
{
  public bool IsTwoFactorEnabled { get; set; }
}