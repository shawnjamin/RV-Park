using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;

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
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

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

    await DatabaseSeeder.SeedAsync(dbContext, logger);
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
