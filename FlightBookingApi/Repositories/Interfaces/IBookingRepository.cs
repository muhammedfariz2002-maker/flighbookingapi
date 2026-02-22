using FlightBookingApi.Models;

namespace FlightBookingApi.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<Booking> AddAsync(Booking booking);
        Task UpdateAsync(Booking booking);
        Task<Booking?> GetByIdAsync(int id);
    }
}