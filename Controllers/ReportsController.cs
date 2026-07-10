using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

public class ReportsController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
    {
        var viewModel = new ReservationReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate
        };

        if (startDate is null || endDate is null)
        {
            return View(viewModel);
        }

        if (endDate < startDate)
        {
            ModelState.AddModelError(string.Empty, "End date must be after start date.");
            return View(viewModel);
        }

        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.Customer)
            .Include(reservation => reservation.Site)
            .Where(reservation =>
                reservation.StartDate.Date <= endDate.Value.Date &&
                reservation.EndDate.Date >= startDate.Value.Date)
            .ToListAsync();

        viewModel.Reservations = reservations
            .Select(reservation =>
            {
                var reportStatus = GetReportStatus(reservation);

                return new ReservationReportRowViewModel
                {
                    ReservationNumber = reservation.ReservationNumber,
                    CustomerName = $"{reservation.Customer?.FirstName} {reservation.Customer?.LastName}".Trim(),
                    Phone = reservation.Customer?.Phone,
                    Email = reservation.Customer?.Email,
                    SiteNumber = reservation.Site?.SiteNumber ?? "Unassigned",
                    StartDate = reservation.StartDate,
                    EndDate = reservation.EndDate,
                    ReservationStatus = reservation.Status,
                    ReportStatus = reportStatus.Name,
                    SortOrder = reportStatus.SortOrder
                };
            })
            .OrderBy(row => row.SortOrder)
            .ThenBy(row => row.StartDate)
            .ThenBy(row => row.CustomerName)
            .ToList();

        return View(viewModel);
    }

    private static (string Name, int SortOrder) GetReportStatus(Reservation reservation)
    {
        if (reservation.Status == ReservationStatus.Completed)
        {
            return ("Completed", 1);
        }

        if (reservation.Status == ReservationStatus.CheckedIn)
        {
            return ("In Progress", 2);
        }

        if (reservation.Status is ReservationStatus.Confirmed or ReservationStatus.PendingPayment)
        {
            return ("Upcoming", 3);
        }

        return ("Cancelled", 4);
    }
}