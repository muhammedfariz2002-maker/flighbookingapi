using Azure.Core;
using FlightBookingApi.DTOs;
using FlightBookingApi.Infrastructure.ExternalServices;
using FlightBookingApi.Models;
using FlightBookingApi.Repositories.Interfaces;
using FlightBookingApi.Services.Interfaces;
using System.Text.Json;

namespace FlightBookingApi.Services
{
    public class BookingService : IBookingService
    {
        private readonly AmadeusClient _amadeusClient;
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<BookingService> _logger;

        public BookingService(
            AmadeusClient amadeusClient,
            IBookingRepository bookingRepository, ILogger<BookingService> logger)
        {
            _amadeusClient = amadeusClient;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task<object> CreateBookingAsync(BookingRequestDto request)
        {
            var offer = request.FlightOffer;

            var flightOfferId = offer.GetProperty("id").GetString();

            _logger.LogInformation(
    "Starting booking creation for FlightOfferId {FlightOfferId}",
    flightOfferId);

            var itinerary = offer.GetProperty("itineraries")[0];
            var segment = itinerary.GetProperty("segments")[0];

            var origin = segment.GetProperty("departure").GetProperty("iataCode").GetString();
            var destination = segment.GetProperty("arrival").GetProperty("iataCode").GetString();
            var departureDateTime = segment.GetProperty("departure").GetProperty("at").GetString();

            var priceObj = offer.GetProperty("price");
            var totalPrice = decimal.Parse(priceObj.GetProperty("total").GetString()!);
            var currency = priceObj.GetProperty("currency").GetString();

            // 1️⃣ Save as Pending first
            var booking = new Booking
            {
                UserId = "TEMP_USER",
                FlightOfferId = flightOfferId!,
                Origin = origin!,
                Destination = destination!,
                DepartureDate = DateTime.Parse(departureDateTime!),
                Price = totalPrice,
                Currency = currency!,
                Status = "Pending"
            };

            await _bookingRepository.AddAsync(booking);

            _logger.LogInformation(
    "Booking saved as Pending with BookingId {BookingId}",
    booking.Id);

            try
            {

                _logger.LogInformation(
    "Calling Amadeus pricing API for BookingId {BookingId}",
    booking.Id);
                // 2️⃣ Confirm pricing
                var pricingResponse = await _amadeusClient
                    .ConfirmPricingAsync(request.FlightOffer);

                // 3️⃣ Move to AwaitingPayment
                booking.Status = "AwaitingPayment";

                _logger.LogInformation(
    "Pricing confirmed. BookingId {BookingId} moved to AwaitingPayment",
    booking.Id);

                booking.FlightOfferJson = request.FlightOffer.GetRawText();
                booking.TravelerJson = JsonSerializer.Serialize(request.Traveler);
                booking.ContactJson = JsonSerializer.Serialize(request.Contact);

                await _bookingRepository.UpdateAsync(booking);

                return new
                {
                    Message = "Pricing confirmed. Awaiting payment.",
                    BookingId = booking.Id
                };
            }
            catch(Exception ex)
            {
                booking.Status = "Failed";
                await _bookingRepository.UpdateAsync(booking);

                _logger.LogError(
    ex,
    "Pricing failed for BookingId {BookingId}",
    booking.Id);

                
                throw;
            }
        }

    }
}