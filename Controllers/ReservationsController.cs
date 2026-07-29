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
            await PopulateEmployeeCreateOptionsAsync();

            var viewModel = new EmployeeReservationFormViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(1),
                AdultCount = 1
            };

            return View(viewModel);
        }

        // Creates a walk-in reservation with a manual payment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeCreate(
            EmployeeReservationFormViewModel viewModel)
        {
            if (viewModel.StartDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(viewModel.StartDate),
                    "The check-in date cannot be in the past.");
            }

            if (viewModel.EndDate.Date <= viewModel.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(viewModel.EndDate),
                    "The check-out date must be after the check-in date.");
            }

            var customerExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(user =>
                    user.Id == viewModel.CustomerId &&
                    user.AccessLevel == AccessLevel.Customer);

            if (!customerExists)
            {
                ModelState.AddModelError(
                    nameof(viewModel.CustomerId),
                    "The selected customer could not be found.");
            }

            var site = await _context.Sites
                .AsNoTracking()
                .Include(site => site.SiteType)
                .FirstOrDefaultAsync(site =>
                    site.Id == viewModel.SiteId &&
                    site.IsActive);

            if (site is null)
            {
                ModelState.AddModelError(
                    nameof(viewModel.SiteId),
                    "The selected site is unavailable or inactive.");
            }
            else if (site.SiteType is null)
            {
                ModelState.AddModelError(
                    nameof(viewModel.SiteId),
                    "The selected site does not have pricing information.");
            }

            if (viewModel.PaymentMethod == PaymentMethod.Stripe)
            {
                ModelState.AddModelError(
                    nameof(viewModel.PaymentMethod),
                    "Stripe cannot be used for a manual payment.");
            }

            if (site is not null &&
                viewModel.EndDate.Date > viewModel.StartDate.Date)
            {
                var siteIsReserved = await _context.Reservations
                    .AnyAsync(reservation =>
                        reservation.SiteId == viewModel.SiteId &&
                        reservation.Status != ReservationStatus.Cancelled &&
                        reservation.Status != ReservationStatus.Completed &&
                        reservation.StartDate < viewModel.EndDate.Date &&
                        reservation.EndDate > viewModel.StartDate.Date);

                if (siteIsReserved)
                {
                    ModelState.AddModelError(
                        nameof(viewModel.SiteId),
                        "The selected site is already reserved during those dates.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateEmployeeCreateOptionsAsync(
                    viewModel.CustomerId,
                    viewModel.SiteId,
                    viewModel.PaymentMethod);

                return View(viewModel);
            }

            // Calculate the number of nights and total cost.
            var numberOfNights =
                (viewModel.EndDate.Date - viewModel.StartDate.Date).Days;

            var nightlyRate = site!.SiteType!.Price;
            var totalAmount = nightlyRate * numberOfNights;

            // Create the reservation record.
            var reservation = new Reservation
            {
                ReservationNumber = GenerateReservationNumber(),
                CustomerId = viewModel.CustomerId,
                SiteId = viewModel.SiteId,
                StartDate = viewModel.StartDate.Date,
                EndDate = viewModel.EndDate.Date,
                AdultCount = viewModel.AdultCount,
                ChildCount = viewModel.ChildCount,
                PetCount = viewModel.PetCount,
                SpecialRequestsOrNotes = viewModel.SpecialRequestsOrNotes,
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            // Create the site-charge bill.
            var bill = new Bill
            {
                Type = BillType.SiteCharge,
                Description =
                    $"{numberOfNights} night(s) at Site {site.SiteNumber}",
                Amount = totalAmount,
                CreatedAt = DateTime.UtcNow
            };

            // Create the manual payment.
            var payment = new Payment
            {
                PaymentMethod = viewModel.PaymentMethod,
                StripeTransactionId = null,
                Notes = viewModel.PaymentNotes,
                Amount = totalAmount,
                PaidAt = DateTime.UtcNow
            };

            // Keep all three database operations together.
            await using var databaseTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Save first so the reservation receives its generated ID.
                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Connect the bill to the reservation.
                bill.ReservationId = reservation.Id;
                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                // Connect the payment to the bill.
                payment.BillId = bill.Id;
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await databaseTransaction.CommitAsync();
            }
            catch
            {
                await databaseTransaction.RollbackAsync();
                throw;
            }

            TempData["SuccessMessage"] =
                $"Reservation {reservation.ReservationNumber} was created successfully.";

            return RedirectToAction(nameof(Index));
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

        private async Task PopulateEmployeeCreateOptionsAsync(
            int? selectedCustomerId = null,
            int? selectedSiteId = null,
            PaymentMethod? selectedPaymentMethod = null)
        {
            var customers = await _context.Users
                .AsNoTracking()
                .Where(user => user.AccessLevel == AccessLevel.Customer)
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .ToListAsync();

            ViewBag.Customers = customers
                .Select(customer => new SelectListItem
                {
                    Value = customer.Id.ToString(),
                    Text = $"{customer.LastName}, {customer.FirstName} — {customer.Email}",
                    Selected = customer.Id == selectedCustomerId
                })
                .ToList();

            var activeSites = await _context.Sites
                .AsNoTracking()
                .Where(site => site.IsActive)
                .OrderBy(site => site.SiteNumber)
                .ToListAsync();

            ViewBag.Sites = new SelectList(
                activeSites,
                "Id",
                "SiteNumber",
                selectedSiteId);

            ViewBag.PaymentMethods = Enum.GetValues<PaymentMethod>()
                .Where(method => method != PaymentMethod.Stripe)
                .Select(method => new SelectListItem
                {
                    Value = method.ToString(),
                    Text = method == PaymentMethod.Card
                        ? "Credit Card (Manual Entry)"
                        : method.ToString(),
                    Selected = method == selectedPaymentMethod
                })
                .ToList();
        }

        private static string GenerateReservationNumber()
        {
            var randomPart = Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();

            return $"RES-{DateTime.UtcNow:yyyyMMdd}-{randomPart}";
        }
    }
}