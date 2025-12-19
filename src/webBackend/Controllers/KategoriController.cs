using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

namespace webBackend.Controllers;


[Authorize(Roles ="Admin")]
public class KategoriController : Controller
{

  private readonly AgoraDbContext _context;

  public KategoriController(AgoraDbContext context)
  {
    _context = context;
  }

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
  public ActionResult Create(KategoriCreateModel model)
  {

    if (ModelState.IsValid)
    {
      var entity = new Category
      {
        Name = model.Name,
        Url = model.Url

      };

      _context.Categories.Add(entity);
      _context.SaveChanges();
      return RedirectToAction("Index");
    }
    return View();


  }


  public ActionResult Edit(int Id)
  {

    var entity = _context.Categories.Select(i => new KategoriEditModel
    {

      Id = i.Id,
      Name = i.Name,
      Url = i.Url

    }).FirstOrDefault(i => i.Id == Id);
    return View(entity);

  }
  
  [HttpPost]
  public ActionResult Edit(int Id , KategoriEditModel model)
  {
    if (Id != model.Id)
    {
      return NotFound();
    }

    if(ModelState.IsValid)
    {

      var entity = _context.Categories.FirstOrDefault(i => i.Id == model.Id);

      if (entity != null)
      {
        entity.Name = model.Name;
        entity.Url = model.Url;

        _context.SaveChanges();

        TempData["Mesaj"] = $"{entity.Name} güncellendi";

        return RedirectToAction("Index");
      }
      
    }
    
    return View();
  }

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