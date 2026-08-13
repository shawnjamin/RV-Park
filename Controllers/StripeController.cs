using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RVPark.Models;
using RVPark.Services;
using Stripe;
using Stripe.Checkout;

namespace RVPark.Controllers;

/// <summary>
/// Handles all stripe control. Utilizes StripeOptions and the stripe information in appsettings.json
/// </summary>
[Route("")]
[ApiController]
[Authorize]
public class StripeController : Controller
{
    [HttpPost]
    [Authorize(Roles = "Customer, Employee, Manager, Admin")]
    public async Task<IActionResult> BuyAsync(string siteNumber)
    {
        var origin = $"{Request.Scheme}://{Request.Host}";
        var checkoutUrlWithQuery = Request.GetTypedHeaders().Referer.ToString();
        // Create session options
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmountDecimal = 13500,
                        Currency = "USD",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = siteNumber
                        },
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = checkoutUrlWithQuery,
            CancelUrl = origin + "/RvSites/Browse"
        };
        // Create service and session
        var service = new SessionService();
        Session session = await service.CreateAsync(options);
        // Redirect when payment succeeds or is canceled
        Response.Headers.Append("Location", session.Url);
        return new StatusCodeResult(303);
    }
}