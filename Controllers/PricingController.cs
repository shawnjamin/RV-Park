using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers
{
    public class PricingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PricingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pricing
        public async Task<IActionResult> Index()
        {
            var siteTypes = await _context.SiteTypes.ToListAsync();
            return View(siteTypes);
        }

        // GET: Pricing/Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var siteType = await _context.SiteTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (siteType == null) return NotFound();

            return View(siteType);
        }

        // GET: Pricing/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Pricing/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Price,IsActive")] SiteType siteType)
        {
            if (ModelState.IsValid)
            {
                _context.Add(siteType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(siteType);
        }

        // GET: Pricing/Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var siteType = await _context.SiteTypes.FindAsync(id);
            if (siteType == null) return NotFound();
            
            return View(siteType);
        }

        // POST: Pricing/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Price,IsActive")] SiteType siteType)
        {
            if (id != siteType.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(siteType);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SiteTypeExists(siteType.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(siteType);
        }

        // POST: Pricing/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var siteType = await _context.SiteTypes.FindAsync(id);
            if (siteType != null)
            {
                _context.SiteTypes.Remove(siteType);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SiteTypeExists(int id)
        {
            return _context.SiteTypes.Any(e => e.Id == id);
        }
    }
}