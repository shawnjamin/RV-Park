using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using RVPark.Data;
using RVPark.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;

namespace RVPark.Controllers
{
    public class RvSitesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public RvSitesController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: RvSites
        public async Task<IActionResult> Index()
        {
            var sites = _context.Sites.Include(s => s.SiteType);
            return View(await sites.ToListAsync());
        }

        // GET: RvSites/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var site = await _context.Sites
                .Include(s => s.SiteType)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (site == null) return NotFound();

            return View(site);
        }

        // GET: RvSites/Create
        public IActionResult Create()
        {
            ViewBag.SiteTypeId = new SelectList(_context.SiteTypes.Where(st => st.IsActive), "Id", "Name");
            return View();
        }

        // POST: RvSites/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SiteNumber,SiteTypeId,HookupType,SizeSqft,Notes,IsActive")] Site site)
        {
            if (ModelState.IsValid)
            {
                _context.Add(site);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SiteTypeId = new SelectList(_context.SiteTypes.Where(st => st.IsActive), "Id", "Name", site.SiteTypeId);
            return View(site);
        }

        // GET: RvSites/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var site = await _context.Sites.FindAsync(id);
            if (site == null) return NotFound();

            ViewBag.SiteTypeId = new SelectList(_context.SiteTypes.Where(st => st.IsActive), "Id", "Name", site.SiteTypeId);
            return View(site);
        }

        // POST: RvSites/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SiteNumber,SiteTypeId,HookupType,SizeSqft,Notes,IsActive")] Site site)
        {
            if (id != site.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(site);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SiteExists(site.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.SiteTypeId = new SelectList(_context.SiteTypes.Where(st => st.IsActive), "Id", "Name", site.SiteTypeId);
            return View(site);
        }

        // POST: RvSites/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var site = await _context.Sites.FindAsync(id);
            if (site != null)
            {
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SiteExists(int id)
        {
            return _context.Sites.Any(e => e.Id == id);
        }


        // GET: RvSites/ManagePhotos/5
        public async Task<IActionResult> ManagePhotos(int? id)
        {
            if (id == null) return NotFound();

            var site = await _context.Sites
                .Include(s => s.Photos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (site == null) return NotFound();

            return View(site);
        }

        // POST: RvSites/UploadPhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(int SiteId, IFormFile imageFile, string caption)
        {
            var site = await _context.Sites.FindAsync(SiteId);
            if (site == null) return NotFound();

            if (imageFile != null && imageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "sites");
                Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(fileStream);
                }

                var photo = new SitePhoto
                {
                    SiteId = SiteId,
                    Url = "/images/sites/" + uniqueFileName,
                    Caption = caption
                };

                _context.SitePhotos.Add(photo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManagePhotos), new { id = SiteId });
        }

        // POST: RvSites/DeletePhoto
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int photoId, int siteId)
        {
            var photo = await _context.SitePhotos.FindAsync(photoId);
            if (photo != null)
            {
                var filePath = Path.Combine(_hostEnvironment.WebRootPath, photo.Url.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.SitePhotos.Remove(photo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManagePhotos), new { id = siteId });
        }
    }
}