using System.ComponentModel.DataAnnotations;

namespace FlightBookingApi.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        public string UserId { get; set; }

        public string FlightOfferId { get; set; }

        public string Origin { get; set; }

        public string Destination { get; set; }

        public DateTime DepartureDate { get; set; }

        public decimal Price { get; set; }

        public string Currency { get; set; }

        public string Status { get; set; }  // Pending, Confirmed, Failed

        public string? AmadeusOrderId { get; set; }
        public string? FlightOfferJson { get; set; }
        public string? TravelerJson { get; set; }
        public string? ContactJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}