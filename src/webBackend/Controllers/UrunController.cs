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
        var query = _context.Products
        .Where(x => !x.IsDeleted)
        .AsQueryable();

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
            Stock = i.Stock ,
            CarrierNames = i.Carriers.Select(c => c.CarrierName).ToList()
        }).AsNoTracking().ToListAsync();
        

        ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", kategori);

        return View(urunler);
    }

        [AllowAnonymous]
        public ActionResult List(string url, string q)
        {
            var query = _context.Products
            .Where(i => i.IsActive && !i.IsDeleted)
            .AsQueryable();

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
        public async Task<ActionResult> Create()
        {
            ViewBag.Kategoriler = new SelectList(_context.Categories.ToList(), "Id", "Name");
            // Aktif kargoları checkbox listesi için gönderiyoruz
            ViewBag.CarrierList = await _context.Carriers.Where(c => (bool)c.IsActive).ToListAsync();
            return View();
        }

        [Authorize(Policy = "Product.Create")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UrunViewModel model, int[] selectedCarrierIds)
        {
            if (model.ImageFile == null || model.ImageFile.Length == 0)
            {
                ModelState.AddModelError("ImageFile", "Lütfen bir ürün resmi seçiniz.");
            }

            if (ModelState.IsValid)
            {
                
                string fileName = "default.png";

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var ext = Path.GetExtension(model.ImageFile.FileName);
                    fileName = Guid.NewGuid().ToString() + ext;

                    var path = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot/img",
                        fileName
                    );

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                }

                // =========================
                // PRODUCT CREATE
                // =========================
                var entity = new Product()
                {
                    ProductName = model.ProductName,
                    Price = model.Price,
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

                // =========================
                // CARRIER RELATION
                // =========================
                if (selectedCarrierIds != null)
                {
                    foreach (var id in selectedCarrierIds)
                    {
                        var carrier = await _context.Carriers.FindAsync(id);

                        if (carrier != null)
                        {
                            entity.Carriers.Add(carrier);
                        }
                    }
                }

                _context.Products.Add(entity);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

        
        ViewBag.Kategoriler = new SelectList(
            await _context.Categories.ToListAsync(),
            "Id",
            "Name"
        );

        ViewBag.CarrierList = await _context.Carriers
            .Where(c => (bool)c.IsActive)
            .ToListAsync();

        return View(model);
    }

        [HttpGet]
        [Authorize(Policy = "Product.Edit")]
        public async Task<ActionResult> Edit(int id)
        {
            // include ile Carriers'ı çekiyoruz ki view'da 'checked' yapabilelim
            var product = await _context.Products
                .Include(p => p.Carriers)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            var model = new UrunEditModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.ProductDescription!,
                IsActive = product.IsActive,
                AnaSayfa = product.AnaSayfa,
                Price = product.Price.ToString("F2", new System.Globalization.CultureInfo("tr-TR")),
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl,
                Weight = product.Weight,
                Width = product.Width,
                Height = product.Height,
                Length = product.Length,
                Desi = (int)product.Desi!,
                Stock = product.Stock,
                // Modelinde varsa seçili ID'leri gönder:
                SelectedCarrierIds = product.Carriers.Select(c => c.Id).ToArray() 
            };

            ViewBag.Kategoriler = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            ViewBag.CarrierList = await _context.Carriers.Where(c => (bool)c.IsActive).ToListAsync();

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "Product.Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, UrunEditModel model, int[] selectedCarrierIds)
        {
            if (id != model.ProductId) return BadRequest();

            if (ModelState.IsValid)
            {
                // ÖNEMLİ: Carriers ile birlikte çekiyoruz
                var product = await _context.Products.Include(p => p.Carriers).FirstOrDefaultAsync(p => p.ProductId == id);
                if (product == null) return NotFound();

                // Fiyat ve temel alanların güncellenmesi aynı kalıyor...
                product.ProductName = model.ProductName;
                product.ProductDescription = model.Description; 
                product.IsActive = model.IsActive;
                // ... (diğer atamalar)

                // --- KARGO GÜNCELLEME MANTIĞI ---
                product.Carriers.Clear(); // Önce mevcutları siliyoruz (EF aradaki tabloyu otomatik temizler)
                if (selectedCarrierIds != null)
                {
                    foreach (var carrierId in selectedCarrierIds)
                    {
                        var carrier = await _context.Carriers.FindAsync(carrierId);
                        if (carrier != null) product.Carriers.Add(carrier); // Yeni seçimleri ekliyoruz
                    }
                }

                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            ViewBag.Kategoriler = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", model.CategoryId);
            ViewBag.CarrierList = await _context.Carriers.Where(c => (bool)c.IsActive).ToListAsync();
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

            // Sadece silinmemiş olanı getir ki zaten silinmiş bir ürün için onay sayfası açılmasın
            var entity = _context.Products.FirstOrDefault(i => i.ProductId == Id && !i.IsDeleted);

            if (entity != null)
            {
                return View(entity);
            }

            return RedirectToAction("Index");
        }

        [Authorize(Policy = "Product.Delete")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirm(int? Id)
        {
            if (Id == null) return Json(new { success = false, message = "Geçersiz ID." });

            var entity = _context.Products.FirstOrDefault(i => i.ProductId == Id);

            if (entity != null)
            {
                // SOFT DELETE (ARŞİVLEME)
                entity.IsDeleted = true;
                _context.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                _context.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Ürün bulunamadı." });
        }

    }
}
