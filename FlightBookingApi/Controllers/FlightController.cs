using FlightBookingApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FlightBookingApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FlightController : ControllerBase
    {
        private readonly IFlightService _flightService;

        public FlightController(IFlightService flightService)
        {
            _flightService = flightService;
        }

        [EnableRateLimiting("FlightSearchPolicy")]
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string origin,
            string destination,
            string departureDate)
        {
            var result = await _flightService.SearchFlightsAsync(
                origin,
                destination,
                departureDate);

            return Ok(result);
        }
    }
}