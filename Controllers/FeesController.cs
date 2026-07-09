using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

public class FeesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var fees = await context.Bills
            .AsNoTracking()
            .Include(bill => bill.Reservation)
                .ThenInclude(reservation => reservation!.Customer)
            .Where(bill => bill.Type != BillType.SiteCharge)
            .OrderByDescending(bill => bill.CreatedAt)
            .ToListAsync();

        return View(fees);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var fee = await context.Bills
            .AsNoTracking()
            .Include(bill => bill.Reservation)
                .ThenInclude(reservation => reservation!.Customer)
            .FirstOrDefaultAsync(bill => bill.Id == id && bill.Type != BillType.SiteCharge);

        if (fee is null)
        {
            return NotFound();
        }

        return View(fee);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateReservationsAsync();
        PopulateFeeTypes();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ReservationId,Type,Description,Amount")] Bill fee)
    {
        if (fee.Type == BillType.SiteCharge)
        {
            ModelState.AddModelError(nameof(Bill.Type), "Select a reservation fee type.");
        }

        if (!await context.Reservations.AnyAsync(reservation => reservation.Id == fee.ReservationId))
        {
            ModelState.AddModelError(nameof(Bill.ReservationId), "Select a valid reservation.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateReservationsAsync(fee.ReservationId);
            PopulateFeeTypes(fee.Type);
            return View(fee);
        }

        fee.CreatedAt = DateTime.UtcNow;

        context.Bills.Add(fee);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var fee = await context.Bills.FindAsync(id);

        if (fee is null || fee.Type == BillType.SiteCharge)
        {
            return NotFound();
        }

        await PopulateReservationsAsync(fee.ReservationId);
        PopulateFeeTypes(fee.Type);

        return View(fee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ReservationId,Type,Description,Amount,CreatedAt")] Bill fee)
    {
        if (id != fee.Id)
        {
            return NotFound();
        }

        if (fee.Type == BillType.SiteCharge)
        {
            ModelState.AddModelError(nameof(Bill.Type), "Select a reservation fee type.");
        }

        if (!await context.Reservations.AnyAsync(reservation => reservation.Id == fee.ReservationId))
        {
            ModelState.AddModelError(nameof(Bill.ReservationId), "Select a valid reservation.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateReservationsAsync(fee.ReservationId);
            PopulateFeeTypes(fee.Type);
            return View(fee);
        }

        try
        {
            context.Update(fee);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await FeeExists(fee.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var fee = await context.Bills
            .AsNoTracking()
            .Include(bill => bill.Reservation)
                .ThenInclude(reservation => reservation!.Customer)
            .FirstOrDefaultAsync(bill => bill.Id == id && bill.Type != BillType.SiteCharge);

        if (fee is null)
        {
            return NotFound();
        }

        return View(fee);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var fee = await context.Bills.FindAsync(id);

        if (fee is not null && fee.Type != BillType.SiteCharge)
        {
            context.Bills.Remove(fee);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> FeeExists(int id)
    {
        return await context.Bills.AnyAsync(bill => bill.Id == id && bill.Type != BillType.SiteCharge);
    }

    private async Task PopulateReservationsAsync(int? selectedReservationId = null)
    {
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

        ViewData["ReservationId"] = new SelectList(reservations, "Id", "DisplayName", selectedReservationId);
    }

    private void PopulateFeeTypes(BillType? selectedFeeType = null)
    {
        var feeTypes = Enum.GetValues<BillType>()
            .Where(type => type != BillType.SiteCharge)
            .Select(type => new SelectListItem
            {
                Value = type.ToString(),
                Text = type.ToString(),
                Selected = selectedFeeType == type
            })
            .ToList();

        ViewData["FeeTypes"] = feeTypes;
    }
}