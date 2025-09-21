using Microsoft.AspNetCore.Mvc;
using webBackend.Models;

namespace webBackend.ViewComponents;


public class Navbar : ViewComponent
{

  private readonly AgoraDbContext _context;


  public Navbar(AgoraDbContext context)
  {
    _context = context;
  }

  public IViewComponentResult Invoke()
  {
    return View(_context.Categories.ToList());
  }

}