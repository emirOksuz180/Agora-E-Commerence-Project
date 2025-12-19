using webBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace webBackend.Controllers;


[Authorize(Roles ="Admin")]
public class SliderController : Controller
{
    private readonly AgoraDbContext _context;
    public SliderController(AgoraDbContext context)
    {
        _context = context;
    }
    public ActionResult Index()
    {
        return View(_context.Sliders.Select(i => new SliderGetModel
        {
             SliderId = i.SliderId,
             SliderTitle = i.SliderTitle,
             IsActive = i.IsActive,
             DisplayOrder = i.DisplayOrder,
             ImageUrl = i.ImageUrl
        }).ToList());
    }

    public ActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<ActionResult> Create(SliderCreateModel model)
    {
        if (model.ImageUrl == null || model.ImageUrl.Length == 0)
        {
            ModelState.AddModelError("Resim", "Resim seçmelisiniz");
        }

        if (ModelState.IsValid)
        {
            var fileName = Path.GetRandomFileName() + ".jpg";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await model.ImageUrl!.CopyToAsync(stream);
            }

            var entity = new Slider()
            {
                SliderTitle = model.SliderTitle,
                SliderDescription   = model.SliderDescription,
                ImageUrl = fileName,
                IsActive = model.IsActive,
                DisplayOrder = model.DisplayOrder
            };

            _context.Sliders.Add(entity);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        return View(model);
    }

    public ActionResult Edit(int id)
    {
        var entity = _context.Sliders.Select(i => new SliderEditModel
        {
            SliderId = i.SliderId,
            SliderTitle = i.SliderTitle,
            SliderDescription = i.SliderDescription,
            IsActive = i.IsActive,
            ImageName = i.ImageUrl,
            DisplayOrder = i.DisplayOrder
        }).FirstOrDefault(i => i.SliderId == id);

        return View(entity);
    }

    [HttpPost]
    public async Task<ActionResult> Edit(int id, SliderEditModel model)
    {
        if (id != model.SliderId)
        {
            return RedirectToAction("Index");
        }

        if (ModelState.IsValid)
        {
            var entity = _context.Sliders.FirstOrDefault(i => i.SliderId == model.SliderId);

            if (entity != null)
            {
                if (model.ImageUrl != null)
                {
                    var fileName = Path.GetRandomFileName() + ".jpg";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img", fileName);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await model.ImageUrl.CopyToAsync(stream);
                    }

                    entity.ImageUrl = fileName;
                }

                entity.SliderTitle = model.SliderTitle;
                entity.SliderDescription = model.SliderDescription;
                entity.IsActive = model.IsActive;
                entity.DisplayOrder = model.DisplayOrder;

                _context.SaveChanges();

                TempData["Mesaj"] = $"{entity.SliderTitle} isimli slider güncellendi.";

                return RedirectToAction("Index");
            }

        }

        return View(model);
    }

    public ActionResult Delete(int? id)
    {
        if (id == null)
        {
            return RedirectToAction("Index");
        }

        var entity = _context.Sliders.FirstOrDefault(i => i.SliderId == id);

        if (entity != null)
        {
            return View(entity);
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public ActionResult DeleteConfirm(int? id)
    {
        if (id == null)
        {
            return RedirectToAction("Index");
        }

        var entity = _context.Sliders.FirstOrDefault(i => i.SliderId == id);

        if (entity != null)
        {
            _context.Sliders.Remove(entity);
            _context.SaveChanges();

            TempData["Mesaj"] = $"{entity.SliderTitle} slideri silindi.";
        }
        return RedirectToAction("Index");
    }




}