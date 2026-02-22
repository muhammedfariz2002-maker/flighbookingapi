using Stripe.Checkout;

public class StripePaymentService : IPaymentService
{
    public async Task<string> CreateCheckoutSessionAsync(
        int bookingId,
        decimal amount,
        string currency)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            Mode = "payment",

            SuccessUrl = "https://localhost:7067/payment-success",
            CancelUrl = "https://localhost:7067/payment-cancel",

            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency.ToLower(),
                        UnitAmount = (long)(amount * 100), // Stripe uses smallest currency unit
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Flight Booking #{bookingId}"
                        }
                    },
                    Quantity = 1
                }
            },

            Metadata = new Dictionary<string, string>
            {
                { "BookingId", bookingId.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return session.Url;
    }
}