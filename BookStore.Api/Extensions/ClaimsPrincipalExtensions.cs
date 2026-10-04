using System.Security.Claims;

namespace BookStore.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // The logged-in user's id, read from the validated token.
    // One place for this, instead of copying the same line into every controller.
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
}