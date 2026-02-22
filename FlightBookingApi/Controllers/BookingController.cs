using FlightBookingApi.DTOs;
using FlightBookingApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FlightBookingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        // [Authorize]
        [EnableRateLimiting("BookingPolicy")]
        [HttpPost]
        public async Task<IActionResult> CreateBooking(BookingRequestDto request)
        {
            var result = await _bookingService.CreateBookingAsync(request);

            return Ok(result);
        }


    }
}