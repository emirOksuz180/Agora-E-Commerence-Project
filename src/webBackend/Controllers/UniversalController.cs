using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webBackend.Services;

[Authorize] // PermissionHandler zaten arkada URL'e göre yetkiyi kesiyor!
public class UniversalController : Controller
{
    private readonly DynamicEntityManager _dynamicManager;

    public UniversalController(DynamicEntityManager dynamicManager)
    {
        _dynamicManager = dynamicManager;
    }

    // Örn: /Universal/List?entityName=Product
    [HttpGet]
    public async Task<IActionResult> List(string entityName)
    {
        var list = await _dynamicManager.GetAllAsync(entityName); // Bunu aşağıda ekleyeceğiz
        ViewBag.EntityName = entityName;
        return View(list);
    }

    // Örn: /Universal/Edit?entityName=Product&id=5
    [HttpGet]
    public async Task<IActionResult> Edit(string entityName, int id)
    {
        var entity = await _dynamicManager.GetEntityByIdAsync(entityName, id);
        if (entity == null) return NotFound();

        ViewBag.Lookups = await _dynamicManager.GetLookupsAsync(entityName);
        ViewBag.EntityName = entityName;

        return View(entity);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string entityName, [FromForm] Dictionary<string, string> formData)
    {
        // Burada ufak bir dokunuş: Form verisini dinamik olarak işliyoruz
        var success = await _dynamicManager.SaveFromDictionaryAsync(entityName, formData);

        if (success) return RedirectToAction("List", new { entityName });
        
        return View();
    }
}