using backend;
using System.Security.Claims;
namespace backend.Infrastructure;

public static class UserExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(id))
            throw new InvalidOperationException("User id not found in claims");

        return int.Parse(id);
    }
}
