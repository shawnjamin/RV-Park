using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace RVPark.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Public customer dashboard
        [HttpGet]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> MyReservations([FromServices] ApplicationDbContext _context)
        {
            // Read the Email claim out of the newly implemented Cookie
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // Query the DB for this exact user, including their reservations and sites
            var currentUser = await _context.Users
                .Include(u => u.Reservations)
                    .ThenInclude(r => r.Site)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Sort their trips by date
            var myTrips = currentUser.Reservations
                .OrderBy(r => r.StartDate)
                .ToList();

            return View(myTrips);
        }

        // GET: Reservations (Includes the Search logic from the rubric)
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> Index(string searchQuery)
        {
            var reservations = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchQuery = searchQuery.ToLower();
                reservations = reservations.Where(r => 
                    r.ReservationNumber.ToLower().Contains(searchQuery) ||
                    (r.User != null && r.User.FirstName.ToLower().Contains(searchQuery)) ||
                    (r.User != null && r.User.LastName.ToLower().Contains(searchQuery)));
            }

            // Order by StartDate so upcoming are first
            return View(await reservations.OrderByDescending(r => r.StartDate).ToListAsync());
        }

        // GET: Reservations/Edit/5
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reservation = await _context.Reservations
                .Include(r => r.User)
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
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
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
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
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

        // Placeholder for Employee Walk-in View
        [HttpGet]
        [Authorize(Roles = "Manager, Admin")]
        public IActionResult EmployeeCreate()
        {
            return View();
        }

        // Placeholder for Public Customer Edit View
        [HttpGet]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public IActionResult EditMyTrip(int id)
        {
            var mockReservation = new Reservation
            {
                Id = id,
                ReservationNumber = "RES-9999",
                StartDate = DateTime.Now.AddDays(10),
                EndDate = DateTime.Now.AddDays(14)
            };
            return View(mockReservation);
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