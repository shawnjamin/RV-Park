using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reservations (Includes the Search logic from the rubric)
        public async Task<IActionResult> Index(string searchQuery)
        {
            var reservations = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Site)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.ToLower();
                reservations = reservations.Where(r => 
                    r.ReservationNumber.ToLower().Contains(searchQuery) ||
                    (r.Customer != null && r.Customer.FirstName.ToLower().Contains(searchQuery)) ||
                    (r.Customer != null && r.Customer.LastName.ToLower().Contains(searchQuery)));
            }

            // Order by StartDate so upcoming are first
            return View(await reservations.OrderByDescending(r => r.StartDate).ToListAsync());
        }

        // GET: Reservations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Site)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null) return NotFound();

            // Populate the ViewBag with available sites
            ViewBag.AvailableSites = new SelectList(_context.Sites.Where(s => s.IsActive), "Id", "SiteNumber", reservation.SiteId);

            return View(reservation);
        }

        // POST: Reservations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StartDate,EndDate,SiteId")] Reservation updateParams)
        {
            if (id != updateParams.Id) return NotFound();

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null) return NotFound();

            // Update the editable fields
            reservation.StartDate = updateParams.StartDate;
            reservation.EndDate = updateParams.EndDate;
            reservation.SiteId = updateParams.SiteId;

            try
            {
                _context.Update(reservation);
                await _context.SaveChangesAsync();
                // Redirect back to Index after successful save
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(reservation.Id)) return NotFound();
                else throw;
            }
        }

        // POST: Reservations/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledAt = DateTime.UtcNow;
                
                _context.Update(reservation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSites(DateTime startDate, DateTime endDate, int currentReservationId)
        {
            var overlappingReservations = await _context.Reservations
                .Where(r => r.Id != currentReservationId && r.Status != RVPark.Models.ReservationStatus.Cancelled)
                .Where(r => startDate < r.EndDate && endDate > r.StartDate)
                .Select(r => r.SiteId)
                .ToListAsync();

            var availableSites = await _context.Sites
                .Where(s => s.IsActive && !overlappingReservations.Contains(s.Id))
                .Select(s => new {
                    id = s.Id,
                    text = s.SiteNumber + " - Available"
                })
                .ToListAsync();

            return Json(availableSites);
        }
    }
}