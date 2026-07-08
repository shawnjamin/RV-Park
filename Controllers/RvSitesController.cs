using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers;

public class RvSitesController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        var sites = await context.Sites
            .AsNoTracking()
            .Include(site => site.SiteType)
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

        var site = await context.Sites
            .AsNoTracking()
            .Include(site => site.SiteType)
            .FirstOrDefaultAsync(site => site.Id == id);

        if (site is null)
        {
            return NotFound();
        }

        return View(site);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSiteTypesAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("SiteTypeId,SiteNumber,HookupType,SizeSqft,PhotoUrl,Notes,IsActive")] Site site)
    {
        await ValidateSiteTypeAsync(site.SiteTypeId);

        if (!ModelState.IsValid)
        {
            await PopulateSiteTypesAsync(site.SiteTypeId);
            return View(site);
        }

        if (await SiteNumberExists(site.SiteNumber))
        {
            ModelState.AddModelError(nameof(Site.SiteNumber), "A site with this site number already exists.");
            await PopulateSiteTypesAsync(site.SiteTypeId);
            return View(site);
        }

        context.Add(site);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var site = await context.Sites.FindAsync(id);

        if (site is null)
        {
            return NotFound();
        }

        await PopulateSiteTypesAsync(site.SiteTypeId);
        return View(site);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,SiteTypeId,SiteNumber,HookupType,SizeSqft,PhotoUrl,Notes,IsActive")] Site site)
    {
        if (id != site.Id)
        {
            return NotFound();
        }

        await ValidateSiteTypeAsync(site.SiteTypeId);

        if (!ModelState.IsValid)
        {
            await PopulateSiteTypesAsync(site.SiteTypeId);
            return View(site);
        }

        if (await SiteNumberExists(site.SiteNumber, site.Id))
        {
            ModelState.AddModelError(nameof(Site.SiteNumber), "A site with this site number already exists.");
            await PopulateSiteTypesAsync(site.SiteTypeId);
            return View(site);
        }

        try
        {
            context.Update(site);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await SiteExists(site.Id))
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

        var site = await context.Sites
            .AsNoTracking()
            .Include(site => site.SiteType)
            .FirstOrDefaultAsync(site => site.Id == id);

        if (site is null)
        {
            return NotFound();
        }

        return View(site);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var site = await context.Sites.FindAsync(id);

        if (site is not null)
        {
            context.Sites.Remove(site);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> SiteExists(int id)
    {
        return await context.Sites.AnyAsync(site => site.Id == id);
    }

    private async Task<bool> SiteNumberExists(string siteNumber, int? excludingId = null)
    {
        return await context.Sites.AnyAsync(site =>
            site.SiteNumber == siteNumber &&
            (excludingId == null || site.Id != excludingId));
    }

    private async Task PopulateSiteTypesAsync(int? selectedSiteTypeId = null)
    {
        var siteTypes = await context.SiteTypes
            .AsNoTracking()
            .OrderBy(siteType => siteType.Name)
            .ToListAsync();

        ViewData["SiteTypeId"] = new SelectList(siteTypes, "Id", "Name", selectedSiteTypeId);
    }

    private async Task ValidateSiteTypeAsync(int siteTypeId)
    {
        if (!await context.SiteTypes.AnyAsync())
        {
            ModelState.AddModelError(nameof(Site.SiteTypeId), "Create a site type before adding sites.");
            return;
        }

        if (!await context.SiteTypes.AnyAsync(siteType => siteType.Id == siteTypeId))
        {
            ModelState.AddModelError(nameof(Site.SiteTypeId), "Select a valid site type.");
        }
    }
}
