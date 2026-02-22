using FlightBookingApi.DTOs;
using FlightBookingApi.Infrastructure.ExternalServices;
using FlightBookingApi.Repositories;
using FlightBookingApi.Repositories.Interfaces;
using FlightBookingApi.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe;
using Stripe.Checkout;
using System.Text;
using System.Text.Json;

namespace FlightBookingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly AmadeusClient _amadeusClient;

        public PaymentsController(
            IBookingRepository bookingRepository,
            IPaymentService paymentService,
            ILogger<PaymentsController> logger,
            IConfiguration configuration,
            AmadeusClient amadeusClient)
        {
            _bookingRepository = bookingRepository;
            _paymentService = paymentService;
            _logger = logger;
            _configuration = configuration;
            _amadeusClient = amadeusClient;

        }

        [HttpPost("create-session/{bookingId}")]
        public async Task<IActionResult> CreateSession(int bookingId)
        {
            _logger.LogInformation("Creating Stripe session for BookingId {BookingId}", bookingId);

            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
                return NotFound("Booking not found.");

            if (booking.Status != "AwaitingPayment")
                return BadRequest("Booking not ready for payment.");

            var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(
                booking.Id,
                booking.Price,
                booking.Currency
            );

            _logger.LogInformation("Stripe session created for BookingId {BookingId}", bookingId);

            return Ok(new { CheckoutUrl = checkoutUrl });
        }



[HttpPost("webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        var webhookSecret = _configuration["StripeSettings:WebhookSecret"];

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                webhookSecret,
                throwOnApiVersionMismatch: false
            );

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;

                    var bookingId = int.Parse(session.Metadata["BookingId"]);

                    _logger.LogInformation("Payment successful for BookingId {BookingId}", bookingId);

                    var booking = await _bookingRepository.GetByIdAsync(bookingId);

                    if (booking != null)
                    {

                        if (booking.Status == "Confirmed")
                        {
                            _logger.LogInformation(
                                "Booking {BookingId} already confirmed. Skipping duplicate webhook.",
                                bookingId);

                            return Ok();
                        }

                        booking.Status = "PaymentConfirmed";
                        await _bookingRepository.UpdateAsync(booking);

                        _logger.LogInformation("Booking {BookingId} marked as PaymentConfirmed", bookingId);

                        // 🔥 Now create Amadeus order

                        var flightOffer = JsonSerializer.Deserialize<JsonElement>(booking.FlightOfferJson!);
                        var traveler = JsonSerializer.Deserialize<TravelerDto>(booking.TravelerJson!);
                        var contact = JsonSerializer.Deserialize<ContactDto>(booking.ContactJson!);

                        var orderResponse = await _amadeusClient.CreateOrderAsync(
                            flightOffer,
                            traveler!,
                            contact!
                        );

                        using var doc = JsonDocument.Parse(orderResponse);

                        var orderId = doc.RootElement
                            .GetProperty("data")
                            .GetProperty("id")
                            .GetString();

                        booking.AmadeusOrderId = orderId;
                        booking.Status = "Confirmed";

                        await _bookingRepository.UpdateAsync(booking);

                        _logger.LogInformation(
                            "Booking {BookingId} confirmed with Amadeus OrderId {OrderId}",
                            bookingId,
                            orderId);
                    }
                }

                return Ok();
        }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe webhook failed");
                return Ok(); // temporary
            }
        }
}
}