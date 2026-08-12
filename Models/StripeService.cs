using Stripe;
using Stripe.Checkout;

namespace RVPark.Services;

public class StripeService
{
    private readonly IConfiguration _configuration;

    public StripeService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        decimal amount,
        string reservationNumber,
        int reservationId,
        string customerEmail,
        string successUrl,
        string cancelUrl)
    {
        var secretKey = _configuration["Stripe:SecretKey"];

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "Stripe secret key is not configured.");
        }

        StripeConfiguration.ApiKey = secretKey;

        var options = new SessionCreateOptions
        {
            Mode = "payment",

            CustomerEmail = customerEmail,

            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,

            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)(amount * 100),

                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"RV Park Reservation {reservationNumber}",
                            Description = "RV Park site reservation"
                        }
                    },

                    Quantity = 1
                }
            },

            Metadata = new Dictionary<string, string>
            {
                { "ReservationId", reservationId.ToString() },
                { "ReservationNumber", reservationNumber }
            }
        };

        var service = new SessionService();

        var session = await service.CreateAsync(options);

        if (string.IsNullOrWhiteSpace(session.Url))
        {
            throw new InvalidOperationException(
                "Stripe did not return a Checkout URL.");
        }

        return session.Url;
    }
}