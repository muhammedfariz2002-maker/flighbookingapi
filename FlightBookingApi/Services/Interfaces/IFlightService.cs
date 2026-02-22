namespace FlightBookingApi.Services.Interfaces
{
    public interface IFlightService
    {
        Task<object> SearchFlightsAsync(string origin,
    string destination,
    string departureDate);
    }
}