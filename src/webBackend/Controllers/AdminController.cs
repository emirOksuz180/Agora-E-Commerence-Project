// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace webBackend.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



[Authorize]
public class AdminController: Controller
{
  public ActionResult Index()
  {
    if (!User.IsInRole("Admin") && !User.HasClaim(c => c.Type == "Permission"))
    {
        return Forbid(); 
    }
    
    return View();
  }
  
}