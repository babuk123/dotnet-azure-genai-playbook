// How bearer-token (JWT) auth and policy-based authorization work in ASP.NET Core.
// The demo validates HS256 tokens with a small custom handler so every step is visible and runs offline.
// production/ shows the real setup: Microsoft Entra ID with JwtBearer, plus mTLS client certificates.
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var signingKey = Encoding.UTF8.GetBytes("demo-signing-key-32-bytes-minimum!!");   // production: asymmetric keys from Entra ID
builder.Services.AddSingleton(new DemoTokenService(signingKey, issuer: "https://login.demo-bank", audience: "api://orders"));

builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, DemoJwtHandler>("Bearer", null);

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Orders.Read", p => p.RequireClaim("scp", "orders.read"));
    o.AddPolicy("Orders.Approve", p => p.RequireRole("Approver").RequireClaim("amr", "mfa"));  // role + MFA
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/demo/token", (DemoTokenService tokens, string user, string scopes, string? roles, bool mfa) =>
    tokens.Create(user, scopes.Split(','), roles?.Split(',') ?? [], mfa));                  // stand-in for Entra ID

app.MapGet("/api/orders", (ClaimsPrincipal user) => new { orders = new[] { "ORD-1", "ORD-2" }, user = user.Identity!.Name })
   .RequireAuthorization("Orders.Read");

app.MapPost("/api/orders/{id}/approve", (string id, ClaimsPrincipal user) => $"{id} approved by {user.Identity!.Name}")
   .RequireAuthorization("Orders.Approve");

app.Run();

public class DemoTokenService(byte[] key, string issuer, string audience)
{
    public byte[] Key => key; public string Issuer => issuer; public string Audience => audience;

    public string Create(string user, string[] scopes, string[] roles, bool mfa)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        var payload = new Dictionary<string, object>
        {
            ["iss"] = issuer, ["aud"] = audience, ["sub"] = user, ["name"] = user,
            ["scp"] = string.Join(' ', scopes), ["roles"] = roles, ["amr"] = mfa ? new[] { "pwd", "mfa" } : new[] { "pwd" },
            ["exp"] = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()                 // short-lived access token
        };
        var unsigned = $"{B64(JsonSerializer.SerializeToUtf8Bytes(header))}.{B64(JsonSerializer.SerializeToUtf8Bytes(payload))}";
        return $"{unsigned}.{B64(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(unsigned)))}";
    }
    public static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public static byte[] FromB64(string s) { s = s.Replace('-', '+').Replace('_', '/'); return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=')); }
}

// Validates exactly what JwtBearer validates: signature, issuer, audience, expiry; then maps claims
public class DemoJwtHandler(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e, DemoTokenService tokens)
    : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var auth = Request.Headers.Authorization.ToString();
        if (!auth.StartsWith("Bearer ")) return Task.FromResult(AuthenticateResult.NoResult());
        var parts = auth["Bearer ".Length..].Split('.');
        if (parts.Length != 3) return Fail("malformed token");

        var expected = HMACSHA256.HashData(tokens.Key, Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"));
        if (!CryptographicOperations.FixedTimeEquals(expected, DemoTokenService.FromB64(parts[2]))) return Fail("bad signature");

        using var doc = JsonDocument.Parse(DemoTokenService.FromB64(parts[1]));
        var p = doc.RootElement;
        if (p.GetProperty("iss").GetString() != tokens.Issuer) return Fail("wrong issuer");
        if (p.GetProperty("aud").GetString() != tokens.Audience) return Fail("wrong audience");
        if (DateTimeOffset.FromUnixTimeSeconds(p.GetProperty("exp").GetInt64()) < DateTimeOffset.UtcNow) return Fail("expired");

        var claims = new List<Claim> { new(ClaimTypes.Name, p.GetProperty("name").GetString()!) };
        claims.AddRange(p.GetProperty("scp").GetString()!.Split(' ').Select(s => new Claim("scp", s)));
        claims.AddRange(p.GetProperty("roles").EnumerateArray().Select(r => new Claim(ClaimTypes.Role, r.GetString()!)));
        claims.AddRange(p.GetProperty("amr").EnumerateArray().Select(a => new Claim("amr", a.GetString()!)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
    private static Task<AuthenticateResult> Fail(string why) => Task.FromResult(AuthenticateResult.Fail(why));
}
