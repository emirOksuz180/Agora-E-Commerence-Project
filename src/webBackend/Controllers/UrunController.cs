using Microsoft.AspNetCore.Mvc;
using System.Linq;
using webBackend.Models;

using Microsoft.AspNetCore.Authorization;

namespace webBackend.Controllers
{
   
    public class UrunController : Controller
    {
        private readonly AgoraDbContext _context;

        public UrunController(AgoraDbContext context)
        {
            _context = context;
        }





        public ActionResult Index()
        {
            return View();
        }



        // routte parakmetreliri

        public ActionResult List(string url , string q)
        {
            var query = _context.Products.Where(i => i.IsActive).AsQueryable(); // queryable

            if (!string.IsNullOrEmpty(url))
            {
                query = query.Where(i => i.Category.Url == url);
            }
            if(!string.IsNullOrEmpty(q)) {
                query = query.Where(i => i.ProductName.ToLower().Contains(q.ToLower()));
                ViewData["q"] = q;
            }
            // List<Product> urunler = _context.Products.Where(i => i.IsActive && i.Category.Url == url).ToList();
            return View(query.ToList());
        }


        public ActionResult Details(int id)
        {

            // var product = _context.Products.FirstOrDefault(i => i.ProductId == id); //
            // ikinci seçeneki kullanıdım//
            var product = _context.Products.Find(id);

            if (product == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["BenzerUrunler"] = _context.Products
                                                .Where(i => i.IsActive && i.CategoryId == product.CategoryId && i.
                                                ProductId != id)
                                                .Take(4) // mak 4 ürün aldık //
                                                .ToList();

            return View(product);

        }


        public ActionResult Create()
        {
            ViewData["Kategoriler"] = _context.Categories.ToList();
            return View();
        }
        

        public ActionResult Create(UrunCreateModel model)
        {
            var entity = new Product()
            {
                ProductName = model.ProductName,
                Price = (decimal)model.Price,
                IsActive = model.IsActive,
                AnaSayfa = model.AnaSayfa,
                CategoryId = model.CategoryId,
                ImageUrl = "1.jpeg"


            };

            _context.Products.Add(entity);
            _context.SaveChanges();

            return RedirectToAction("Index");
      
        }
    }
}
