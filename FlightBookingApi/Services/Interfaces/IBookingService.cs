using FlightBookingApi.DTOs;

namespace FlightBookingApi.Services.Interfaces
{
    public interface IBookingService
    {
        Task<object> CreateBookingAsync(BookingRequestDto request);
        
    }
}