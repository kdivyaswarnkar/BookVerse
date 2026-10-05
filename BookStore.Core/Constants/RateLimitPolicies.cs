namespace BookStore.Core.Constants;

// Names of the rate limit policies. Used in [EnableRateLimiting("...")] on controller actions.
public static class RateLimitPolicies
{
    public const string Login = "login";        // login, refresh, verify email, reset password: stop password guessing
    public const string Email = "email";        // register, resend verification, forgot password: stop mail spam
    public const string Checkout = "checkout";  // coupon preview, place order, create payment: stop abuse by one user
}
