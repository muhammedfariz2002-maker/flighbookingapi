namespace FlightBookingApi.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}