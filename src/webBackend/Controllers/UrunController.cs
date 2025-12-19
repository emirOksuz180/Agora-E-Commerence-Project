using Microsoft.AspNetCore.Mvc;
using System.Linq;
using webBackend.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.AspNetCore.Authorization;

namespace webBackend.Controllers
{

    [Authorize(Roles ="Admin")]
    public class UrunController : Controller
    {
        private readonly AgoraDbContext _context;

        public UrunController(AgoraDbContext context)
        {
            _context = context;
        }


        
        public ActionResult Index(int? kategori)
        {

            var query = _context.Products.AsQueryable();

            if(kategori != null)
            {
                query = query.Where(i=> i.CategoryId == kategori);
            }
            var urunler = query.Select(i => new UrunGetModel
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Price = i.Price,
                IsActive = i.IsActive,
                AnaSayfa = i.AnaSayfa,
                Category = i.Category,
                ImageUrl = i.ImageUrl
            }).ToList();

            ViewBag.Categories = new SelectList(_context.Categories.ToList(), "Id", "Name" , kategori);

            return View(urunler);
        }

        [AllowAnonymous]
        public ActionResult List(string url, string q)
        {
            var query = _context.Products.Where(i => i.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(url))
            {
                query = query.Where(i => i.Category.Url == url);
            }

            if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(i => i.ProductName.ToLower().Contains(q.ToLower()));
                ViewData["q"] = q;
            }

            return View(query.ToList());
        }

        [AllowAnonymous]
        public ActionResult Details(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["BenzerUrunler"] = _context.Products
                .Where(i => i.IsActive && i.CategoryId == product.CategoryId && i.ProductId != id)
                .Take(4)
                .ToList();

            return View(product);
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Kategoriler = new SelectList(_context.Categories.ToList(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(UrunCreateModel model)
        {
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageUrl", "Resim Seçmelisiniz");
            }

            if (ModelState.IsValid)
            {
                string? fileName = null;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(model.ImageFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension) || model.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageUrl", "Geçersiz resim dosyası.");
                        ViewData["Kategoriler"] = _context.Categories.ToList();
                        return View(model);
                    }

                    try
                    {
                        using var stream = model.ImageFile!.OpenReadStream();
                        using var image = Image.Load<Rgba32>(stream);
                    }
                    catch
                    {
                        ModelState.AddModelError("ImageUrl", "Geçerli bir resim dosyası değil.");
                        ViewData["Kategoriler"] = _context.Categories.ToList();
                        return View(model);
                    }

                    fileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", fileName);

                    using var fileStream = new FileStream(path, FileMode.Create);
                    await model.ImageFile!.CopyToAsync(fileStream);
                }

                var entity = new Product()
                {
                    ProductName = model.ProductName,
                    Price = (decimal)(model.Price ?? 0),
                    IsActive = model.IsActive,
                    AnaSayfa = model.AnaSayfa,
                    CategoryId = (int)model.CategoryId!,
                    ImageUrl = "/img/" + fileName
                };

                _context.Products.Add(entity);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.Kategoriler = new SelectList(_context.Categories.ToList(), "Id", "Name");
            return View(model);
        }

        public ActionResult Edit(int id)
        {
            var entity = _context.Products.Select(i => new UrunEditModel
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                ProductDescription = i.ProductDescription,
                IsActive = i.IsActive,
                AnaSayfa = i.AnaSayfa,
                Price = i.Price,
                CategoryId = i.CategoryId,
                ImageUrl = i.ImageUrl
            }).FirstOrDefault(i => i.ProductId == id);

            ViewData["Kategoriler"] = _context.Categories.ToList();
            return View(entity);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(int id, UrunEditModel model)
        {
            if (id != model.ProductId)
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                var entity = _context.Products.FirstOrDefault(i => i.ProductId == model.ProductId);
                if (entity == null)
                    return NotFound();

                entity.ProductName = model.ProductName;
                entity.ProductDescription = model.ProductDescription;
                entity.Price = model.Price ?? 0;
                entity.IsActive = model.IsActive;
                entity.AnaSayfa = model.AnaSayfa;
                entity.CategoryId = (int)model.CategoryId!;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(model.ImageFile.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension) || model.ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "Geçersiz resim dosyası veya boyutu çok büyük.");
                        ViewData["Kategoriler"] = _context.Categories.ToList();
                        return View(model);
                    }

                    try
                    {
                        using var stream = model.ImageFile.OpenReadStream();
                        using var image = Image.Load<Rgba32>(stream);
                    }
                    catch
                    {
                        ModelState.AddModelError("ImageFile", "Geçerli bir resim dosyası değil.");
                        ViewData["Kategoriler"] = _context.Categories.ToList();
                        return View(model);
                    }

                    var newFileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", newFileName);

                    using var fileStream = new FileStream(path, FileMode.Create);
                    await model.ImageFile.CopyToAsync(fileStream);

                    if (!string.IsNullOrEmpty(entity.ImageUrl))
                    {
                        var oldFileName = Path.GetFileName(entity.ImageUrl);
                        var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", oldFileName);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    entity.ImageUrl = "/img/" + newFileName;
                }

                _context.SaveChanges();
                TempData["Mesaj"] = $"{entity.ProductName} ürünü güncellendi";
                return RedirectToAction("Index");
            }

            // ModelState geçersizse, hata durumunda geri dön
            ViewData["Kategoriler"] = _context.Categories.ToList();
            return View(model);
        }


        public ActionResult Delete(int? Id)
        {
            if (Id == null)
            {
                return RedirectToAction("Index");
            }

            var entity = _context.Products.FirstOrDefault(i => i.ProductId == Id);

            if (entity != null)
            {
                return View(entity);
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public ActionResult DeleteConfirm(int? Id)
        {

            if (Id == null)
            {
                return RedirectToAction("Index");
            }

            var entity = _context.Products.FirstOrDefault(i => i.ProductId == Id);

            if (entity != null)
            {
                _context.Products.Remove(entity);
                _context.SaveChanges();

                TempData["Mesaj"] = $"{entity.ProductName} kategorisi silindi";


            }

            return RedirectToAction("Index");

        }



    }
}
