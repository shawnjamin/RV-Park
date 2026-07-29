using Microsoft.AspNetCore.Mvc;

namespace RVPark.Controllers
{
    public class AccountController : Controller
    {
        // Login GET
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Login POST bypass
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            return RedirectToAction("MyReservations", "Reservations");
        }

        // Register GET
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Register POST bypass
        [HttpPost]
        public IActionResult Register(string firstName, string lastName, string email, string phoneNumber, string password)
        {
            return RedirectToAction("MyReservations", "Reservations");
        }

        // Logout POST bypass
        [HttpPost]
        public IActionResult Logout()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}