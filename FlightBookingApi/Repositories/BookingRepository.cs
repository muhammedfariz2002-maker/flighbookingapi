using FlightBookingApi.Data;
using FlightBookingApi.Models;
using FlightBookingApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlightBookingApi.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking> AddAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }

        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id);
        }
    }
}