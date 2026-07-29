using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

/// <summary>
/// Handles displaying, creating, editing, and deleting reservation fees.
/// Site charges are excluded because they are managed separately from
/// additional reservation fees.
/// </summary>
public class FeesController(ApplicationDbContext context) : Controller
{

#region Index and Details Actions

    /// <summary>
    /// Displays a list of all additional reservation fees.
    /// Normal site charges are excluded from the results.
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // Retrieve all non-site charge bills with their associated
        // reservation and customer information.
        var fees = await context.Bills
            .AsNoTracking()
            .Include(bill => bill.Reservation)
                .ThenInclude(reservation => reservation!.Customer)
            .Where(bill => bill.Type != BillType.SiteCharge)
            .OrderByDescending(bill => bill.CreatedAt)
            .ToListAsync();
        // Send the fee list to the Index view
        return View(fees);
    }

    /// <summary>
    /// Displays the details of a specific reservation fee.
    /// </summary>
    /// <param name="id">The ID of the fee to display. </param>
    public async Task<IActionResult> Details(int? id)
    {
        // A fee won't be found with no ID.
        if (id is null)
        {
            return NotFound();
        }

        // Get the requested fee, include its reservation and customer.
        // Site charges can't be viewed through this controller.
        var fee = await context.Bills
            .AsNoTracking()
            .Include(bill => bill.Reservation)
                .ThenInclude(reservation => reservation!.Customer)
            .FirstOrDefaultAsync(bill => bill.Id == id && bill.Type != BillType.SiteCharge);

        // Return a 404 response if the fee does not exist.
        if (fee is null)
        {
            return NotFound();
        }
        
        // Send the selected fee to the Details view. 
        return View(fee);
    }
#endregion

#region Create Actions

    /// <summary>
    /// Displays the form used to create a new reservation fee.
    /// </summary>
    public async Task<IActionResult> Create()
    {
        // Populate the reservation and fee type dropdown lists.
        await PopulateReservationsAsync();
        PopulateFeeTypes();
        return View();
    }

    /// <summary>
    /// Process the submitted form for creating a new reservation fee.
    /// </summary>
    /// <param name="fee">The fee information submitted by the user.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ReservationId,Type,Description,Amount")] Bill fee)
    {
        // Site charges are not considered additional reservation fees
        // and can't be created through this controller.
        if (fee.Type == BillType.SiteCharge)
        {
            ModelState.AddModelError(nameof(Bill.Type), "Select a reservation fee type.");
        }

        // Confirm the the selected reservation exists in the database.
        if (!await context.Reservations.AnyAsync(reservation => reservation.Id == fee.ReservationId))
        {
            ModelState.AddModelError(nameof(Bill.ReservationId), "Select a valid reservation.");
        }

        // Re-display the form when the validation fails.
        if (!ModelState.IsValid)
        {
            // Rebuild the dropdown lists because ViewData is not preserved
            // when the form is submitted.
            await PopulateReservationsAsync(fee.ReservationId);
            PopulateFeeTypes(fee.Type);
            return View(fee);
        }

        // Record the creation time in UTC before saving the fee.
        fee.CreatedAt = DateTime.UtcNow;

        // Add the new fee to the database
        context.Bills.Add(fee);
        await context.SaveChangesAsync();

        // Return the user to the fee list.
        return RedirectToAction(nameof(Index));
    }
