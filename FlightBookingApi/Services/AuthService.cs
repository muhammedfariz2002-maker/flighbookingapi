using FlightBookingApi.Data.Models;
using FlightBookingApi.Repositories.Interfaces;
using FlightBookingApi.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using FlightBookingApi.Helpers;
using FlightBookingApi.DTOs;
using Microsoft.Extensions.Logging;

namespace FlightBookingApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly JwtTokenGenerator _jwtTokenGenerator;
        private readonly ILogger<AuthService> _logger;


        public AuthService(IUserRepository userRepository,
            JwtTokenGenerator jwtTokenGenerator, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
            _logger = logger;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            return user != null;
        }

        public async Task RegisterUserAsync(string email, string password)
        {
            var user = new User
            {
                Email = email,
                PasswordHash = _passwordHasher.HashPassword(null!, password)
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            _logger.LogInformation("User registered successfully: {Email}", email);
        }

        public async Task<AuthResponseDto?> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("Login failed. User not found: {Email}", email);
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

            if (result == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login failed. Invalid password for: {Email}", email);
                return null;
            }

            var token = _jwtTokenGenerator.GenerateToken(user);

            _logger.LogInformation("Login successful for: {Email}", email);

            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };
        }
    }
}