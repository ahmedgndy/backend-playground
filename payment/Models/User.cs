namespace Payment.Models;

// Minimal user entity - only stores Stripe reference and plan
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? StripeCustomerId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.None;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
