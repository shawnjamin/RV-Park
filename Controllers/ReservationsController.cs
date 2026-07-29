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

        // Displays the public customer reservation dashboard.
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
        // Displays all reservations and optionally filters them by reservation
        // number or customer name.
        public async Task<IActionResult> Index(string searchQuery)
        {
            var reservations = _context.Reservations
                .Include(reservation => reservation.User)
                .Include(reservation => reservation.Site)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedSearch = searchQuery.Trim().ToLower();

                reservations = reservations.Where(reservation =>
                    reservation.ReservationNumber.ToLower().Contains(normalizedSearch) ||
                    (reservation.User != null &&
                     reservation.User.FirstName.ToLower().Contains(normalizedSearch)) ||
                    (reservation.User != null &&
                     reservation.User.LastName.ToLower().Contains(normalizedSearch)));
            }

            return View(await reservations
                .OrderByDescending(reservation => reservation.StartDate)
                .ToListAsync());
        }


        // GET: Reservations/Edit/5
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(item => item.User)
                .Include(item => item.Site)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (reservation is null)
            {
                return NotFound();
            }

            // Populate the site dropdown with active sites.
            ViewBag.AvailableSites = new SelectList(
                _context.Sites.Where(site => site.IsActive),
                "Id",
                "SiteNumber",
                reservation.SiteId);

            return View(reservation);
        }

        // Saves editable reservation fields.
        [HttpPost]
        [ValidateAntiForgeryToken]

        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,StartDate,EndDate,SiteId")] Reservation updateParams)
        {
            if (id != updateParams.Id)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation is null)
            {
                return NotFound();
            }

            reservation.StartDate = updateParams.StartDate;
            reservation.EndDate = updateParams.EndDate;
            reservation.SiteId = updateParams.SiteId;

            try
            {
                _context.Update(reservation);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReservationExists(reservation.Id))
                {
                    return NotFound();
                }

                throw;
            }
        }

        // Cancels a reservation without deleting it from the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation is not null)
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledAt = DateTime.UtcNow;

                _context.Update(reservation);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Checks whether a reservation still exists.
        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(reservation => reservation.Id == id);
        }

        // Loads the employee walk-in reservation form.
        [HttpGet]
        [Authorize(Roles = "Manager, Admin")]
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

        // Creates a walk-in reservation, customer record when necessary,
        // site-charge bill, and manual payment.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmployeeCreate(
            EmployeeReservationFormViewModel viewModel)
        {
            // Check-in cannot be earlier than today.
            if (viewModel.StartDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(viewModel.StartDate),
                    "The check-in date cannot be in the past.");
            }

            // Check-out must occur after check-in.
            if (viewModel.EndDate.Date <= viewModel.StartDate.Date)
            {
                ModelState.AddModelError(
                    nameof(viewModel.EndDate),
                    "The check-out date must be after the check-in date.");
            }

            User? existingCustomer = null;

            // When an existing customer was selected, verify that the record
            // exists and belongs to a customer account.
            if (viewModel.CustomerId.HasValue &&
                viewModel.CustomerId.Value > 0)
            {
                existingCustomer = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user =>
                        user.Id == viewModel.CustomerId.Value &&
                        user.AccessLevel == AccessLevel.Customer);

                if (existingCustomer is null)
                {
                    ModelState.AddModelError(
                        nameof(viewModel.CustomerId),
                        "The selected customer could not be found.");
                }
            }
            else
            {
                // A new walk-in customer requires basic contact information.
                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerFirstName))
                {
                    ModelState.AddModelError(
                        nameof(viewModel.NewCustomerFirstName),
                        "The customer's first name is required.");
                }

                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerLastName))
                {
                    ModelState.AddModelError(
                        nameof(viewModel.NewCustomerLastName),
                        "The customer's last name is required.");
                }

                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerEmail))
                {
                    ModelState.AddModelError(
                        nameof(viewModel.NewCustomerEmail),
                        "The customer's email is required.");
                }

                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerPhone))
                {
                    ModelState.AddModelError(
                        nameof(viewModel.NewCustomerPhone),
                        "The customer's phone number is required.");
                }

                // Prevent duplicate user accounts with the same email address.
                if (!string.IsNullOrWhiteSpace(viewModel.NewCustomerEmail))
                {
                    var normalizedEmail =
                        viewModel.NewCustomerEmail.Trim().ToLowerInvariant();

                    var emailAlreadyExists = await _context.Users
                        .AnyAsync(user =>
                            user.Email.ToLower() == normalizedEmail);

                    if (emailAlreadyExists)
                    {
                        ModelState.AddModelError(
                            nameof(viewModel.NewCustomerEmail),
                            "An account with this email already exists. Select the existing customer instead.");
                    }
                }
            }

            // Load the selected active site and its pricing information.
            var site = await _context.Sites
                .AsNoTracking()
                .Include(item => item.SiteType)
                .FirstOrDefaultAsync(item =>
                    item.Id == viewModel.SiteId &&
                    item.IsActive);

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

            // Stripe is reserved for the online checkout workflow.
            if (viewModel.PaymentMethod == PaymentMethod.Stripe)
            {
                ModelState.AddModelError(
                    nameof(viewModel.PaymentMethod),
                    "Stripe cannot be used for a manual payment.");
            }

            // Prevent double-booking the selected site.
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

            // Reload dropdown values before returning an invalid form.
            if (!ModelState.IsValid)
            {
                await PopulateEmployeeCreateOptionsAsync(
                    viewModel.CustomerId,
                    viewModel.SiteId,
                    viewModel.PaymentMethod);

                return View(viewModel);
            }

            // Calculate the total site charge.
            var numberOfNights =
                (viewModel.EndDate.Date - viewModel.StartDate.Date).Days;

            var nightlyRate = site!.SiteType!.Price;
            var totalAmount = nightlyRate * numberOfNights;

            // Use the existing customer ID when one was selected.
            // A new customer's ID will be assigned after that record is saved.
            var customerId = existingCustomer?.Id ?? 0;

            // Keep customer, reservation, bill, and payment creation together.
            // If one save fails, the entire operation is rolled back.
            await using var databaseTransaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Create a new customer account when the employee did not
                // select an existing customer.
                if (existingCustomer is null)
                {
                    var newCustomer = new User
                    {
                        FirstName = viewModel.NewCustomerFirstName!.Trim(),
                        LastName = viewModel.NewCustomerLastName!.Trim(),
                        Email = viewModel.NewCustomerEmail!.Trim(),
                        Phone = viewModel.NewCustomerPhone!.Trim(),
                        AccessLevel = AccessLevel.Customer,
                        IsLocked = false,

                        // Temporary value used until the project's account setup
                        // or password-reset workflow is connected.
                        PasswordHash = $"TEMP-{Guid.NewGuid():N}",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(newCustomer);
                    await _context.SaveChangesAsync();

                    customerId = newCustomer.Id;
                }

                // Create the confirmed walk-in reservation.
                var reservation = new Reservation
                {
                    ReservationNumber = GenerateReservationNumber(),
                    CustomerId = customerId,
                    SiteId = viewModel.SiteId,
                    StartDate = viewModel.StartDate.Date,
                    EndDate = viewModel.EndDate.Date,
                    AdultCount = viewModel.AdultCount,
                    ChildCount = viewModel.ChildCount,
                    PetCount = viewModel.PetCount,
                    SpecialRequestsOrNotes =
                        viewModel.SpecialRequestsOrNotes,
                    Status = ReservationStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();

                // Create the site-charge bill linked to the reservation.
                var bill = new Bill
                {
                    ReservationId = reservation.Id,
                    Type = BillType.SiteCharge,
                    Description =
                        $"{numberOfNights} night(s) at Site {site.SiteNumber}",
                    Amount = totalAmount,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();

                // Record the employee-processed manual payment.
                var payment = new Payment
                {
                    BillId = bill.Id,
                    PaymentMethod = viewModel.PaymentMethod,
                    StripeTransactionId = null,
                    Notes = viewModel.PaymentNotes,
                    Amount = totalAmount,
                    PaidAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                await databaseTransaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Reservation {reservation.ReservationNumber} was created successfully.";
            }
            catch
            {
                await databaseTransaction.RollbackAsync();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Load the real reservation for the customer to edit
        [HttpGet]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> EditMyTrip(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Site)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Save the customer's changes to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> EditMyTrip(int id, [Bind("StartDate,EndDate")] Reservation updateParams)
        {
            // Fetch the tracked entity directly
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation != null)
            {
                // Modify the tracked properties
                reservation.StartDate = updateParams.StartDate;
                reservation.EndDate = updateParams.EndDate;

                // Let EF Core automatically detect the changes and save them
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Trip dates successfully updated!";
            }

            return RedirectToAction(nameof(MyReservations));
        }

        // POST: Cancels a customer's reservation
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> CancelMyTrip(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledAt = DateTime.UtcNow;

                // Let EF Core automatically detect the changes and save them
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNumber} has been cancelled.";
            }

            return RedirectToAction(nameof(MyReservations));
        }

        // Returns active sites that do not overlap another reservation.
        [HttpGet]
        public async Task<IActionResult> GetAvailableSites(
            DateTime startDate,
            DateTime endDate,
            int currentReservationId)
        {
            var overlappingReservations = await _context.Reservations
                .Where(reservation =>
                    reservation.Id != currentReservationId &&
                    reservation.Status != ReservationStatus.Cancelled)
                .Where(reservation =>
                    startDate < reservation.EndDate &&
                    endDate > reservation.StartDate)
                .Select(reservation => reservation.SiteId)
                .ToListAsync();

            var availableSites = await _context.Sites
                .Where(site =>
                    site.IsActive &&
                    !overlappingReservations.Contains(site.Id))
                .Select(site => new
                {
                    id = site.Id,
                    text = site.SiteNumber + " - Available"
                })
                .ToListAsync();

            return Json(availableSites);
        }

        // Populates the customer, site, and payment-method dropdowns used by
        // the employee walk-in reservation form.
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
                    Text =
                        $"{customer.LastName}, {customer.FirstName} — {customer.Email}",
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

        // Generates a readable reservation number with a date and random suffix.
        private static string GenerateReservationNumber()
        {
            var randomPart = Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();

            return $"RES-{DateTime.UtcNow:yyyyMMdd}-{randomPart}";
        }
    }
}