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

        // Public customer dashboard
        [HttpGet]
        public IActionResult MyReservations()
        {
            // Mock data for UI testing
            var mockReservations = new List<Reservation>
            {
                new Reservation 
                { 
                    Id = 1, 
                    ReservationNumber = "RES-1042", 
                    StartDate = DateTime.Now.AddDays(12), 
                    EndDate = DateTime.Now.AddDays(15),
                    Status = ReservationStatus.Confirmed,
                    Site = new Site { SiteNumber = "B14" }
                }
            };

            return View(mockReservations);
        }

        // GET: Reservations (Includes the Search logic from the rubric)
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

        // Loads the employee walk-in reservation form.
        [HttpGet]
        public async Task<IActionResult> EmployeeCreate()
        {
            var customers = await _context.Users
                .AsNoTracking()
                .Where(user => user.AccessLevel == AccessLevel.Customer)
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .ToListAsync();

            var activeSites = await _context.Sites
                .AsNoTracking()
                .Where(site => site.IsActive)
                .OrderBy(site => site.SiteNumber)
                .ToListAsync();

            ViewBag.Customers = new SelectList(
                customers,
                "Id",
                "Email");

            ViewBag.Sites = new SelectList(
                activeSites,
                "Id",
                "SiteNumber");

            ViewBag.PaymentMethods = new SelectList(
                Enum.GetValues<PaymentMethod>()
                    .Where(method => method != PaymentMethod.Stripe));

            var viewModel = new EmployeeReservationFormViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                AdultCount = 1
            };

            return View(viewModel);

        }

        // Placeholder for Public Customer Edit View
        [HttpGet]
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