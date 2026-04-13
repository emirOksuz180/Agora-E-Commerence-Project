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

    [Authorize(Policy = "Product.View")]
    public class UrunController : Controller
    {
        private readonly AgoraDbContext _context;

        public UrunController(AgoraDbContext context)
        {
            _context = context;
        }


    [Authorize(Policy = "Product.View")]
    public async Task<ActionResult> Index(int? kategori)
    {
        var query = _context.Products.AsQueryable();

        if (kategori != null)
        {
            query = query.Where(i => i.CategoryId == kategori);
        }

        var urunler = await query.Select(i => new UrunViewModel
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            Price = i.Price,
            IsActive = i.IsActive,
            AnaSayfa = i.AnaSayfa,
            CategoryId = i.CategoryId, 
            CategoryName = i.Category.Name, 
            ImageUrl = i.ImageUrl,
            Weight = i.Weight,
            Width = i.Width,
            Height = i.Height,
            Length = i.Length,
            Desi = (decimal)i.Desi!, 
            
            IsPhysical = i.IsPhysical ?? true,
            Stock = i.Stock 
        }).AsNoTracking().ToListAsync();

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", kategori);

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

        [Authorize(Policy = "Product.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UrunViewModel model)
        {
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Lütfen bir ürün resmi seçiniz.");
            }

            if (ModelState.IsValid)
            {
                string fileName = "default.png"; 

                if (model.ImageFile != null)
                {
                    var extension = Path.GetExtension(model.ImageFile.FileName).ToLower();
                    fileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", fileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                }

                var entity = new Product() 
                {
                    ProductName = model.ProductName,
                    Price = model.Price,
                    // SQL Hatasını önlemek için (Geçici önlem, SQL'de kolonu büyütmelisin)
                    ProductDescription = model.Description, 
                    IsActive = model.IsActive,
                    AnaSayfa = model.AnaSayfa,
                    CategoryId = model.CategoryId,
                    ImageUrl = "/img/" + fileName,
                    Weight = model.Weight,
                    Width = model.Width,
                    Height = model.Height,
                    Length = model.Length,
                    IsPhysical = model.IsPhysical ?? true,
                    Stock = model.Stock 
                };

                _context.Products.Add(entity);
                await _context.SaveChangesAsync();
                await _context.Entry(entity).ReloadAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Kategoriler = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View(model);
        }

        [HttpGet]
        [Authorize(Policy = "Product.Edit")]
        public async Task<ActionResult> Edit(int id)
        {
            
            var entity = await _context.Products
                .Select(i => new UrunEditModel
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Description = i.ProductDescription!,
                    IsActive = i.IsActive,
                    AnaSayfa = i.AnaSayfa,
                    Price = i.Price.ToString("F2", new System.Globalization.CultureInfo("tr-TR")),
                    CategoryId = i.CategoryId,
                    ImageUrl = i.ImageUrl,
                    Weight = i.Weight,
                    Width = i.Width,
                    Height = i.Height,
                    Length = i.Length,
                    Desi = (int)i.Desi!
                }).FirstOrDefaultAsync(i => i.ProductId == id);

            if (entity == null)
            {
                return NotFound();
            }

            
            ViewBag.Kategoriler = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", entity.CategoryId);

            return View(entity);
        }

        [HttpPost]
        [Authorize(Policy = "Product.Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, UrunEditModel model)
        {
            if (id != model.ProductId) return BadRequest();

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Fiyat İşleme
                string priceValue = model.Price.Replace(".", ",");
                if (decimal.TryParse(priceValue, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("tr-TR"), out decimal parsedPrice))
                {
                    product.Price = parsedPrice;
                }

                product.ProductName = model.ProductName;
                product.ProductDescription = model.Description; 
                product.IsActive = model.IsActive;
                product.AnaSayfa = model.AnaSayfa;
                product.CategoryId = model.CategoryId;

                // --- EKSİK ALANLAR BURADA EKLENDİ ---
                product.Stock = model.Stock;
                product.Weight = model.Weight;
                product.Width = model.Width;
                product.Height = model.Height;
                product.Length = model.Length;
                // ------------------------------------

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var extension = Path.GetExtension(model.ImageFile.FileName).ToLower();
                    var fileName = Guid.NewGuid().ToString() + extension;
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", fileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/img/" + fileName;
                }

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Kategoriler = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            return View(model);
        }

        [HttpGet]
        [Authorize(Policy = "Product.Delete")]
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

        [Authorize(Policy = "Product.Delete")]
        [HttpPost, ActionName("Delete")]
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
