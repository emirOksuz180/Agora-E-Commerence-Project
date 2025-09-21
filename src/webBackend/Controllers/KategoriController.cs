using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webBackend.Models;

namespace webBackend.Controllers;


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
      KategoriAdi = i.Name,
      Url = i.Url,
      UrunSayisi = i.Products.Count

    }).ToList();
    return View(kategoriler);
  }
  

  [HttpPost]
  public ActionResult Create(KategoriCreatModel model)
  {
    var entity  = new Kategori {
      KategoriAdi = model.KategoriAdi,
      Url = model.url
    };

    _context.Categories.Add(entity);
    _context.SaveChanges();
    return View();
  }

  public ActionResult Edit(int id) {
    var entity = _context.Categories.Select(i=> new KategoriEditModel
    {

      Id = i.Id,
      KategoriAdi = i.KategoriAdi,
      Url = i.Url

    }).FirstOrDefult(i => i.Id == id);

    return View(entity); 
  }


}