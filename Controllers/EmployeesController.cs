using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

public class EmployeeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var employees = await context.Employees
            .AsNoTracking()
            .Include(employee => employee.User)
            .OrderBy(employee => employee.FirstName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync();

            return View(employees);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var employee = await context.Employees
            .AsNoTracking()
            .Include(employee => employee.User)
            .FirstOrDefaultAsync(employee => employee.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        return View(employee);
    }

    public IActionResult Create()
    {
        PopulateAccessLevels();
        return View(new EmployeeAccountFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeAccountFormViewModel viewModel)
    {
        if (await context.Users.AnyAsync(user => user.Email == viewModel.Email))
        {
            ModelState.AddModelError(nameof(viewModel.Email), "An account with this email already exists.");
        }

        if(!ModelState.IsValid)
        {
            PopulateAccessLevels(viewModel.AccessLevel);
            return View(viewModel);
        }

        var user = new User
        {
            Email = viewModel.Email,
            PasswordHash = $"TEMP-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var employee = new Employee
        {
            Id = user.Id,
            FirstName = viewModel.FirstName,
            LastName = viewModel.LastName,
            AccessLevel = viewModel.AccessLevel,
            IsLocked = viewModel.IsLocked
        };

        context.Employees.Add(employee);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var employee = await context.Employees
            .Include(employee => employee.User)
            .FirstOrDefaultAsync(employee => employee.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        var viewModel = new EmployeeAccountFormViewModel
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.User.Email,
            AccessLevel = employee.AccessLevel,
            IsLocked = employee.IsLocked
        };

        PopulateAccessLevels(employee.AccessLevel);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeAccountFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        var emailAlreadyExists = await context.Users
            .AnyAsync(user => user.Email == viewModel.Email && user.Id != id);

        if (emailAlreadyExists)
        {
            ModelState.AddModelError(nameof(viewModel.Email), "An account with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            PopulateAccessLevels(viewModel.AccessLevel);
            return View(viewModel);
        }

        var employee = await context.Employees
            .Include(employee => employee.User)
            .FirstOrDefaultAsync(employee => employee.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        employee.FirstName = viewModel.FirstName;
        employee.LastName = viewModel.LastName;
        employee.AccessLevel = viewModel.AccessLevel;
        employee.IsLocked = viewModel.IsLocked;
        employee.User.Email = viewModel.Email;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var employee = await context.Employees.FindAsync(id);

        if (employee is null)
        {
            return NotFound();
        }

        employee.IsLocked = !employee.IsLocked;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private void PopulateAccessLevels(EmployeeAccessLevel? selectedAccessLevel = null)
    {
        var accessLevels = Enum.GetValues<EmployeeAccessLevel>()
            .Select(accessLevel => new SelectListItem
            {
                Value = accessLevel.ToString(),
                Text = accessLevel.ToString(),
                Selected = selectedAccessLevel == accessLevel
            })
            .ToList();

        ViewData["AccessLevels"] = accessLevels;
    }
}