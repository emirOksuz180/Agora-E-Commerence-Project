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
    public async Task<ActionResult> Login(AccountLoginModel model , string? returnUrl)
    {
        if(ModelState.IsValid)
        {

            var user = await _userManager.FindByEmailAsync(model.Email);

            

            if(user!=null)
            {
                await _signInManager.SignOutAsync();

               var result = await _signInManager.PasswordSignInAsync(user , model.Password , model.BeniHatirla , false);

                    if (result.Succeeded)
                     {
                            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                            {
                                return Redirect(returnUrl);
                            }

                            await _cartService.TransferCartToUser(user.UserName!);      

                        return RedirectToAction("Index", "Home");
                      }
                else if(result.IsLockedOut)
                {
                    var lockoutDate = await _userManager.GetLockoutEndDateAsync(user);
                    var timeleft = lockoutDate.Value - DateTime.UtcNow;
                    ModelState.AddModelError("" , $"Hesabınız kilitlendi lütfen {timeleft.Minutes + 1}");
                }
                else
                {
                    ModelState.AddModelError("" , "hatalı parola");
                }

            }
            else
            {
                ModelState.AddModelError("" , "Hatalı email");
            }
            
        }
        return View(model);
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
public async Task<ActionResult> ConfirmEmail(string userId, string token)
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

    var result = await _userManager.ConfirmEmailAsync(user, token);

    if (result.Succeeded)
    {
        TempData["Mesaj"] = "Email adresiniz başarıyla doğrulandı. Giriş yapabilirsiniz.";
        return RedirectToAction("Login");
    }

    TempData["Mesaj"] = "Email doğrulama başarısız.";
    return RedirectToAction("Login");
}


    



}