#endregion
    
    #region Edit Actions

    /// <summary>
    /// Displays the form used to edit an existing reservation fee. 
    /// </summary>
    /// <param name="id">The ID of the fee to edit.</param>
    /// <returns></returns>
    public async Task<IActionResult> Edit(int? id)
    {
        // A fee can't be edited without an ID.
        if (id is null)
        {
            return NotFound();
        }

        // Find the fee by its primary key.
        var fee = await context.Bills.FindAsync(id);

        // Return a 404 if the fee doesn't exist or it is a site charge.
        if (fee is null || fee.Type == BillType.SiteCharge)
        {
            return NotFound();
        }

        // Build the dropdown lists and preselect the fee's current values.
        await PopulateReservationsAsync(fee.ReservationId);
        PopulateFeeTypes(fee.Type);

        return View(fee);
    }

    /// <summary>
    /// Processes the submitted form for editing an existing reservation fee.
    /// </summary>
    /// <param name="id">The fee ID from the route.</param>
    /// <param name="fee">The updated fee information submitted by the user.</param>
    /// <returns></returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ReservationId,Type,Description,Amount,CreatedAt")] Bill fee)
    {
        // This makes sure that the route ID matches the fee submitted in the form
        if (id != fee.Id)
        {
            return NotFound();
        }
        
        // Prevent a reservation fee from being changed into a site fee.
        if (fee.Type == BillType.SiteCharge)
        {
            ModelState.AddModelError(nameof(Bill.Type), "Select a reservation fee type.");
        }
        
        // Confirms that the selected reservation does exist.
        if (!await context.Reservations.AnyAsync(reservation => reservation.Id == fee.ReservationId))
        {
            ModelState.AddModelError(nameof(Bill.ReservationId), "Select a valid reservation.");
        }
        
        // Re display the form when the validation fails.
        if (!ModelState.IsValid)
        {
            await PopulateReservationsAsync(fee.ReservationId);
            PopulateFeeTypes(fee.Type);
            return View(fee);
        }

        try
        {
            // Mark the fee that was submitted as updated and save the changes made.
            context.Update(fee);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // A concurrency exception could happen if another operation
            // modified or deleted the fee before the update was saved.
            if (!await FeeExists(fee.Id))
            {
                return NotFound();
            }

            throw;
        }

        // Return the usesr to the fee list after the update is successful.
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Delete Actions

    /// <summary>
    /// Displays the confirmation page for deleting a reservation fee.
    /// </summary>
    /// <param name="id">The ID of the fee to be deleted</param>
    public async Task<IActionResult> Delete(int? id)
    {
        // A fee can't be deleted with no ID.
        if (id is null)
        {
            return NotFound();
        }

        // Get the fee and its related reservation and customer information
        // so that the confirmation page can show it's details.
        var fee = await context.Bills
            .AsNoTracking()
            .Include(bill => bill.Reservation)
                .ThenInclude(reservation => reservation!.Customer)
            .FirstOrDefaultAsync(bill => bill.Id == id && bill.Type != BillType.SiteCharge);

        // Return 404 if the fee doesn't exist.
        if (fee is null)
        {
            return NotFound();
        }

        return View(fee);
    }

    /// <summary>
    /// Permanently deletes the selected reservation fee after confirmation.
    /// </summary>
    /// <param name="id">The ID of the fee to delete</param>
    /// <returns></returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Get the fee being deleted.
        var fee = await context.Bills.FindAsync(id);

        // Delete the fee if it exists AND it isn't a site charge
        if (fee is not null && fee.Type != BillType.SiteCharge)
        {
            context.Bills.Remove(fee);
            await context.SaveChangesAsync();
        }

        // Return the user to the fee list.
        return RedirectToAction(nameof(Index));
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// Determines whether a non-site charge fee exists.
    /// </summary>
    /// <param name="id">The ID of the fee to find.</param>
    private async Task<bool> FeeExists(int id)
    {
        return await context.Bills.AnyAsync(bill => bill.Id == id && bill.Type != BillType.SiteCharge);
    }

    /// <summary>
    /// Builds the reservation dropdown list used by the Create and Edit views.
    /// </summary>
    /// <param name="selectedReservationId">
    /// The reservation that should be selected when the dropdown is displayed.
    /// </param>
    private async Task PopulateReservationsAsync(int? selectedReservationId = null)
    {
        // Retrieve reservations and create a readable display value
        // containing the reservation number and customer's name.
        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.Customer)
            .OrderBy(reservation => reservation.ReservationNumber)
            .Select(reservation => new
            {
                reservation.Id,
                DisplayName = $"{reservation.ReservationNumber} - {reservation.Customer!.FirstName} {reservation.Customer.LastName}"
            })
            .ToListAsync();

        // Store the dropdown options in ViewData for the view.
        ViewData["ReservationId"] = new SelectList(reservations, "Id", "DisplayName", selectedReservationId);
    }

    /// <summary>
    /// Builds the fee-type dropdown list used by the Create and Edit views.
    /// siteCharge is excluded because it is managed separately. 
    /// </summary>
    /// <param name="selectedFeeType">This is the fee-type that should be selected when the dropdown is displayed.</param>
    private void PopulateFeeTypes(BillType? selectedFeeType = null)
    {
        // Convert the BillType enum into dropdown options while excluding
        // the standard site-charge type.
        var feeTypes = Enum.GetValues<BillType>()
            .Where(type => type != BillType.SiteCharge)
            .Select(type => new SelectListItem
            {
                Value = type.ToString(),
                Text = type.ToString(),
                Selected = selectedFeeType == type
            })
            .ToList();

        // Store the fee-type options in a ViewData for the view.
        ViewData["FeeTypes"] = feeTypes;
    }
    #endregion
}