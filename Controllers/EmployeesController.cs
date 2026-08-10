using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RVPark.Controllers
{
    [Authorize(Roles = "Admin, Manager")]
    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Employees (Index)
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Users
                .AsNoTracking()
                .Where(user => user.AccessLevel != AccessLevel.Customer)
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .ToListAsync();

            return View(employees);
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var employee = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == id && user.AccessLevel != AccessLevel.Customer);

            if (employee is null) return NotFound();

            return View(employee);
        }

        // GET: Employees/Create (Form Load)
        [HttpGet]
        public IActionResult Create()
        {
            PopulateAccessLevels();
            return View(new RVPark.Models.User());
        }

        // POST: Employees/Create (Form Submission)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,AccessLevel,IsLocked")] RVPark.Models.User userModel)
        {
            // Remove validation errors for properties not present in the Create form
            ModelState.Remove(nameof(userModel.PasswordHash));
            ModelState.Remove(nameof(userModel.CreatedAt));
            ModelState.Remove(nameof(userModel.Phone));
            ModelState.Remove("Reservations");

            // Email uniqueness check
            if (!string.IsNullOrWhiteSpace(userModel.Email) &&
                await _context.Users.AnyAsync(u => u.Email.ToLower() == userModel.Email.Trim().ToLower()))
            {
                ModelState.AddModelError(nameof(userModel.Email), "An account with this email already exists.");
            }

            if (!ModelState.IsValid)
            {
                PopulateAccessLevels(userModel.AccessLevel);
                return View(userModel);
            }

            var newEmployee = new RVPark.Models.User
            {
                FirstName = userModel.FirstName?.Trim() ?? string.Empty,
                LastName = userModel.LastName?.Trim() ?? string.Empty,
                Email = userModel.Email?.Trim() ?? string.Empty,
                Phone = userModel.Phone ?? string.Empty,
                AccessLevel = userModel.AccessLevel,
                IsLocked = userModel.IsLocked,
                PasswordHash = $"TEMP-{Guid.NewGuid():N}",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newEmployee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Employee {newEmployee.FirstName} {newEmployee.LastName} created successfully!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Employees/Edit/5 (Form Load)
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var employee = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user => user.Id == id && user.AccessLevel != AccessLevel.Customer);

            if (employee is null) return NotFound();

            PopulateAccessLevels(employee.AccessLevel);
            return View(employee);
        }

        // POST: Employees/Edit/5 (Form Submission)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Email,AccessLevel,IsLocked")] RVPark.Models.User userModel)
        {
            if (id != userModel.Id) return NotFound();

            // Remove validation errors for properties not present in the Edit form
            ModelState.Remove(nameof(userModel.PasswordHash));
            ModelState.Remove(nameof(userModel.CreatedAt));
            ModelState.Remove(nameof(userModel.Phone));
            ModelState.Remove("Reservations");

            // Email uniqueness check (excluding current user)
            if (!string.IsNullOrWhiteSpace(userModel.Email) &&
                await _context.Users.AnyAsync(u => u.Email.ToLower() == userModel.Email.Trim().ToLower() && u.Id != id))
            {
                ModelState.AddModelError(nameof(userModel.Email), "An account with this email already exists.");
            }

            if (!ModelState.IsValid)
            {
                PopulateAccessLevels(userModel.AccessLevel);
                return View(userModel);
            }

            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.AccessLevel != AccessLevel.Customer);

            if (employee is null) return NotFound();

            employee.FirstName = userModel.FirstName?.Trim() ?? employee.FirstName;
            employee.LastName = userModel.LastName?.Trim() ?? employee.LastName;
            employee.Email = userModel.Email?.Trim() ?? employee.Email;
            employee.AccessLevel = userModel.AccessLevel;
            employee.IsLocked = userModel.IsLocked;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Employee account updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Employees/ToggleLock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var employee = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.AccessLevel != AccessLevel.Customer);

            if (employee is null) return NotFound();

            employee.IsLocked = !employee.IsLocked;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Helper: Access Level Dropdown
        private void PopulateAccessLevels(AccessLevel? selectedAccessLevel = null)
        {
            var accessLevels = Enum.GetValues<AccessLevel>()
                .Where(level => level != AccessLevel.Customer)
                .Select(accessLevel => new SelectListItem
                {
                    Value = accessLevel.ToString(),
                    Text = accessLevel.ToString(),
                    Selected = selectedAccessLevel == accessLevel
                })
                .ToList();

            ViewBag.AccessLevels = accessLevels;
        }
    }
}