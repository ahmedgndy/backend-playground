    using Payment.Models;

    namespace Payment.Services;

    // Service interface - all Stripe operations go through here
    public interface IStripeService
    {
        // Customer management
        Task<Payment.DTOs.CustomerResponse> CreateCustomerAsync(string email, string? name = null);
        Task<Payment.DTOs.CustomerResponse?> GetCustomerAsync(string customerId);

        // Payment method management
        Task<List<Payment.DTOs.PaymentMethodResponse>> GetPaymentMethodsAsync(string customerId);
        Task<Payment.DTOs.PaymentMethodResponse?> GetDefaultPaymentMethodAsync(string customerId);
        Task SetDefaultPaymentMethodAsync(string customerId, string paymentMethodId);

        // Checkout sessions
        Task<Payment.DTOs.CheckoutSessionResponse> CreateCheckoutSessionAsync(string customerId, SubscriptionPlan plan);
        Task<Payment.DTOs.CheckoutSessionResponse> CreateSetupSessionAsync(string customerId);

        // Payments
        Task<Payment.DTOs.LastPaymentResponse?> GetLastPaymentAsync(string customerId);

        // Subscriptions
        Task<string?> GetActiveSubscriptionIdAsync(string customerId);
        Task ChangePlanAsync(string customerId, SubscriptionPlan newPlan);
    }
