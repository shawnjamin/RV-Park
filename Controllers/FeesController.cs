using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RVPark.Data;
using RVPark.Models;

namespace RVPark.Controllers
{
    public class FeesController(ApplicationDbContext context) : Controller
    {
        public async Task<IActionResult> Index() 
        {
            var fees = await context.Bills
                .AsNoTracking()
                .Include(bill => bill.Reservation)
                    .ThenInclude(reservation => reservation!.Customer)
                .Where(bill => bill.Type != BillType.SiteCharge)
                .OrderByDescending(bill => bill.CreatedAt)
                .ToListAsync();

            return View(fees);
        }

        public async Task<IActionResult> Create() 
        {
            return View(); 
        }
    }
}