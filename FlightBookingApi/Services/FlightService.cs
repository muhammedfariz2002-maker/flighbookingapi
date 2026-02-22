using FlightBookingApi.Infrastructure.ExternalServices;
using FlightBookingApi.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;

namespace FlightBookingApi.Services
{
    public class FlightService : IFlightService
    {
        private readonly AmadeusClient _amadeusClient;
        private readonly IDistributedCache _cache;

        public FlightService(AmadeusClient amadeusClient, IDistributedCache cache)
        {
            _amadeusClient = amadeusClient;
            _cache = cache;
        }
        public async Task<object> SearchFlightsAsync(
            string origin,
            string destination,
            string departureDate)
        {
            var cacheKey = $"flight:{origin}:{destination}:{departureDate}";

            // 1️⃣ Check Redis
            var cachedData = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedData))
            {
                Console.WriteLine("CACHE HIT 🔥");
                return JsonSerializer.Deserialize<object>(cachedData)!;
            }

            Console.WriteLine("CACHE MISS ❌ Calling Amadeus...");

            // 2️⃣ Call Amadeus
            var json = await _amadeusClient.SearchFlightsAsync(
                origin,
                destination,
                departureDate);

            // 3️⃣ Store in Redis for 5 minutes
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            await _cache.SetStringAsync(cacheKey, json, options);

            return JsonSerializer.Deserialize<object>(json)!;
        }
    }
}