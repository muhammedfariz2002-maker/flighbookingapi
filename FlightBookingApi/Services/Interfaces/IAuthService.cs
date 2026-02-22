using FlightBookingApi.Data.Models;
using FlightBookingApi.DTOs;

namespace FlightBookingApi.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> UserExistsAsync(string email);
        Task RegisterUserAsync(string email, string password);
        Task<AuthResponseDto?> LoginAsync(string email, string password);
    }
}