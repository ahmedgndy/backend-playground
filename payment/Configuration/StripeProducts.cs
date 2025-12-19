using Payment.Models;

namespace Payment.Configuration;

// Centralized Stripe product/price mapping - NO magic strings
public static class StripeProducts
{
    // Product IDs from Stripe Dashboard
    public static readonly Dictionary<SubscriptionPlan, string> ProductIds = new()
    {
        { SubscriptionPlan.Starter, "prod_TapKAi79CuHQH0" },
        { SubscriptionPlan.Professional, "prod_TawnErWStdDwne" }
    };

    // Price IDs - UPDATE these with your actual Stripe price IDs
    public static readonly Dictionary<SubscriptionPlan, string> PriceIds = new()
    {
        { SubscriptionPlan.Starter, "price_1Sdku8GI6RzKXyl7DcRcZV33" },
        { SubscriptionPlan.Professional, "price_1SddfSGI6RzKXyl7O4LmBNQ4" }
    };

    public static string GetProductId(SubscriptionPlan plan) =>
        ProductIds.TryGetValue(plan, out var id) ? id : throw new ArgumentException($"Invalid plan: {plan}");

    public static string GetPriceId(SubscriptionPlan plan) =>
        PriceIds.TryGetValue(plan, out var id) ? id : throw new ArgumentException($"Invalid plan: {plan}");
}
