using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RVPark.Controllers
{
    [Authorize(Roles = "Employee, Manager, Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Login GET
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            // Today's Check-ins (Reservations starting today that aren't cancelled)
            ViewBag.TodaysCheckIns = await _context.Reservations
                .Where(r => r.StartDate.Date == today && r.Status != ReservationStatus.Cancelled)
                .CountAsync();

            // Active Sites vs Total Sites
            ViewBag.ActiveSites = await _context.Sites.CountAsync(s => s.IsActive);
            ViewBag.TotalSites = await _context.Sites.CountAsync();

            // Pending Payments (Bills that have no payments associated yet)
            ViewBag.PendingPayments = await _context.Bills
                .Where(b => !_context.Payments.Any(p => p.BillId == b.Id))
                .CountAsync();

            // Needs Attention (e.g., Reservations cancelled today)
            ViewBag.NeedsAttention = await _context.Reservations
                .Where(r => r.CancelledAt != null && r.CancelledAt.Value.Date == today)
                .CountAsync();

            return View();
        }
    }
}