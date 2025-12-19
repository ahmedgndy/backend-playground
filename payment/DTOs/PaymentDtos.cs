using Payment.Models;

namespace Payment.DTOs;

// Request/Response DTOs - keeps controllers clean

public record CreateCustomerRequest(string Email, string? Name = null);

public record CustomerResponse(string CustomerId, string Email, string? Name);

public record SetDefaultPaymentMethodRequest(string PaymentMethodId);

public record ChangePlanRequest(SubscriptionPlan NewPlan);

public record PaymentMethodResponse(
    string Id,
    string Type,
    string? Last4,
    string? Brand,
    long? ExpMonth,
    long? ExpYear,
    bool IsDefault
);

public record CheckoutSessionResponse(string SessionId, string Url);

public record LastPaymentResponse(
    string Id,
    long Amount,
    string Currency,
    string Status,
    DateTime Created,
    string? PaymentMethodLast4
);

public record UserPlanResponse(
    int UserId,
    string Email,
    SubscriptionPlan Plan,
    string? StripeCustomerId
);
