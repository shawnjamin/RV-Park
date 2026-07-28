using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;
using SQLitePCL;

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
        public IActionResult Login(User user)
        {
            // Check for valid username and password
            // Customer redirect
            if (user.AccessLevel is AccessLevel.Customer)
            {
                return RedirectToAction("MyReservations", "Reservations");
            }
            // Manager & Admin redirect
            if (user.AccessLevel is AccessLevel.Admin or AccessLevel.Manager)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            // Staff Redirect
            else if (user.AccessLevel is AccessLevel.Employee)
            {
                return RedirectToAction("Create", "Employees");
            }

            return View();
        }

        // Register GET
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Register POST bypass
        [HttpPost]
        public IActionResult Register(User user, ApplicationDbContext context)
        {
            // Check for duplicate username
            // ViewData["DataException"] can then be used in the HTML with Razor to display an error or redirect
            // ErrorMessage is also set and can be used if desired
            if (context.Users.Any(u => u.Username != user.Username))
            {
                // No dusplicate username
                // using var db = new ApplicationDbContext(new DbContextOptions<ApplicationDbContext>());
                // Add user to database and redirect if there are no issues
                context.Add(user);
                context.SaveChanges();
                ViewData["DataException"] = false;
                // Default role is Customer, so a new user will always be redirected to reservations
                return RedirectToAction("MyReservations", "Reservations");
            }
            // Don't redirect if username already exists
            ViewData["DataException"] = true;
            ViewData["ErrorMessage"] = "Username already exists";
            return View();
        }

        // Logout POST bypass
        [HttpPost]
        public IActionResult Logout()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}