using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;
using RVPark.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Stripe;
using System.Globalization;

var culture = new CultureInfo("en-US");

CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var seedRequested = args.Contains("--seed", StringComparer.OrdinalIgnoreCase);
var builderArgs = args
    .Where(arg => !arg.Equals("--seed", StringComparison.OrdinalIgnoreCase))
    .ToArray();

var builder = WebApplication.CreateBuilder(builderArgs);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
EnsureSqliteDirectoryExists(connectionString);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registers PasswordHasher which can be accessed through Dependency Injection
builder.Services.AddScoped<IPasswordHasher<User>, UserPasswordHasher>();
builder.Services.AddTransient<UserPasswordHasher>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        // Redirect to a 403 page for access denied (since the page technically doesn't exist for the user)
        // This 403 forbidden status can be caught by razor pages or MVC to display an error message if we want to
        options.AccessDeniedPath = null;
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// Combines the mail settings in appsettings.json with the MailSettings class for programmatical/runtime availability
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// Register MailService
builder.Services.AddTransient<MailService>();

// Set Stripe API key
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (seedRequested || app.Configuration.GetValue<bool>("Database:SeedOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseSeeder");
    var passwordHasher = scope.ServiceProvider
        .GetRequiredService<IPasswordHasher<User>>();

    await DatabaseSeeder.SeedAsync(dbContext, logger, passwordHasher);
}

if (seedRequested)
{
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


await app.RunAsync();

static void EnsureSqliteDirectoryExists(string connectionString)
{
    var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
    var dataSource = connectionStringBuilder.DataSource;

    if (string.IsNullOrWhiteSpace(dataSource) ||
        dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    var directory = Path.GetDirectoryName(dataSource);

    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }
}
