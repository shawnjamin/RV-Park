using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

public class RvSitesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var sites = await context.RvSites
            .AsNoTracking()
            .OrderBy(site => site.SiteNumber)
            .ToListAsync();

        return View(sites);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var rvSite = await context.RvSites
            .AsNoTracking()
            .FirstOrDefaultAsync(site => site.Id == id);

        if (rvSite is null)
        {
            return NotFound();
        }

        return View(rvSite);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SiteNumber,MaxRvLength,NightlyRate,HookupType,IsAvailable")] RvSite rvSite)
    {
        if (!ModelState.IsValid)
        {
            return View(rvSite);
        }

        if (await SiteNumberExists(rvSite.SiteNumber))
        {
            ModelState.AddModelError(nameof(RvSite.SiteNumber), "A site with this site number already exists.");
            return View(rvSite);
        }

        context.Add(rvSite);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var rvSite = await context.RvSites.FindAsync(id);

        if (rvSite is null)
        {
            return NotFound();
        }

        return View(rvSite);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,SiteNumber,MaxRvLength,NightlyRate,HookupType,IsAvailable")] RvSite rvSite)
    {
        if (id != rvSite.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(rvSite);
        }

        if (await SiteNumberExists(rvSite.SiteNumber, rvSite.Id))
        {
            ModelState.AddModelError(nameof(RvSite.SiteNumber), "A site with this site number already exists.");
            return View(rvSite);
        }

        try
        {
            context.Update(rvSite);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await RvSiteExists(rvSite.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var rvSite = await context.RvSites
            .AsNoTracking()
            .FirstOrDefaultAsync(site => site.Id == id);

        if (rvSite is null)
        {
            return NotFound();
        }

        return View(rvSite);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rvSite = await context.RvSites.FindAsync(id);

        if (rvSite is not null)
        {
            context.RvSites.Remove(rvSite);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> RvSiteExists(int id)
    {
        return await context.RvSites.AnyAsync(site => site.Id == id);
    }

    private async Task<bool> SiteNumberExists(string siteNumber, int? excludingId = null)
    {
        return await context.RvSites.AnyAsync(site =>
            site.SiteNumber == siteNumber &&
            (excludingId == null || site.Id != excludingId));
    }
}
