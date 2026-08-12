using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace RVPark.Controllers
{
    // Require authentication for all endpoints in this controller, but restrict roles per-action
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReservationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Public Customer Dashboard View
        [HttpGet]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> MyReservations([FromServices] ApplicationDbContext _context)
        {
            // Retrieve User Email Claim
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            // Query Current User and Associated Records
            var currentUser = await _context.Users
                .Include(u => u.Reservations)
                    .ThenInclude(r => r.Site)
                .FirstOrDefaultAsync(u => u.Email == userEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Order Customer Trips by Date
            var myTrips = currentUser.Reservations
                .OrderBy(r => r.StartDate)
                .ToList();

            return View(myTrips);
        }

        // Administrator and Employee Index Search View (Restricted to Staff/Admin)
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
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

        // Administrator Edit View Loading (Restricted to Staff/Admin)
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
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

            // Active Site Dropdown Population
            ViewBag.AvailableSites = new SelectList(
                _context.Sites.Where(site => site.IsActive),
                "Id",
                "SiteNumber",
                reservation.SiteId);

            return View(reservation);
        }

        // Administrator Edit Save Submission (Restricted to Staff/Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,StartDate,EndDate,SiteId")] Reservation updateParams)
        {
            if (id != updateParams.Id)
            {
                return NotFound();
            }

            // This edit form intentionally posts only editable reservation fields.
            ModelState.Remove(nameof(Reservation.ReservationNumber));

            // Chronological Date Validation
            if (updateParams.EndDate.Date <= updateParams.StartDate.Date)
            {
                ModelState.AddModelError("EndDate", "The check-out date must be after the check-in date.");
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation is null)
            {
                return NotFound();
            }

            // Overlapping Reservation Conflict Verification
            var siteIsReserved = await _context.Reservations
                .AnyAsync(r => r.SiteId == updateParams.SiteId &&
                               r.Id != reservation.Id &&
                               r.Status != ReservationStatus.Cancelled &&
                               r.Status != ReservationStatus.Completed &&
                               r.StartDate < updateParams.EndDate.Date &&
                               r.EndDate > updateParams.StartDate.Date);

            if (siteIsReserved)
            {
                ModelState.AddModelError("SiteId", "The selected site is already reserved during those dates.");
            }

            // If any validation failed, reload the dropdown and return to view
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableSites = new SelectList(
                    _context.Sites.Where(site => site.IsActive),
                    "Id",
                    "SiteNumber",
                    updateParams.SiteId);
                return View(reservation);
            }

            // Update Reservation fields
            reservation.StartDate = updateParams.StartDate;
            reservation.EndDate = updateParams.EndDate;
            reservation.SiteId = updateParams.SiteId;

            // Total Site Charge Recalculation
            // Fetch the newly assigned site to get its specific pricing
            var updatedSite = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s => s.Id == updateParams.SiteId);

            if (updatedSite != null)
            {
                var numberOfNights = (reservation.EndDate.Date - reservation.StartDate.Date).Days;
                var nightlyRate = updatedSite.SiteType?.Price ?? 0;
                var newTotalAmount = nightlyRate * numberOfNights;

                var siteChargeBill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.ReservationId == reservation.Id && b.Type == BillType.SiteCharge);

                if (siteChargeBill != null)
                {
                    siteChargeBill.Amount = newTotalAmount;
                    siteChargeBill.Description = $"{numberOfNights} night(s) at Site {updatedSite.SiteNumber}";
                    _context.Update(siteChargeBill);
                }
            }

            try
            {
                _context.Update(reservation);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNumber} successfully updated.";
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

        // Administrator Cancel Submission (Restricted to Staff/Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
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

        // Database Verification for Reservation Existence
        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(reservation => reservation.Id == id);
        }

        // Employee Walk-In Form Loading
        [HttpGet]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
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

        // Employee Walk-In Form Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
        public async Task<IActionResult> EmployeeCreate(
            EmployeeReservationFormViewModel viewModel,
            [FromServices] IPasswordHasher<User> passwordHasher)
        {
            // Past Date Validation Check
            if (viewModel.StartDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(viewModel.StartDate), "The check-in date cannot be in the past.");
            }

            // Chronological Order Validation Check
            if (viewModel.EndDate.Date <= viewModel.StartDate.Date)
            {
                ModelState.AddModelError(nameof(viewModel.EndDate), "The check-out date must be after the check-in date.");
            }

            User? existingCustomer = null;

            // Existing Customer Account Verification
            if (viewModel.CustomerId.HasValue && viewModel.CustomerId.Value > 0)
            {
                existingCustomer = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user => user.Id == viewModel.CustomerId.Value && user.AccessLevel == AccessLevel.Customer);

                if (existingCustomer is null)
                {
                    ModelState.AddModelError(nameof(viewModel.CustomerId), "The selected customer could not be found.");
                }
            }
            else
            {
                // New Walk-In Customer Field Validation
                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerFirstName)) ModelState.AddModelError(nameof(viewModel.NewCustomerFirstName), "The customer's first name is required.");
                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerLastName)) ModelState.AddModelError(nameof(viewModel.NewCustomerLastName), "The customer's last name is required.");
                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerEmail)) ModelState.AddModelError(nameof(viewModel.NewCustomerEmail), "The customer's email is required.");
                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerPhone)) ModelState.AddModelError(nameof(viewModel.NewCustomerPhone), "The customer's phone number is required.");
                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerPassword)) ModelState.AddModelError(nameof(viewModel.NewCustomerPassword), "A password is required.");
                if (string.IsNullOrWhiteSpace(viewModel.NewCustomerConfirmPassword)) ModelState.AddModelError(nameof(viewModel.NewCustomerConfirmPassword), "Password confirmation is required.");
                else if (viewModel.NewCustomerPassword != viewModel.NewCustomerConfirmPassword) ModelState.AddModelError(nameof(viewModel.NewCustomerConfirmPassword), "Passwords do not match.");

                // Email Duplication Check
                if (!string.IsNullOrWhiteSpace(viewModel.NewCustomerEmail))
                {
                    var normalizedEmail = viewModel.NewCustomerEmail.Trim().ToLowerInvariant();
                    var emailAlreadyExists = await _context.Users.AnyAsync(user => user.Email.ToLower() == normalizedEmail);
                    if (emailAlreadyExists)
                    {
                        ModelState.AddModelError(nameof(viewModel.NewCustomerEmail), "An account with this email already exists. Select the existing customer instead.");
                    }
                }
            }

            // Site Pricing and Availability Check
            var site = await _context.Sites
                .AsNoTracking()
                .Include(item => item.SiteType)
                .FirstOrDefaultAsync(item => item.Id == viewModel.SiteId && item.IsActive);

            if (site is null)
            {
                ModelState.AddModelError(nameof(viewModel.SiteId), "The selected site is unavailable or inactive.");
            }
            else if (site.SiteType is null)
            {
                ModelState.AddModelError(nameof(viewModel.SiteId), "The selected site does not have pricing information.");
            }

            // Stripe Prohibition for Manual Employee Entries
            if (viewModel.PaymentMethod == PaymentMethod.Stripe)
            {
                ModelState.AddModelError(nameof(viewModel.PaymentMethod), "Stripe cannot be used for a manual payment.");
            }

            // Double Booking Validation Check
            if (site is not null && viewModel.EndDate.Date > viewModel.StartDate.Date)
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
                    ModelState.AddModelError(nameof(viewModel.SiteId), "The selected site is already reserved during those dates.");
                }
            }

            // Dropdown Value Reload for Invalid Form Rendering
            if (!ModelState.IsValid)
            {
                await PopulateEmployeeCreateOptionsAsync(viewModel.CustomerId, viewModel.SiteId, viewModel.PaymentMethod);
                return View(viewModel);
            }

            // Total Site Charge Calculation
            var numberOfNights = (viewModel.EndDate.Date - viewModel.StartDate.Date).Days;
            var nightlyRate = site!.SiteType!.Price;
            var totalAmount = nightlyRate * numberOfNights;

            // Customer ID Assignment
            var customerId = existingCustomer?.Id ?? 0;

            // Database Transaction Isolation Block
            await using var databaseTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // New Customer Record Creation
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
                        PasswordHash = string.Empty,
                        CreatedAt = DateTime.UtcNow
                    };

                    newCustomer.PasswordHash = passwordHasher.HashPassword(newCustomer, viewModel.NewCustomerPassword!);

                    _context.Users.Add(newCustomer);
                    await _context.SaveChangesAsync();
                    customerId = newCustomer.Id;
                }

                // Military Verification Note Formatting
                var finalNotes = viewModel.SpecialRequestsOrNotes;
                if (viewModel.IsMilitary)
                {
                    finalNotes = string.IsNullOrEmpty(finalNotes) 
                        ? "[MILITARY ID VERIFIED]" 
                        : $"[MILITARY ID VERIFIED] {finalNotes}";
                }

                // Unpaid Reservation Record Creation
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
                    SpecialRequestsOrNotes = finalNotes,
                    Status = ReservationStatus.PendingPayment, // Default to pending payment!
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();


                // Linked Reservation Bill Generation
                var bill = new Bill
                {
                    ReservationId = reservation.Id,
                    Type = BillType.SiteCharge,
                    Description = $"{numberOfNights} night(s) at Site {site.SiteNumber}",
                    Amount = totalAmount,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Bills.Add(bill);
                await _context.SaveChangesAsync();


                // Card payments continue to manual card entry
                if (viewModel.PaymentMethod == PaymentMethod.Card)
                {
                    await databaseTransaction.CommitAsync();

                    return RedirectToAction(
                        nameof(ManualCardEntry),
                        new { reservationId = reservation.Id });
                }


                // Cash and Check payments are recorded immediately
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
                reservation.Status = ReservationStatus.Confirmed;

                await _context.SaveChangesAsync();
                await databaseTransaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"{viewModel.PaymentMethod} payment for reservation {reservation.ReservationNumber} was processed successfully.";

                return RedirectToAction(nameof(Details), new { id = reservation.Id });
            }
            catch
            {
                await databaseTransaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
        public async Task<IActionResult> ManualCardEntry(int reservationId)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b =>
                    b.ReservationId == reservationId &&
                    b.Type == BillType.SiteCharge);

            if (bill == null)
            {
                return NotFound();
            }

            // Auto-generate authorization reference number
            var referenceNumber =
                $"AUTH-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

            ViewBag.ReservationId = reservation.Id;
            ViewBag.AmountDue = bill.Amount;
            ViewBag.ReferenceNumber = referenceNumber;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
        public async Task<IActionResult> ProcessManualCard(
            int reservationId,
            string cardholderName,
            string lastFourDigits,
            string referenceNumber,
            string? paymentNotes)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                return NotFound();
            }

            var bill = await _context.Bills
                .FirstOrDefaultAsync(b =>
                    b.ReservationId == reservationId &&
                    b.Type == BillType.SiteCharge);

            if (bill == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(cardholderName) ||
                string.IsNullOrWhiteSpace(lastFourDigits) ||
                lastFourDigits.Length != 4 ||
                !lastFourDigits.All(char.IsDigit))
            {
                TempData["ErrorMessage"] =
                    "Please enter valid card payment information.";

                return RedirectToAction(
                    nameof(ManualCardEntry),
                    new { reservationId });
            }

            var payment = new Payment
            {
                BillId = bill.Id,
                PaymentMethod = PaymentMethod.Card,
                StripeTransactionId = null,
                Amount = bill.Amount,
                Notes =
                    $"Cardholder: {cardholderName}; " +
                    $"Card ending in {lastFourDigits}; " +
                    $"Reference: {referenceNumber}" +
                    (string.IsNullOrWhiteSpace(paymentNotes)
                        ? ""
                        : $"; Notes: {paymentNotes}"),
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Card payment for reservation {reservation.ReservationNumber} was processed successfully.";

            return RedirectToAction(nameof(Edit), new { id = reservation.Id });
        }

        // Live Pricing API Endpoint for Walk-In Form UI
        [HttpGet]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
        public async Task<IActionResult> CalculatePrice(int siteId, DateTime startDate, DateTime endDate)
        {
            if (endDate <= startDate)
            {
                return Json(new { success = false, message = "Check-out must be after check-in." });
            }

            var site = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || !site.IsActive || site.SiteType == null)
            {
                return Json(new { success = false, message = "Invalid site or missing pricing." });
            }

            var numberOfNights = (endDate.Date - startDate.Date).Days;
            var total = site.SiteType.Price * numberOfNights;

            return Json(new { success = true, total = total });
        }

        // Public Customer Checkout Page Loading
        [HttpGet]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")] 
        public async Task<IActionResult> Create(int siteId, string checkIn, string checkOut)
        {
            // Query String Date Validation
            if (!DateTime.TryParse(checkIn, out DateTime startDate) || 
                !DateTime.TryParse(checkOut, out DateTime endDate))
            {
                TempData["ErrorMessage"] = "Please select a valid Check-In and Check-Out Date";
                return RedirectToAction("Browse", "RVSites");
            }

            // Checkout Page Site Information Retrieval
            var site = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s => s.Id == siteId);

            if (site == null || !site.IsActive)
            {
                TempData["ErrorMessage"] = "The selected site is no longer available.";
                return RedirectToAction("Index", "Home");
            }

            // Double Booking Availability Verification
            var siteIsReserved = await _context.Reservations
                .AnyAsync(r => r.SiteId == siteId &&
                               r.Status != ReservationStatus.Cancelled &&
                               r.Status != ReservationStatus.Completed &&
                               r.StartDate < endDate.Date &&
                               r.EndDate > startDate.Date);

            if (siteIsReserved)
            {
                TempData["ErrorMessage"] = "Sorry, that site was just booked by someone else!";
                return RedirectToAction("Index", "Home");
            }

            // Summary Card Cost Calculation
            var numberOfNights = (endDate.Date - startDate.Date).Days;
            var nightlyRate = site.SiteType?.Price ?? 0;
            var totalAmount = nightlyRate * numberOfNights;

            // View Data Assignment
            ViewBag.Site = site;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.NumberOfNights = numberOfNights;
            ViewBag.TotalAmount = totalAmount;

            // Blank Reservation Model Initialization
            var reservation = new Reservation
            {
                SiteId = siteId,
                StartDate = startDate,
                EndDate = endDate,
                AdultCount = 1 
            };

            return View(reservation);
        }

        // Public Customer Checkout Form Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> Create(
            [Bind("SiteId,StartDate,EndDate,AdultCount,ChildCount,PetCount,SpecialRequestsOrNotes")] Reservation reservation, 
            string? stripeToken = null) // <-- Added Stripe Token Parameter
        {
            // Customer Context Identification
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Site Pricing Verification
            var site = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(s => s.Id == reservation.SiteId);

            if (site == null)
            {
                 TempData["ErrorMessage"] = "Error processing reservation. Site not found.";
                 return RedirectToAction("Index", "Home");
            }

            // Check-out Date Validation
            if (reservation.EndDate.Date <= reservation.StartDate.Date)
            {
                 ModelState.AddModelError("EndDate", "Check-out date must be after check-in date.");
            }

            if (ModelState.IsValid)
            {
                // Pricing Assessment
                var numberOfNights = (reservation.EndDate.Date - reservation.StartDate.Date).Days;
                var totalAmount = (site.SiteType?.Price ?? 0) * numberOfNights;

                // Database Transaction Block Initialization
                await using var databaseTransaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // New Reservation Record Commit
                    reservation.CustomerId = customer.Id;
                    reservation.ReservationNumber = GenerateReservationNumber();
                    // Status depends on if payment data was received
                    reservation.Status = string.IsNullOrEmpty(stripeToken) ? ReservationStatus.PendingPayment : ReservationStatus.Confirmed;
                    reservation.CreatedAt = DateTime.UtcNow;

                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();

                    // Generated Bill Commitment (ALWAYS CREATE THE BILL)
                    var bill = new Bill
                    {
                        ReservationId = reservation.Id,
                        Type = BillType.SiteCharge,
                        Description = $"{numberOfNights} night(s) at Site {site.SiteNumber}",
                        Amount = totalAmount,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Bills.Add(bill);
                    await _context.SaveChangesAsync();

                    // ONLY log a payment if Stripe actually sent us a transaction token!
                    if (!string.IsNullOrEmpty(stripeToken))
                    {
                        var payment = new Payment
                        {
                            BillId = bill.Id,
                            PaymentMethod = PaymentMethod.Stripe,
                            StripeTransactionId = stripeToken, 
                            Notes = "Online booking payment",
                            Amount = totalAmount,
                            PaidAt = DateTime.UtcNow
                        };

                        _context.Payments.Add(payment);
                        await _context.SaveChangesAsync();
                    }

                    // Final Transaction Persistence
                    await databaseTransaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Booking confirmed! Your reservation number is {reservation.ReservationNumber}.";
                    
                    return RedirectToAction(nameof(MyReservations));
                }
                catch
                {
                    await databaseTransaction.RollbackAsync();
                    TempData["ErrorMessage"] = "An error occurred while processing your booking. Please try again.";
                }
            }

            // Form Rejection Render Variables Payload
            ViewBag.Site = site;
            ViewBag.StartDate = reservation.StartDate;
            ViewBag.EndDate = reservation.EndDate;
            ViewBag.NumberOfNights = (reservation.EndDate.Date - reservation.StartDate.Date).Days;
            ViewBag.TotalAmount = (site.SiteType?.Price ?? 0) * ViewBag.NumberOfNights;

            return View(reservation);
        }

        // Active Customer Reservation Editor Loading
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

            // Past Booking Modification Protection
            if (reservation.StartDate.Date <= DateTime.Today)
            {
                TempData["ErrorMessage"] = "Trips that have already started or passed cannot be modified.";
                return RedirectToAction(nameof(MyReservations));
            }

            return View(reservation);
        }

       // Active Customer Reservation Database Modification Form
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> EditMyTrip(int id, [Bind("StartDate,EndDate")] Reservation updateParams)
        {
            // Chronological Validation Logic
            if (updateParams.EndDate <= updateParams.StartDate)
            {
                TempData["ErrorMessage"] = "Check-out date must be after your check-in date.";
                return RedirectToAction(nameof(EditMyTrip), new { id = id });
            }

            // Eagerly load the Site and SiteType so we can get the correct pricing
            var reservation = await _context.Reservations
                .Include(r => r.Site)
                    .ThenInclude(s => s.SiteType)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation != null)
            {
                // Historic Record Manipulation Block
                if (reservation.StartDate.Date <= DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Trips that have already started or passed cannot be modified.";
                    return RedirectToAction(nameof(MyReservations));
                }

                // Overlapping Reservation Conflict Verification
                var siteIsReserved = await _context.Reservations
                    .AnyAsync(r => r.SiteId == reservation.SiteId &&
                                   r.Id != reservation.Id && 
                                   r.Status != ReservationStatus.Cancelled &&
                                   r.Status != ReservationStatus.Completed &&
                                   r.StartDate < updateParams.EndDate.Date &&
                                   r.EndDate > updateParams.StartDate.Date);

                if (siteIsReserved)
                {
                    TempData["ErrorMessage"] = "Those dates conflict with another booking for this site. Please choose different dates.";
                    return RedirectToAction(nameof(EditMyTrip), new { id = id });
                }

                // Update Dates
                reservation.StartDate = updateParams.StartDate;
                reservation.EndDate = updateParams.EndDate;

                // Total Site Charge Recalculation
                var numberOfNights = (reservation.EndDate.Date - reservation.StartDate.Date).Days;
                var nightlyRate = reservation.Site?.SiteType?.Price ?? 0;
                var newTotalAmount = nightlyRate * numberOfNights;

                // Fetch and update the associated bill
                var siteChargeBill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.ReservationId == reservation.Id && b.Type == BillType.SiteCharge);
                
                if (siteChargeBill != null)
                {
                    siteChargeBill.Amount = newTotalAmount;
                    siteChargeBill.Description = $"{numberOfNights} night(s) at Site {reservation.Site?.SiteNumber}";
                    _context.Update(siteChargeBill);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Trip dates successfully updated! Your bill has been adjusted.";
            }

            return RedirectToAction(nameof(MyReservations));
        }

        // Customer Initiated Cancellation Submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> CancelMyTrip(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation != null)
            {
                // Started Trip Deletion Prevention
                if (reservation.StartDate.Date <= DateTime.Today)
                {
                    TempData["ErrorMessage"] = "Trips that have already started or passed cannot be cancelled.";
                    return RedirectToAction(nameof(MyReservations));
                }

                reservation.Status = ReservationStatus.Cancelled;
                reservation.CancelledAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = $"Reservation {reservation.ReservationNumber} has been cancelled.";
            }

            return RedirectToAction(nameof(MyReservations));
        }

        // Administrator and Employee Reservation Details View
        [HttpGet]
        [Authorize(Roles = "Customer, Employee, Manager, Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the reservation and eagerly load all necessary related data
            var reservation = await _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Site)
                .Include(r => r.Bills)           
                    .ThenInclude(b => b.Payments) 
                .FirstOrDefaultAsync(m => m.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // Active Non-Overlapping Site Search Query
        [HttpGet]
        [Authorize(Roles = "Staff, Employee, Manager, Admin")]
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

        // Form Select Item Preloading Assembly
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

        // Unique Identifier Prefix Assembly
        private static string GenerateReservationNumber()
        {
            var randomPart = Guid.NewGuid()
                .ToString("N")[..6]
                .ToUpperInvariant();

            return $"RES-{DateTime.UtcNow:yyyyMMdd}-{randomPart}";
        }
    }
}