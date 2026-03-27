using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;
using Microsoft.Extensions.Options;
using webBackend.Services;
using webBackend.Services.webBackend.Services;
using Microsoft.AspNetCore.Authorization;
using webBackend.CustomHandlers.Authorization;
using System.Globalization; 
using Microsoft.AspNetCore.Localization; 

var builder = WebApplication.CreateBuilder(args);

//  Uygulama Servisleri 
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddTransient<ICartService, CartService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

//  Veritabanı Yapılandırması 
builder.Services.AddDbContext<AgoraDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sql =>
    {
        sql.EnableRetryOnFailure(3);
    }));

//   Kimlik (Identity) Yapılandırması 
builder.Services
    .AddIdentity<AppUser, AppRole>(options => 
    {
        
        options.SignIn.RequireConfirmedAccount = true; 
        options.SignIn.RequireConfirmedEmail = true;

        // Kilitleme (Lockout) ayarları - Güvenlik için önemli
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;

        // 2FA Sağlayıcısı olarak varsayılan Mail sağlayıcısını mühürlüyoruz
        options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultEmailProvider;
    })
    .AddEntityFrameworkStores<AgoraDbContext>()
    .AddDefaultTokenProviders()
    .AddRoles<AppRole>();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
});

//  Cookie ve Yetkilendirme //
builder.Services.AddMemoryCache(); 
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>(); 

builder.Services.AddAuthorization(options =>
{
    
});




builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

//  Uygulama insası 
var app = builder.Build();

var defaultCulture = new CultureInfo("tr-TR");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(defaultCulture),
    SupportedCultures = new List<CultureInfo> { defaultCulture },
    SupportedUICultures = new List<CultureInfo> { defaultCulture }
};

app.UseRequestLocalization(localizationOptions);


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

//  Route Yapılandırması

app.MapControllerRoute(
    name: "urunler_by_kategori",
    pattern: "urunler/{url?}",
    defaults: new { controller = "Urun", action = "List" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<AppRole>>();
    var userManager = services.GetRequiredService<UserManager<AppUser>>();

    // Admin Rolü Kontrolü
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new AppRole { Name = "Admin" });
    }

    // Admin Kullanıcı Kontrolü
    var adminEmail = "emiroksuz035@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            AdSoyad = "Emir Öksüz"
        };

        await userManager.CreateAsync(adminUser, "Admin123");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.Run();