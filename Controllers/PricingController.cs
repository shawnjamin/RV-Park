using Microsoft.AspNetCore.Mvc;

namespace RVPark.Controllers
{
    public class PricingController : Controller
    {
        public IActionResult Index() 
        {
            return View(); 
        }

        public IActionResult Create() 
        {
            return View(); 
        }
    }
}