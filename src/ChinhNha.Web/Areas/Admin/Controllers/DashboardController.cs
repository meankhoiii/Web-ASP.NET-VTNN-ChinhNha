using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
