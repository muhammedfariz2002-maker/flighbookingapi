using System.Text.Json;

namespace FlightBookingApi.DTOs
{
    public class BookingRequestDto
    {
        public JsonElement FlightOffer { get; set; }

        public TravelerDto Traveler { get; set; }

        public ContactDto Contact { get; set; }
    }

    public class TravelerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DateOfBirth { get; set; }
        public string Gender { get; set; }
    }

    public class ContactDto
    {
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
    }
}