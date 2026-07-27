using Microsoft.AspNetCore.Mvc;

namespace RVPark.Controllers
{
    public class DashboardController : Controller
    {
        // Login GET
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

    }
}