using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RVPark.Models;

namespace RVPark.Data;

public static class DatabaseSeeder
{
    private const string PlaceholderPasswordHash = "TEST-ONLY-NOT-A-REAL-PASSWORD-HASH";
    private const string SeedPassword = "RVParkSeed123!";

    public static async Task SeedAsync(
        ApplicationDbContext context,
        ILogger logger,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await EnsureNoPendingMigrationsAsync(context, cancellationToken);

        logger.LogInformation("Seeding test data.");

        var siteTypes = await SeedSiteTypesAsync(context, cancellationToken);
        var sites = await SeedSitesAsync(context, siteTypes, cancellationToken);
        await SeedSitePhotosAsync(context, sites, cancellationToken);
        var customers = await SeedCustomersAsync(context, passwordHasher, cancellationToken);
        await SeedEmployeesAsync(context, passwordHasher, cancellationToken);
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
                MaxRvLengthFt = 45,
                HookupType = HookupType.FullHookup,
                SizeSqft = 2400,
                Notes = "Seed premium full-hookup pull-through site.",
                IsActive = true
            },
            new Site
            {
                SiteTypeId = siteTypes["Standard Back-In"].Id,
                SiteNumber = "A02",
                MaxRvLengthFt = 35,
                HookupType = HookupType.FullHookup,
                SizeSqft = 1800,
                Notes = "Seed standard back-in full-hookup site.",
                IsActive = true
            },
            new Site
            {
                SiteTypeId = siteTypes["Standard Back-In"].Id,
                SiteNumber = "B01",
                MaxRvLengthFt = 30,
                HookupType = HookupType.PartialHookup,
                SizeSqft = 1600,
                Notes = "Seed partial-hookup site near bathhouse.",
                IsActive = true
            },
            new Site
            {
                SiteTypeId = siteTypes["Tent and Van"].Id,
                SiteNumber = "C01",
                MaxRvLengthFt = null,
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

    private static async Task SeedSitePhotosAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Site> sites,
        CancellationToken cancellationToken)
    {
        var seedSitePhotos = new[]
        {
            new SitePhoto
            {
                SiteId = sites["A01"].Id,
                Url = "/images/sites/60e78691-2bdc-4cf4-ac83-895af3b25999_camping-in-rv.jpg",
                Caption = "RV campsite surrounded by trees."
            },
            new SitePhoto
            {
                SiteId = sites["C01"].Id,
                Url = "/images/sites/ceb52507-b7e1-4757-b01e-7c6f24b25b8a_rv-camping-under-tree.jpg",
                Caption = "Shaded campsite beneath a large tree."
            }
        };

        foreach (var seedSitePhoto in seedSitePhotos)
        {
            if (!await context.SitePhotos.AnyAsync(
                photo => photo.SiteId == seedSitePhoto.SiteId && photo.Url == seedSitePhoto.Url,
                cancellationToken))
            {
                context.SitePhotos.Add(seedSitePhoto);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, User>> SeedCustomersAsync(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var seedCustomers = new[]
        {
            new UserSeed("customer.alex@example.test", "1234567890", "Alex", "Rivera", AccessLevel.Customer, false),
            new UserSeed("customer.jordan@example.test", "4743729384", "Jordan", "Lee", AccessLevel.Customer, false),
            new UserSeed("customer.taylor@example.test", "1203948374", "Taylor", "Morgan", AccessLevel.Customer, false)
        };

        foreach (var seedCustomer in seedCustomers)
        {
            await EnsureUserAsync(context, seedCustomer, passwordHasher, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);

        var seedCustomerEmails = seedCustomers.Select(seed => seed.Email).ToArray();

        return await context.Users
            .Where(customer => seedCustomerEmails.Contains(customer.Email))
            .ToDictionaryAsync(customer => customer.Email, cancellationToken);
    }

    private static async Task SeedEmployeesAsync(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var seedEmployees = new[]
        {
            new UserSeed("admin.avery@example.test", "1111111111", "Avery", "Brooks", AccessLevel.Admin, false),
            new UserSeed("manager.casey@example.test", "2222222222", "Casey", "Nguyen", AccessLevel.Manager, false),
            new UserSeed("staff.riley@example.test", "3333333333", "Riley", "Patel", AccessLevel.Employee, false)
        };

        foreach (var seedEmployee in seedEmployees)
        {
            await EnsureUserAsync(context, seedEmployee, passwordHasher, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Reservation>> SeedReservationsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, User> customers,
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
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1006",
                CustomerId = customers["customer.taylor@example.test"].Id,
                SiteId = sites["A01"].Id,
                SpecialRequestsOrNotes = "Seed confirmed family reservation.",
                AdultCount = 2,
                ChildCount = 1,
                PetCount = 0,
                StartDate = new DateTime(2026, 8, 12, 15, 0, 0),
                EndDate = new DateTime(2026, 8, 16, 11, 0, 0),
                Status = ReservationStatus.Confirmed,
                CreatedAt = new DateTime(2026, 7, 10, 10, 30, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1007",
                CustomerId = customers["customer.alex@example.test"].Id,
                SiteId = sites["B01"].Id,
                SpecialRequestsOrNotes = "Seed pending reservation with a pet.",
                AdultCount = 1,
                ChildCount = 0,
                PetCount = 1,
                PetNotes = "One leashed dog.",
                StartDate = new DateTime(2026, 8, 20, 15, 0, 0),
                EndDate = new DateTime(2026, 8, 23, 11, 0, 0),
                Status = ReservationStatus.PendingPayment,
                CreatedAt = new DateTime(2026, 7, 18, 13, 15, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1008",
                CustomerId = customers["customer.jordan@example.test"].Id,
                SiteId = sites["C01"].Id,
                SpecialRequestsOrNotes = "Seed completed tent reservation.",
                AdultCount = 2,
                ChildCount = 0,
                PetCount = 0,
                StartDate = new DateTime(2026, 5, 10, 15, 0, 0),
                EndDate = new DateTime(2026, 5, 12, 11, 0, 0),
                Status = ReservationStatus.Completed,
                CreatedAt = new DateTime(2026, 4, 20, 9, 0, 0),
                CheckedInAt = new DateTime(2026, 5, 10, 15, 10, 0),
                CheckedOutAt = new DateTime(2026, 5, 12, 10, 30, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1009",
                CustomerId = customers["customer.taylor@example.test"].Id,
                SiteId = sites["A02"].Id,
                SpecialRequestsOrNotes = "Seed cancelled family reservation.",
                AdultCount = 2,
                ChildCount = 2,
                PetCount = 0,
                StartDate = new DateTime(2026, 8, 20, 15, 0, 0),
                EndDate = new DateTime(2026, 8, 22, 11, 0, 0),
                Status = ReservationStatus.Cancelled,
                CreatedAt = new DateTime(2026, 7, 12, 11, 45, 0),
                CancelledAt = new DateTime(2026, 7, 20, 14, 20, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1010",
                CustomerId = customers["customer.alex@example.test"].Id,
                SiteId = sites["C01"].Id,
                SpecialRequestsOrNotes = "Seed confirmed fall reservation.",
                AdultCount = 2,
                ChildCount = 0,
                PetCount = 0,
                StartDate = new DateTime(2026, 9, 12, 15, 0, 0),
                EndDate = new DateTime(2026, 9, 15, 11, 0, 0),
                Status = ReservationStatus.Confirmed,
                CreatedAt = new DateTime(2026, 7, 22, 8, 30, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1011",
                CustomerId = customers["customer.jordan@example.test"].Id,
                SiteId = sites["A01"].Id,
                SpecialRequestsOrNotes = "Seed completed early-summer reservation.",
                AdultCount = 2,
                ChildCount = 1,
                PetCount = 1,
                PetNotes = "One cat in the RV.",
                StartDate = new DateTime(2026, 6, 1, 15, 0, 0),
                EndDate = new DateTime(2026, 6, 4, 11, 0, 0),
                Status = ReservationStatus.Completed,
                CreatedAt = new DateTime(2026, 5, 12, 15, 40, 0),
                CheckedInAt = new DateTime(2026, 6, 1, 15, 0, 0),
                CheckedOutAt = new DateTime(2026, 6, 4, 10, 50, 0)
            },
            new Reservation
            {
                ReservationNumber = "SEED-RES-1012",
                CustomerId = customers["customer.taylor@example.test"].Id,
                SiteId = sites["A02"].Id,
                SpecialRequestsOrNotes = "Seed pending autumn reservation.",
                AdultCount = 1,
                ChildCount = 0,
                PetCount = 0,
                StartDate = new DateTime(2026, 10, 1, 15, 0, 0),
                EndDate = new DateTime(2026, 10, 5, 11, 0, 0),
                Status = ReservationStatus.PendingPayment,
                CreatedAt = new DateTime(2026, 7, 24, 17, 10, 0)
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
        UserSeed seedUser,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var existingUser = await context.Users
            .FirstOrDefaultAsync(user => user.Email == seedUser.Email, cancellationToken);

        if (existingUser is not null)
        {
            // Re-hash password to ensure compatibility with UserPasswordHasher
            existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, SeedPassword);
            existingUser.IsLocked = seedUser.IsLocked;
            existingUser.AccessLevel = seedUser.AccessLevel;
            context.Users.Update(existingUser);
            await context.SaveChangesAsync(cancellationToken);

            return existingUser;
        }

        var user = new User
        {
            Email = seedUser.Email,
            Phone = seedUser.Phone,
            FirstName = seedUser.FirstName,
            LastName = seedUser.LastName,
            CreatedAt = new DateTime(2026, 7, 1, 12, 0, 0),
            AccessLevel = seedUser.AccessLevel,
            IsLocked = seedUser.IsLocked
        };

        user.PasswordHash = passwordHasher.HashPassword(user, SeedPassword);

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user;
    }

    private sealed record UserSeed(string Email, string Phone, string FirstName, string LastName, AccessLevel AccessLevel, bool IsLocked);

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