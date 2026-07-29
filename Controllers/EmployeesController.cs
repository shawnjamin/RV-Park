using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

public class EmployeesController(ApplicationDbContext context) : Controller
{
    // Shows all employee accounts
    public async Task<IActionResult> Index()
    {
        var employees = await context.Employees
            .AsNoTracking()
            .Include(employee => employee.User) // Include User so we the employee email can be displayed.
            .OrderBy(employee => employee.FirstName)
            .ThenBy(employee => employee.FirstName)
            .ToListAsync();

            return View(employees);
    }

    // Shows details for one employee account.
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

    // Loads the blank create employee form.
    public IActionResult Create()
    {
        PopulateAccessLevels();
        return View(new EmployeeAccountFormViewModel());
    }

    // Handles the submitted create employee form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeAccountFormViewModel viewModel)
    {
        // If the email already exists, don't allow it, throw an error.
        if (await context.Users.AnyAsync(user => user.Email == viewModel.Email))
        {
            ModelState.AddModelError(nameof(viewModel.Email), "An account with this email already exists.");
        }

        if(!ModelState.IsValid)
        {
            PopulateAccessLevels(viewModel.AccessLevel);
            return View(viewModel);
        }

        // Create the User account first because Employee uses the same Id.
        var user = new User
        {
            Email = viewModel.Email,
            PasswordHash = $"TEMP-{Guid.NewGuid():N}", // Temporary placeholder password for prototype
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create the Employee record connected to the User record.
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

    // Loads the edit form for an existing employee.
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

        // Convert Employee/User data into the ViewModel used by the form.
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

    // Handles the submittend edit employee form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeAccountFormViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        // Make sure the new email is not already used by another user.
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

        // Update Employee fields.
        employee.FirstName = viewModel.FirstName;
        employee.LastName = viewModel.LastName;
        employee.AccessLevel = viewModel.AccessLevel;
        employee.IsLocked = viewModel.IsLocked;
        // Update related UserData
        employee.User.Email = viewModel.Email;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Locks or Unlocks an employee account from the employee list.
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

    // Builds the dropdown list for meployee access levels.
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