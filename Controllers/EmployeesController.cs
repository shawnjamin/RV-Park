using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

[Authorize(Roles = "Admin, Manager")]
public class EmployeesController(ApplicationDbContext context) : Controller
{
    // Shows all employee accounts
    public async Task<IActionResult> Index()
    {
        var employees = await context.Users
            .AsNoTracking()
            .Where( user => user.AccessLevel != AccessLevel.Customer) // Filter by access level where the access level IS NOT equal to the customer's access level
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
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

        var employee = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync( user =>
                user.Id == id &&
                user.AccessLevel != AccessLevel.Customer);

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
        return View(new User());
    }

    // Handles the submitted create employee form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User userModel)
    {
        // If the email already exists, don't allow it, throw an error.
        if (await context.Users.AnyAsync(user => user.Email == userModel.Email))
        {
            ModelState.AddModelError(nameof(userModel.Email), "An account with this email already exists.");
        }

        if(!ModelState.IsValid)
        {
            PopulateAccessLevels(userModel.AccessLevel);
            return View(userModel);
        }

        // Create the User account first because Employee uses the same Id.
        var user = new User
        {
            Email = userModel.Email,
            PasswordHash = $"TEMP-{Guid.NewGuid():N}", // Temporary placeholder password for prototype
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create the Employee record connected to the User record.
        var employee = new User
        {
            Id = user.Id,
            FirstName = userModel.FirstName,
            LastName = userModel.LastName,
            AccessLevel = userModel.AccessLevel,
            IsLocked = userModel.IsLocked
        };

        context.Users.Add(employee);
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

        var employee = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync( user =>
                user.Id == id &&
                user.AccessLevel != AccessLevel.Customer);

        if (employee is null)
        {
            return NotFound();
        }

        // Convert User data into the userModel used by the form.
        var userModel = new User
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            AccessLevel = employee.AccessLevel,
            IsLocked = employee.IsLocked
        };

        PopulateAccessLevels(employee.AccessLevel);
        return View(userModel);
    }

    // Handles the submittend edit employee form.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, User userModel)
    {
        if (id != userModel.Id)
        {
            return NotFound();
        }

        // Make sure the new email is not already used by another user.
        var emailAlreadyExists = await context.Users
            .AnyAsync(user => user.Email == userModel.Email && user.Id != id);

        if (emailAlreadyExists)
        {
            ModelState.AddModelError(nameof(userModel.Email), "An account with this email already exists.");
        }

        if (!ModelState.IsValid)
        {
            PopulateAccessLevels(userModel.AccessLevel);
            return View(userModel);
        }

        var employee = await context.Users
            .FirstOrDefaultAsync( user =>
                user.Id == id &&
                user.AccessLevel != AccessLevel.Customer);

        if (employee is null)
        {
            return NotFound();
        }

        // Update Employee fields.
        employee.FirstName = userModel.FirstName;
        employee.LastName = userModel.LastName;
        employee.AccessLevel = userModel.AccessLevel;
        employee.IsLocked = userModel.IsLocked;
        // Update related UserData
        employee.Email = userModel.Email;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Locks or Unlocks an employee account from the employee list.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var employee = await context.Users
            .FirstOrDefaultAsync( user =>
                user.Id == id &&
                user.AccessLevel != AccessLevel.Customer);

        if (employee is null)
        {
            return NotFound();
        }

        employee.IsLocked = !employee.IsLocked;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // Builds the dropdown list for meployee access levels.
    private void PopulateAccessLevels(AccessLevel? selectedAccessLevel = null)
    {
        var accessLevels = Enum.GetValues<AccessLevel>()
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