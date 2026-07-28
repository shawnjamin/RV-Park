using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;
using RVPark.Services;
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
        public IActionResult Login(string email, string password, bool rememberMe,
            [FromServices] ApplicationDbContext context, [FromServices] UserPasswordHasher hasher)
        {
            // ViewData["UserFoundOrWrongPassword"] can be used in the HTML with Razor to display an error or success message
            User foundUser = null;
            // Only set foundUser if the email is actually found
            // This has to be done in an if statement so we can check for null next
            if (context.Users.Any(u => u.Email == email))
            {
                foundUser = context.Users.First(u =>
                    u.Email == email && u.PasswordHash == hasher.HashPassword(u, password));
            }

            // User found
            if (foundUser != null)
            {
                ViewData["UserFoundAndPassSuccess"] = true;
                // Customer redirect
                if (foundUser.AccessLevel is AccessLevel.Customer)
                {
                    return RedirectToAction("MyReservations", "Reservations");
                }

                // Manager & Admin redirect
                if (foundUser.AccessLevel is AccessLevel.Admin or AccessLevel.Manager)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                // Staff Redirect
                else if (foundUser.AccessLevel is AccessLevel.Employee)
                {
                    return RedirectToAction("Index", "Employees");
                }
            }

            // No user found
            ViewData["UserFoundAndPassSuccess"] = false;
            ViewData["ErrorMessage"] = "Email not found or password was incorrect. Please try again.";
        return View();
        }

        // Register GET
        [HttpGet]
        public IActionResult Register()
        {
            return View(new User());
        }

        // Register POST bypass
        [HttpPost]
        public IActionResult RegisterSubmit(User userFromForm, string password, string confirmPassword, [FromServices] ApplicationDbContext context, [FromServices] UserPasswordHasher hasher)
        {
            // Check for match between passwords
            // ViewData["PasswordMatch"] can be used in the HTML with Razor to display an error
            if (password != confirmPassword)
            {
                ViewData["PasswordMatch"] = false;
                ViewData["Error Message"] = "Passwords do not match";
                return View();
            }
            ViewData["PasswordMatch"] = true;
            // Have to hash password before adding to DB
            userFromForm.PasswordHash = hasher.HashPassword(userFromForm, password);
            // Check for duplicate Email
            if (context.Users.Any(u => u.Email == userFromForm.Email))
            {
                // Don't redirect if email already exists
                // ViewData["EmailAlreadyExists"] can be used in the HTML with Razor to display an error
                // ErrorMessage is also set and can be used if desired
                ViewData["EmailAlreadyExists"] = true;
                ViewData["ErrorMessage"] = "Email already exists";
                return View();
            }
            // Add user to database and redirect if there are no issues
            context.Add(userFromForm);
            context.SaveChanges();
            ViewData["EmailAlreadyExists"] = false;
            // Default role is Customer, so a new user will always be redirected to reservations
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