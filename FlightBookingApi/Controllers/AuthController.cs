using FlightBookingApi.DTOs;
using FlightBookingApi.Helpers;
using FlightBookingApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace FlightBookingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequestDto request)
        {
            var userExists = await _authService.UserExistsAsync(request.Email);

            if (userExists)
                return BadRequest("User already exists.");

            await _authService.RegisterUserAsync(request.Email, request.Password);

            return Ok(new { Message = "User registered successfully." });
        }

        [EnableRateLimiting("LoginPolicy")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestDto request)
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);

            if (result == null)
                return Unauthorized("Invalid credentials.");

            return Ok(result);
        }


        [Authorize]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            return Ok(new
            {
                UserId = userId,
                Email = email
            });
        }
    }
}