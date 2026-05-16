using System.Security.Claims;

namespace IssueTrackerApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
        => user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("UserId claim not found.");

    public static string GetUserName(this ClaimsPrincipal user)
        => user.FindFirstValue("name")
        ?? user.FindFirstValue("cognito:username")
        ?? user.FindFirstValue("email")
        ?? "Anonymous";
}
