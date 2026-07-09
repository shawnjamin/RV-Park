using Microsoft.EntityFrameworkCore;
using RVPark.Models;

namespace RVPark.Data;

public static class DatabaseSeeder
{
    private const string PlaceholderPasswordHash = "TEST-ONLY-NOT-A-REAL-PASSWORD-HASH";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        await EnsureNoPendingMigrationsAsync(context, cancellationToken);

        logger.LogInformation("Seeding test data.");

        var siteTypes = await SeedSiteTypesAsync(context, cancellationToken);
        var sites = await SeedSitesAsync(context, siteTypes, cancellationToken);
        var customers = await SeedCustomersAsync(context, cancellationToken);
        await SeedEmployeesAsync(context, cancellationToken);
        var reservations = await SeedReservationsAsync(context, customers, sites, cancellationToken);
        var bills = await SeedBillsAsync(context, reservations, cancellationToken);
        await SeedPaymentsAsync(context, bills, cancellationToken);

        logger.LogInformation("Finished seeding test data.");
    }

    private static async Task EnsureNoPendingMigrationsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);

        if (pendingMigrations.Any())
        {
            throw new InvalidOperationException(
                "The database has pending migrations. Run 'dotnet ef database update' or enable Database:MigrateOnStartup before seeding.");
        }
    }

    private static async Task<Dictionary<string, SiteType>> SeedSiteTypesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var seedSiteTypes = new[]
        {
            new SiteType
            {
                Name = "Standard Back-In",
                Description = "Back-in RV site with standard amenities.",
                Price = 55.00m,
                StartDate = new DateTime(2026, 1, 1), 
                EndDate = null,
                IsActive = true
            },
            new SiteType
            {
                Name = "Premium Pull-Through",
                Description = "Larger pull-through site with premium access.",
                Price = 85.00m,
                StartDate = new DateTime(2026, 1, 1), 
                EndDate = new DateTime(2026, 6, 1),
                IsActive = false
            },
            new SiteType
            {
                Name = "Tent and Van",
                Description = "Smaller site for tent campers and compact vans.",
                Price = 35.00m,
                StartDate = new DateTime(2026, 7, 1), 
                EndDate = new DateTime(2026, 7, 10),
                IsActive = true
            }
        };

        var existingSites = await context.SiteTypes.ToListAsync(cancellationToken);
        foreach (var site in existingSites)
        {
            // If the record exists but StartDate is at default (0001-01-01), set it
            if (site.StartDate == DateTime.MinValue)
            {
                site.StartDate = new DateTime(2026, 1, 1);
                context.SiteTypes.Update(site);
            }
        }
        await context.SaveChangesAsync(cancellationToken);

        foreach (var seedSiteType in seedSiteTypes)
        {
            if (!await context.SiteTypes.AnyAsync(siteType => siteType.Name == seedSiteType.Name, cancellationToken))
            {
                context.SiteTypes.Add(seedSiteType);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var seedSiteTypeNames = seedSiteTypes.Select(seed => seed.Name).ToArray();

        var seededSiteTypes = await context.SiteTypes
            .Where(siteType => seedSiteTypeNames.Contains(siteType.Name))
            .OrderBy(siteType => siteType.Id)
            .ToListAsync(cancellationToken);

        return seededSiteTypes
            .GroupBy(siteType => siteType.Name)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static async Task<Dictionary<string, Site>> SeedSitesAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, SiteType> siteTypes,
        CancellationToken cancellationToken)
    {
        var seedSites = new[]
        {
            new Site
            {
                SiteTypeId = siteTypes["Premium Pull-Through"].Id,
                SiteNumber = "A01",
                HookupType = HookupType.FullHookup,
                SizeSqft = 2400,
                Notes = "Seed premium full-hookup pull-through site.",
                IsActive = true
            },
            new Site
            {
                SiteTypeId = siteTypes["Standard Back-In"].Id,
                SiteNumber = "A02",
                HookupType = HookupType.FullHookup,
                SizeSqft = 1800,
                Notes = "Seed standard back-in full-hookup site.",
                IsActive = true
            },
            new Site
            {
                SiteTypeId = siteTypes["Standard Back-In"].Id,
                SiteNumber = "B01",
                HookupType = HookupType.PartialHookup,
                SizeSqft = 1600,
                Notes = "Seed partial-hookup site near bathhouse.",
                IsActive = true
            },
            new Site
            {
                SiteTypeId = siteTypes["Tent and Van"].Id,
                SiteNumber = "C01",
                HookupType = HookupType.NoHookup,
                SizeSqft = 900,
                Notes = "Seed no-hookup tent and van site.",
                IsActive = true
            }
        };

        foreach (var seedSite in seedSites)
        {
            if (!await context.Sites.AnyAsync(site => site.SiteNumber == seedSite.SiteNumber, cancellationToken))
            {
                context.Sites.Add(seedSite);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var seedSiteNumbers = seedSites.Select(seed => seed.SiteNumber).ToArray();

        return await context.Sites
            .Where(site => seedSiteNumbers.Contains(site.SiteNumber))
            .ToDictionaryAsync(site => site.SiteNumber, cancellationToken);
    }

    private static async Task<Dictionary<string, Customer>> SeedCustomersAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var seedCustomers = new[]
        {
            new CustomerSeed("customer.alex@example.test", "Alex", "Rivera", "555-0101"),
            new CustomerSeed("customer.jordan@example.test", "Jordan", "Lee", "555-0102"),
            new CustomerSeed("customer.taylor@example.test", "Taylor", "Morgan", "555-0103")
        };

        foreach (var seedCustomer in seedCustomers)
        {
            var user = await EnsureUserAsync(context, seedCustomer.Email, cancellationToken);

            if (!await context.Customers.AnyAsync(customer => customer.Id == user.Id, cancellationToken))
            {
                context.Customers.Add(new Customer
                {
                    Id = user.Id,
                    FirstName = seedCustomer.FirstName,
                    LastName = seedCustomer.LastName,
                    Phone = seedCustomer.Phone,
                    Email = seedCustomer.Email
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var seedCustomerEmails = seedCustomers.Select(seed => seed.Email).ToArray();

        return await context.Customers
            .Include(customer => customer.User)
            .Where(customer => seedCustomerEmails.Contains(customer.User.Email))
            .ToDictionaryAsync(customer => customer.User.Email, cancellationToken);
    }

    private static async Task SeedEmployeesAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var seedEmployees = new[]
        {
            new EmployeeSeed("admin.avery@example.test", "Avery", "Brooks", EmployeeAccessLevel.Admin),
            new EmployeeSeed("manager.casey@example.test", "Casey", "Nguyen", EmployeeAccessLevel.Manager),
            new EmployeeSeed("staff.riley@example.test", "Riley", "Patel", EmployeeAccessLevel.Staff)
        };

        foreach (var seedEmployee in seedEmployees)
        {
            var user = await EnsureUserAsync(context, seedEmployee.Email, cancellationToken);

            if (!await context.Employees.AnyAsync(employee => employee.Id == user.Id, cancellationToken))
            {
                context.Employees.Add(new Employee
                {
                    Id = user.Id,
                    FirstName = seedEmployee.FirstName,
                    LastName = seedEmployee.LastName,
                    AccessLevel = seedEmployee.AccessLevel,
                    IsLocked = false
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Reservation>> SeedReservationsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Customer> customers,
        IReadOnlyDictionary<string, Site> sites,
        CancellationToken cancellationToken)
    {
        var seedReservations = new[]
        {
            new Reservation
            {
                ReservationNumber = "SEED-RES-1001",
                CustomerId = customers["customer.alex@example.test"].Id,
                SiteId = sites["A01"].Id,
                SpecialRequestsOrNotes = "Seed pending payment reservation.",
                AdultCount = 2,
                ChildCount = 0,
                PetCount = 1,
                PetNotes = "One small dog.",
                StartDate = new DateTime(2026, 8, 1, 15, 0, 0),
                EndDate = new DateTime(2026, 8, 4, 11, 0, 0),
                Status = ReservationStatus.PendingPayment,
                CreatedAt = new DateTime(2026, 7, 1, 14, 0, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1002",
                CustomerId = customers["customer.jordan@example.test"].Id,
                SiteId = sites["B01"].Id,
                SpecialRequestsOrNotes = "Seed confirmed reservation.",
                AdultCount = 2,
                ChildCount = 2,
                PetCount = 0,
                StartDate = new DateTime(2026, 8, 10, 15, 0, 0),
                EndDate = new DateTime(2026, 8, 13, 11, 0, 0),
                Status = ReservationStatus.Confirmed,
                CreatedAt = new DateTime(2026, 7, 2, 9, 30, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1003",
                CustomerId = customers["customer.taylor@example.test"].Id,
                SiteId = sites["C01"].Id,
                SpecialRequestsOrNotes = "Seed checked-in reservation.",
                AdultCount = 1,
                ChildCount = 0,
                PetCount = 0,
                StartDate = new DateTime(2026, 7, 7, 15, 0, 0),
                EndDate = new DateTime(2026, 7, 10, 11, 0, 0),
                Status = ReservationStatus.CheckedIn,
                CreatedAt = new DateTime(2026, 6, 25, 10, 15, 0),
                CheckedInAt = new DateTime(2026, 7, 7, 15, 20, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1004",
                CustomerId = customers["customer.alex@example.test"].Id,
                SiteId = sites["A02"].Id,
                SpecialRequestsOrNotes = "Seed completed reservation.",
                AdultCount = 2,
                ChildCount = 1,
                PetCount = 0,
                StartDate = new DateTime(2026, 6, 15, 15, 0, 0),
                EndDate = new DateTime(2026, 6, 18, 11, 0, 0),
                Status = ReservationStatus.Completed,
                CreatedAt = new DateTime(2026, 5, 30, 12, 0, 0),
                CheckedInAt = new DateTime(2026, 6, 15, 15, 5, 0),
                CheckedOutAt = new DateTime(2026, 6, 18, 10, 45, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1005",
                CustomerId = customers["customer.jordan@example.test"].Id,
                SiteId = sites["A02"].Id,
                SpecialRequestsOrNotes = "Seed cancelled reservation.",
                AdultCount = 1,
                ChildCount = 0,
                PetCount = 0,
                StartDate = new DateTime(2026, 9, 5, 15, 0, 0),
                EndDate = new DateTime(2026, 9, 7, 11, 0, 0),
                Status = ReservationStatus.Cancelled,
                CreatedAt = new DateTime(2026, 7, 4, 16, 0, 0),
                CancelledAt = new DateTime(2026, 7, 6, 8, 45, 0)
            }
        };

        foreach (var seedReservation in seedReservations)
        {
            if (!await context.Reservations.AnyAsync(
                reservation => reservation.ReservationNumber == seedReservation.ReservationNumber,
                cancellationToken))
            {
                context.Reservations.Add(seedReservation);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var seedReservationNumbers = seedReservations.Select(seed => seed.ReservationNumber).ToArray();

        return await context.Reservations
            .Where(reservation => seedReservationNumbers.Contains(reservation.ReservationNumber))
            .ToDictionaryAsync(reservation => reservation.ReservationNumber, cancellationToken);
    }

    private static async Task<Dictionary<string, Bill>> SeedBillsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Reservation> reservations,
        CancellationToken cancellationToken)
    {
        var seedBills = new[]
        {
            new BillSeed("seed:bill:site-charge:SEED-RES-1001", "SEED-RES-1001", BillType.SiteCharge, 255.00m),
            new BillSeed("seed:bill:late-fee:SEED-RES-1003", "SEED-RES-1003", BillType.LateFee, 25.00m),
            new BillSeed("seed:bill:cancellation-fee:SEED-RES-1005", "SEED-RES-1005", BillType.CancellationFee, 35.00m),
            new BillSeed("seed:bill:early-check-in:SEED-RES-1002", "SEED-RES-1002", BillType.EarlyCheckInFee, 15.00m),
            new BillSeed("seed:bill:late-check-out:SEED-RES-1004", "SEED-RES-1004", BillType.LateCheckOutFee, 20.00m)
        };

        foreach (var seedBill in seedBills)
        {
            if (!await context.Bills.AnyAsync(bill => bill.Description == seedBill.Key, cancellationToken))
            {
                context.Bills.Add(new Bill
                {
                    ReservationId = reservations[seedBill.ReservationNumber].Id,
                    Type = seedBill.Type,
                    Description = seedBill.Key,
                    Amount = seedBill.Amount,
                    CreatedAt = new DateTime(2026, 7, 2, 12, 0, 0)
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var seedBillKeys = seedBills.Select(seed => seed.Key).ToArray();

        var seededBills = await context.Bills
            .Where(bill => seedBillKeys.Contains(bill.Description))
            .OrderBy(bill => bill.Id)
            .ToListAsync(cancellationToken);

        return seededBills
            .GroupBy(bill => bill.Description!)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static async Task SeedPaymentsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Bill> bills,
        CancellationToken cancellationToken)
    {
        var seedPayments = new[]
        {
            new PaymentSeed("seed:payment:card:SEED-RES-1001", "seed:bill:site-charge:SEED-RES-1001", PaymentMethod.Card, 255.00m, null),
            new PaymentSeed("seed:payment:cash:SEED-RES-1003", "seed:bill:late-fee:SEED-RES-1003", PaymentMethod.Cash, 25.00m, null),
            new PaymentSeed("seed:payment:check:SEED-RES-1002", "seed:bill:early-check-in:SEED-RES-1002", PaymentMethod.Check, 15.00m, null),
            new PaymentSeed("seed:payment:stripe:SEED-RES-1004", "seed:bill:late-check-out:SEED-RES-1004", PaymentMethod.Stripe, 20.00m, "seed_stripe_seed_res_1004")
        };

        foreach (var seedPayment in seedPayments)
        {
            var paymentExists = seedPayment.StripeTransactionId is not null
                ? await context.Payments.AnyAsync(
                    payment => payment.StripeTransactionId == seedPayment.StripeTransactionId,
                    cancellationToken)
                : await context.Payments.AnyAsync(payment => payment.Notes == seedPayment.Key, cancellationToken);

            if (!paymentExists)
            {
                context.Payments.Add(new Payment
                {
                    BillId = bills[seedPayment.BillKey].Id,
                    PaymentMethod = seedPayment.PaymentMethod,
                    StripeTransactionId = seedPayment.StripeTransactionId,
                    Notes = seedPayment.Key,
                    Amount = seedPayment.Amount,
                    PaidAt = new DateTime(2026, 7, 3, 13, 0, 0)
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<User> EnsureUserAsync(
        ApplicationDbContext context,
        string email,
        CancellationToken cancellationToken)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

        if (existingUser is not null)
        {
            return existingUser;
        }

        var user = new User
        {
            Email = email,
            PasswordHash = PlaceholderPasswordHash,
            CreatedAt = new DateTime(2026, 7, 1, 12, 0, 0)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user;
    }

    private sealed record CustomerSeed(string Email, string FirstName, string LastName, string Phone);

    private sealed record EmployeeSeed(string Email, string FirstName, string LastName, EmployeeAccessLevel AccessLevel);

    private sealed record BillSeed(
        string Key,
        string ReservationNumber,
        BillType Type,
        decimal Amount);

    private sealed record PaymentSeed(
        string Key,
        string BillKey,
        PaymentMethod PaymentMethod,
        decimal Amount,
        string? StripeTransactionId);
}
