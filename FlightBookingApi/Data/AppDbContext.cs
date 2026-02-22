using FlightBookingApi.Data.Models;
using FlightBookingApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FlightBookingApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Booking> Bookings { get; set; }
    }
}