using Microsoft.AspNetCore.Mvc;

namespace RVPark.Controllers
{
    // Auth navigation
    public class AccountController : Controller
    {
        // Login GET
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Register GET
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
    }
}