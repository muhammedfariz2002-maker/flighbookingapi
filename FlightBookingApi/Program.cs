using FlightBookingApi.Data;
using Microsoft.EntityFrameworkCore;
using FlightBookingApi.Repositories;
using FlightBookingApi.Repositories.Interfaces;
using FlightBookingApi.Services;
using FlightBookingApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FlightBookingApi.Helpers;
using System.Threading.RateLimiting;
using FlightBookingApi.Infrastructure.ExternalServices;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using Stripe;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFlightService, FlightService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentService, StripePaymentService>();

builder.Services.AddScoped<JwtTokenGenerator>();


var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});


builder.Services.AddRateLimiter(options =>
{

    options.AddPolicy("BookingPolicy", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.User?.Identity?.Name ??
                      context.Connection.RemoteIpAddress?.ToString() ??
                      "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many login attempts. Please try again later.", cancellationToken);
    };

    options.AddPolicy("FlightSearchPolicy", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        }));
    options.AddPolicy("LoginPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                 // Max 5 attempts
                Window = TimeSpan.FromMinutes(1), // Per 1 minute
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration =
        builder.Configuration["RedisSettings:ConnectionString"];

    options.InstanceName = "FlightBooking_";
});

builder.Services.AddHttpClient<AmadeusClient>();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()

    // Reduce framework noise
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)

    .WriteTo.Console()
    .WriteTo.File(
        "Logs/log-.txt",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();



StripeConfiguration.ApiKey =
    builder.Configuration["StripeSettings:SecretKey"];

builder.Host.UseSerilog();

var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();