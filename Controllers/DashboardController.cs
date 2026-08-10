using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RVPark.Controllers
{
    [Authorize(Roles = "Employee, Manager, Admin")]
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