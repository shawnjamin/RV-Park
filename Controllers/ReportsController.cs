using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

/// <summary>
/// Generates reservation reports for a selected date range.
/// </summary>
public class ReportsController(ApplicationDbContext context) : Controller
{

    #region Report Actions

    /// <summary>
    /// Displays the reservation report page and generates a report
    /// when both a start date and end date are provided.
    /// </summary>
    /// <param name="startDate">The beginning of the requested report period.</param>
    /// <param name="endDate">The end of the requested report period.</param>
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
    {
        // Create the view model and preserve the selected date range
        // so the dates remain visible in the report form.
        var viewModel = new ReservationReportViewModel
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Display the report form without querying the database
        // until the user provides both dates.
        if (startDate is null || endDate is null)
        {
            return View(viewModel);
        }

        // Prevent the report from running when the date range is invalid.
        if (endDate < startDate)
        {
            ModelState.AddModelError(string.Empty, "End date must be after start date.");
            return View(viewModel);
        }

        // Retrieve reservations that overlap the selected date range.
        // Customer and site information are included because both are
        // displayed in the final report.
        var reservations = await context.Reservations
            .AsNoTracking()
            .Include(reservation => reservation.Customer)
            .Include(reservation => reservation.Site)
            .Where(reservation =>
                reservation.StartDate.Date <= endDate.Value.Date &&
                reservation.EndDate.Date >= startDate.Value.Date)
            .ToListAsync();

        // Convert each reservation entity into a report-specific row.
        viewModel.Reservations = reservations
            .Select(reservation =>
            {
                // Translate the system reservation status into a simplified
                // report status and determine its sorting priority.
                var reportStatus = GetReportStatus(reservation);

                return new ReservationReportRowViewModel
                {
                    ReservationNumber = reservation.ReservationNumber,

                    // Combine the customer's first and last names while
                    // removing unnecessary spaces if either value is missing.
                    CustomerName = $"{reservation.Customer?.FirstName} {reservation.Customer?.LastName}".Trim(),
                    Phone = reservation.Customer?.Phone,
                    Email = reservation.Customer?.Email,

                    // Display a fallback value if no site has been assigned.
                    SiteNumber = reservation.Site?.SiteNumber ?? "Unassigned",
                    StartDate = reservation.StartDate,
                    EndDate = reservation.EndDate,

                    // Preserve the original reservation status while also
                    // providing the simplified status used by the report.
                    ReservationStatus = reservation.Status,
                    ReportStatus = reportStatus.Name,
                    SortOrder = reportStatus.SortOrder
                };
            })
            // Group rows by report status priority.
            .OrderBy(row => row.SortOrder)

            // Within each status, show earlier reservations first.
            .ThenBy(row => row.StartDate)

            // Use the customer name as a final alphabetical tie-breaker.
            .ThenBy(row => row.CustomerName)
            .ToList();
        // Send the completed report data to the Index view.
        return View(viewModel);
    }
    #endregion

    #region Helper Methods

    /// <summary>
    /// Converts a reservation's internal status into a simplified
    /// report status and sorting priority.
    /// </summary>
    /// <param name="reservation">
    /// The reservation whose status should be evaluated.
    /// </param>
    /// <returns>
    /// A tuple containing the display status and its report sort order.
    /// </returns>
    private static (string Name, int SortOrder) GetReportStatus(Reservation reservation)
    {
        // Completed reservations appear first in the report.
        if (reservation.Status == ReservationStatus.Completed)
        {
            return ("Completed", 1);
        }

        // Checked-in reservations are currently active.
        if (reservation.Status == ReservationStatus.CheckedIn)
        {
            return ("In Progress", 2);
        }

        // Confirmed and pending-payment reservations have not started yet.
        if (reservation.Status is ReservationStatus.Confirmed or ReservationStatus.PendingPayment)
        {
            return ("Upcoming", 3);
        }

        // Any remaining status is treated as cancelled for report purposes.
        return ("Cancelled", 4);
    }
    #endregion
}