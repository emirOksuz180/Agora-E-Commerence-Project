using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

namespace webBackend.Controllers;

[Authorize]
[Authorize(Policy = "Category.View")]
public class KategoriController : Controller
{

  private readonly AgoraDbContext _context;

  public KategoriController(AgoraDbContext context)
  {
    _context = context;
  }

  [Authorize(Policy = "Category.View")]
  public ActionResult Index()
  {

    var kategoriler = _context.Categories.Select(i => new KategoriGetModel
    {
      Id = i.Id,
      Name = i.Name,
      Url = i.Url,
      ProductCount = i.Products.Count

    }).ToList();
    return View(kategoriler);
  }


[HttpGet]
public ActionResult Create()
{
    return View();
}

[HttpPost]
[Authorize(Policy = "Category.Create")]
public ActionResult Create(KategoriCreateModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    var entity = new Category
    {
        Name = model.Name,
        Url = model.Url
    };

    try
    {
        _context.Categories.Add(entity);
        _context.SaveChanges();

        TempData["Mesaj"] = "Kategori oluşturuldu";
        return RedirectToAction("Index");
    }
    catch (DbUpdateException)
    {
        ModelState.AddModelError("Url", "Bu URL zaten kullanımda.");
        return View(model);
    }
}



[HttpGet]
[Authorize(Policy = "Category.Edit")]
public async Task<IActionResult> Edit(int? id)
{
    if (id == null) return NotFound();

    var category = await _context.Categories
        .Select(c => new KategoriEditModel 
        {
            Id = c.Id,
            Name = c.Name,
            
        })
        .FirstOrDefaultAsync(m => m.Id == id);

    if (category == null) return NotFound();

    return View(category);
}

[HttpPost]
[Authorize(Policy = "Category.Edit")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, KategoriEditModel model)
{
    if (id != model.Id) return BadRequest();

    if (ModelState.IsValid)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return NotFound();

        category.Name = model.Name;
        

        try
        {
            _context.Update(category);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Categories.Any(e => e.Id == id)) return NotFound();
            else throw;
        }
        return RedirectToAction(nameof(Index));
    }
    return View(model);
}




  [Authorize(Policy = "Category.Delete")]
  public ActionResult Delete(int? Id)
  {
    if(Id == null)
    {
      return RedirectToAction("Index");
    }

    var entity = _context.Categories.FirstOrDefault(i => i.Id == Id);

    if(entity != null)
    {
      return View(entity); 
    }

    return RedirectToAction("Index");
  }


  [HttpPost]
  [Authorize(Policy = "Category.Delete")]
  public ActionResult DeleteConfirm(int? Id)
  {
    
    if(Id == null)
    {
      return RedirectToAction("Index");
    }

    var entity = _context.Categories.FirstOrDefault(i => i.Id == Id);

    if(entity != null)
    {
      _context.Categories.Remove(entity);
      _context.SaveChanges();

      TempData["Mesaj"] = $"{entity.Name} kategorisi silindi";

      
    }

    return RedirectToAction("Index");

  }


}