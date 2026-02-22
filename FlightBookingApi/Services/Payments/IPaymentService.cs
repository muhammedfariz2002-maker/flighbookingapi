public interface IPaymentService
{
    Task<string> CreateCheckoutSessionAsync(
        int bookingId,
        decimal amount,
        string currency);
}