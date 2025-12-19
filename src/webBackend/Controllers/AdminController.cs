// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace webBackend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


[Authorize(Roles = "Admin")]
public class AdminController: Controller
{
  public ActionResult Index()
  {
    return View();
  }
  
}