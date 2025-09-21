using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace webBackend.Controllers;


[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController: Controller
{
  public ActionResult Index()
  {
    return View();
  }
  
}