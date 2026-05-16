using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace IssueTrackerApi.Middleware;

public class CognitoJwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _issuer;
    private readonly string _clientId;
    private IList<JsonWebKey>? _cachedKeys;
    private readonly SemaphoreSlim _keyLock = new(1, 1);

    public CognitoJwtMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        var region = config["Aws:Region"]!;
        var poolId = config["Aws:CognitoUserPoolId"]!;
        _clientId = config["Aws:CognitoAppClientId"]!;
        _issuer = $"https://cognito-idp.{region}.amazonaws.com/{poolId}";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var principal = await ValidateTokenAsync(token);
            if (principal is not null)
                context.User = principal;
        }

        await _next(context);
    }

    private async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var keys = await GetSigningKeysAsync();
            var handler = new JwtSecurityTokenHandler();

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _clientId,
                ValidateLifetime = true,
                IssuerSigningKeys = keys.Select(k => (SecurityKey)k),
                ClockSkew = TimeSpan.FromMinutes(5),
            };

            return handler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IList<JsonWebKey>> GetSigningKeysAsync()
    {
        if (_cachedKeys is not null) return _cachedKeys;

        await _keyLock.WaitAsync();
        try
        {
            if (_cachedKeys is not null) return _cachedKeys;
            using var http = new HttpClient();
            var json = await http.GetStringAsync($"{_issuer}/.well-known/jwks.json");
            _cachedKeys = new JsonWebKeySet(json).Keys;
            return _cachedKeys;
        }
        finally
        {
            _keyLock.Release();
        }
    }
}
