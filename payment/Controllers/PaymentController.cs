using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payment.Data;
using Payment.DTOs;
using Payment.Models;
using Payment.Services;

namespace Payment.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly AppDbContext _db;

    public PaymentController(IStripeService stripeService, AppDbContext db)
    {
        _stripeService = stripeService;
        _db = db;
    }

    // Creates or retrieves a Stripe customer and stores reference locally
    [HttpPost("customer")]
    public async Task<ActionResult<CustomerResponse>> CreateOrGetCustomer([FromBody] CreateCustomerRequest request)
    {
        // Check if user already exists
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser?.StripeCustomerId != null)
        {
            var existing = await _stripeService.GetCustomerAsync(existingUser.StripeCustomerId);
            if (existing != null) return Ok(existing);
        }

        // Create new Stripe customer
        var customer = await _stripeService.CreateCustomerAsync(request.Email, request.Name);

        if (existingUser != null)
        {
            existingUser.StripeCustomerId = customer.CustomerId;
        }
        else
        {
            _db.Users.Add(new User
            {
                Email = request.Email,
                StripeCustomerId = customer.CustomerId,
                SubscriptionPlan = SubscriptionPlan.None
            });
        }

        await _db.SaveChangesAsync();
        return Ok(customer);
    }

    // Gets customer by email
    [HttpGet("customer/{email}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user?.StripeCustomerId == null)
            return NotFound("Customer not found");

        var customer = await _stripeService.GetCustomerAsync(user.StripeCustomerId);
        return customer != null ? Ok(customer) : NotFound("Stripe customer not found");
    }

    // Creates a setup session to add a new payment method
    [HttpPost("payment-method/setup/{customerId}")]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateSetupSession(string customerId)
    {
        var session = await _stripeService.CreateSetupSessionAsync(customerId);
        return Ok(session);
    }

    // Lists all payment methods for a customer - live from Stripe
    [HttpGet("payment-methods/{customerId}")]
    public async Task<ActionResult<List<PaymentMethodResponse>>> GetPaymentMethods(string customerId)
    {
        var methods = await _stripeService.GetPaymentMethodsAsync(customerId);
        return Ok(methods);
    }

    // Gets the default payment method - live from Stripe
    [HttpGet("payment-method/default/{customerId}")]
    public async Task<ActionResult<PaymentMethodResponse>> GetDefaultPaymentMethod(string customerId)
    {
        var method = await _stripeService.GetDefaultPaymentMethodAsync(customerId);
        return method != null ? Ok(method) : NotFound("No default payment method set");
    }

    // Sets a payment method as default in Stripe
    [HttpPut("payment-method/default/{customerId}")]
    public async Task<IActionResult> SetDefaultPaymentMethod(
        string customerId,
        [FromBody] SetDefaultPaymentMethodRequest request)
    {
        await _stripeService.SetDefaultPaymentMethodAsync(customerId, request.PaymentMethodId);
        return Ok(new { message = "Default payment method updated" });
    }

    // Creates a checkout session for subscription
    [HttpPost("checkout/{customerId}")]
    public async Task<ActionResult<CheckoutSessionResponse>> CreateCheckoutSession(
        string customerId,
        [FromQuery] SubscriptionPlan plan)
    {
        if (plan == SubscriptionPlan.None)
            return BadRequest("Invalid plan selected");

        var session = await _stripeService.CreateCheckoutSessionAsync(customerId, plan);
        return Ok(session);
    }

    // Gets the last successful payment - live from Stripe
    [HttpGet("last-payment/{customerId}")]
    public async Task<ActionResult<LastPaymentResponse>> GetLastPayment(string customerId)
    {
        var payment = await _stripeService.GetLastPaymentAsync(customerId);
        return payment != null ? Ok(payment) : NotFound("No payments found");
    }

    // Gets user's current plan from local DB
    [HttpGet("plan/{email}")]
    public async Task<ActionResult<UserPlanResponse>> GetUserPlan(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return NotFound("User not found");

        return Ok(new UserPlanResponse(user.Id, user.Email, user.SubscriptionPlan, user.StripeCustomerId));
    }

    // Changes subscription plan - updates in Stripe and locally
    [HttpPut("plan/{email}")]
    public async Task<IActionResult> ChangePlan(string email, [FromBody] ChangePlanRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user?.StripeCustomerId == null)
            return NotFound("User not found");

        if (request.NewPlan == SubscriptionPlan.None)
            return BadRequest("Invalid plan");

        try
        {
            await _stripeService.ChangePlanAsync(user.StripeCustomerId, request.NewPlan);
            user.SubscriptionPlan = request.NewPlan;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Plan changed to {request.NewPlan}" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Updates local plan after successful checkout (called from webhook or frontend)
    [HttpPost("plan/confirm/{email}")]
    public async Task<IActionResult> ConfirmPlan(string email, [FromQuery] SubscriptionPlan plan)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return NotFound("User not found");

        user.SubscriptionPlan = plan;
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Plan confirmed: {plan}" });
    }
}
