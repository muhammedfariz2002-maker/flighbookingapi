using FlightBookingApi.DTOs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.WebUtilities;
using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FlightBookingApi.Infrastructure.ExternalServices
{
    public class AmadeusClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private string? _accessToken;
        private DateTime _tokenExpiry;

        public AmadeusClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            // If token still valid, reuse it
            if (!string.IsNullOrEmpty(_accessToken) && _tokenExpiry > DateTime.UtcNow)
            {
                return _accessToken;
            }

            var baseUrl = _configuration["AmadeusSettings:BaseUrl"];
            var clientId = _configuration["AmadeusSettings:ClientId"];
            var clientSecret = _configuration["AmadeusSettings:ClientSecret"];

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl}/v1/security/oauth2/token");

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", clientId! },
                { "client_secret", clientSecret! }
            });

            var response = await _httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);

            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 1 min buffer

            return _accessToken!;
        }


        public async Task<string> SearchFlightsAsync(
            string origin,
            string destination,
            string departureDate)
        {
            var token = await GetAccessTokenAsync();

            var baseUrl = _configuration["AmadeusSettings:BaseUrl"];

            var url = $"{baseUrl}/v2/shopping/flight-offers";

            var queryParams = new Dictionary<string, string>
    {
        { "originLocationCode", origin },
        { "destinationLocationCode", destination },
        { "departureDate", departureDate },
        { "adults", "1" },
        { "max", "3" }
    };

            var fullUrl = QueryHelpers.AddQueryString(url, queryParams);

            Console.WriteLine("FINAL URL: " + fullUrl);

            var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();

            return body; // temporary raw return
        }


        public async Task<string> ConfirmPricingAsync(JsonElement flightOffer)
        {
            var token = await GetAccessTokenAsync();

            var baseUrl = _configuration["AmadeusSettings:BaseUrl"];
            var url = $"{baseUrl}/v1/shopping/flight-offers/pricing";

            var pricingRequest = new
            {
                data = new
                {
                    type = "flight-offers-pricing",
                    flightOffers = new[] { flightOffer }
                }
            };

            var jsonBody = JsonSerializer.Serialize(pricingRequest);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Content = new StringContent(
                jsonBody,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            return body;
        }


        public async Task<string> CreateOrderAsync(
    JsonElement flightOffer,
    TravelerDto traveler,
    ContactDto contact)
        {
            var token = await GetAccessTokenAsync();

            var baseUrl = _configuration["AmadeusSettings:BaseUrl"];
            var url = $"{baseUrl}/v1/booking/flight-orders";

            var orderRequest = new
            {
                data = new
                {
                    type = "flight-order",
                    flightOffers = new[] { flightOffer },
                    travelers = new[]
                    {
                new
                {
                    id = "1",
                    dateOfBirth = traveler.DateOfBirth,
                    name = new
                    {
                        firstName = traveler.FirstName,
                        lastName = traveler.LastName
                    },
                    gender = traveler.Gender,
                    contact = new
                    {
                        emailAddress = contact.Email,
                        phones = new[]
                        {
                            new
                            {
                                deviceType = "MOBILE",
                                number = contact.PhoneNumber
                            }
                        }
                    }
                }
            }
                }
            };

            var jsonBody = JsonSerializer.Serialize(orderRequest);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Content = new StringContent(
                jsonBody,
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return body;
        }
    }
}