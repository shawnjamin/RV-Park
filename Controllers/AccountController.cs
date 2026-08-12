using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RVPark.Data;
using RVPark.Models;
using RVPark.Services;

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

        // Login POST
        [HttpPost]
        public async Task<IActionResult> LoginSubmit(
            string email, 
            string password, 
            bool rememberMe,
            [FromServices] ApplicationDbContext context, 
            [FromServices] UserPasswordHasher hasher)
        {
            User? foundUser = null;

            if (context.Users.Any(u => u.Email == email))
            {
                foundUser = context.Users.FirstOrDefault(u => u.Email == email);

                if (foundUser != null && hasher.VerifyHashedPassword(foundUser, password) == PasswordVerificationResult.Failed)
                {
                    foundUser = null;
                }
            }

            if (foundUser != null)
            {
                ViewData["UserFoundAndPassSuccess"] = true;

                // Create user identity claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, foundUser.Id.ToString()),
                    new Claim(ClaimTypes.Email, foundUser.Email),
                    new Claim(ClaimTypes.Name, foundUser.FirstName),
                    new Claim(ClaimTypes.Role, foundUser.AccessLevel.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // Remember Me authentication properties
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), 
                    authProperties);

                // Redirect based on role
                if (foundUser.AccessLevel is AccessLevel.Customer)
                {
                    return RedirectToAction("MyReservations", "Reservations");
                }
                else if (foundUser.AccessLevel is AccessLevel.Admin or AccessLevel.Manager)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
                else if (foundUser.AccessLevel is AccessLevel.Employee)
                {
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            ViewData["UserFoundAndPassSuccess"] = false;
            ViewData["ErrorMessage"] = "Email not found or password was incorrect. Please try again.";
            return View("Login");
        }

        // Register GET
        [HttpGet]
        public IActionResult Register()
        {
            return View(new User());
        }

        // Register POST
        [HttpPost]
        public async Task<IActionResult> RegisterSubmit(
            User userFromForm, 
            string password, 
            string confirmPassword, 
            [FromServices] ApplicationDbContext context, 
            [FromServices] UserPasswordHasher hasher,
            [FromServices] MailService mailService)
        {
            // Password match check
            if (password != confirmPassword)
            {
                ViewData["PasswordMatch"] = false;
                ViewData["ErrorMessage"] = "Passwords do not match.";
                return View("Register", userFromForm);
            }

            // Phone validation check
            var digitsOnly = new string(userFromForm.Phone?.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digitsOnly) || digitsOnly.Length < 10)
            {
                ViewData["ErrorMessage"] = "Please enter a valid 10-digit phone number.";
                return View("Register", userFromForm);
            }
            userFromForm.Phone = digitsOnly;

            // Email duplicate check
            if (context.Users.Any(u => u.Email == userFromForm.Email))
            {
                ViewData["EmailAlreadyExists"] = true;
                ViewData["ErrorMessage"] = "An account with that email already exists.";
                return View("Register", userFromForm);
            }

            // Hash password and save user
            userFromForm.PasswordHash = hasher.HashPassword(userFromForm, password);
            context.Add(userFromForm);
            await context.SaveChangesAsync();

            // Auto log in after registration
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userFromForm.Id.ToString()),
                new Claim(ClaimTypes.Email, userFromForm.Email),
                new Claim(ClaimTypes.Name, userFromForm.FirstName),
                new Claim(ClaimTypes.Role, userFromForm.AccessLevel.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity), 
                authProperties);

            TempData["SuccessMessage"] = "Account created successfully! Welcome to RV Park.";
            
            // Send email
            var mailData = new MailData
            {
                EmailToId = userFromForm.Email,
                EmailToName = userFromForm.FirstName,
                EmailSubject = "Welcome to Hill Air Force Base RV Park!",
                EmailBody = "Welcome, and thank you for signing up for the Hill Air Force Base RV Park Reservation System.\n\n" +
                            "This email is being sent to confirm that you have signed up for our system. Feel free to log in and browse or create a reservation at any time!"
            };
            mailService.SendMail(mailData);
            
            return RedirectToAction("MyReservations", "Reservations");
        }

        // Logout POST
        [HttpPost]
        public async Task Logout()
        {
            // This setup ensures that the cookie is removed properly when the user is logged out
            await HttpContext.SignOutAsync("Cookies");
            var properties = new AuthenticationProperties()
            {
                RedirectUri = "/Home/Index"
            };
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, properties);
        }

        // Unauthorized Role Redirection Handling
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}