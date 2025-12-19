using Microsoft.AspNetCore.Mvc;
using webBackend.Models;

namespace webBackend.ViewComponents;

public class Slider : ViewComponent
{

  private readonly AgoraDbContext _context;


  public Slider(AgoraDbContext context)
  {
    _context = context;
  }


  public IViewComponentResult Invoke()
  {
    return View(_context.Sliders.Where(i => i.IsActive).OrderBy(i => i.DisplayOrder).ToList());
  }

}