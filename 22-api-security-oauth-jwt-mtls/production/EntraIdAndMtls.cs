// Production setup. Packages: Microsoft.AspNetCore.Authentication.JwtBearer (or Microsoft.Identity.Web),
// Microsoft.AspNetCore.Authentication.Certificate.
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);

// ---------- 1. OAuth 2.0 / OpenID Connect with Microsoft Entra ID ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://login.microsoftonline.com/<tenant-id>/v2.0";   // keys fetched from the OIDC metadata
        o.Audience = "api://orders-api";
        o.TokenValidationParameters.ValidateIssuer = true;
        o.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
        o.MapInboundClaims = false;                                             // keep "scp", "roles" names
    })
    // ---------- 2. mTLS: client certificate authentication (service-to-service, e.g. APIM → API) ----------
    .AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme, o =>
    {
        o.AllowedCertificateTypes = CertificateTypes.Chained;
        o.RevocationMode = X509RevocationMode.Online;
        o.Events = new CertificateAuthenticationEvents
        {
            OnCertificateValidated = ctx =>
            {
                var allowed = builder.Configuration.GetSection("Mtls:AllowedThumbprints").Get<string[]>() ?? [];
                if (!allowed.Contains(ctx.ClientCertificate.Thumbprint, StringComparer.OrdinalIgnoreCase))
                    ctx.Fail("Certificate not allowed");
                return Task.CompletedTask;
            }
        };
    });

// Kestrel asks the caller for a certificate during the TLS handshake
builder.WebHost.ConfigureKestrel(k => k.ConfigureHttpsDefaults(h =>
    h.ClientCertificateMode = ClientCertificateMode.RequireCertificate));
// Behind a TLS-terminating ingress/APIM, the cert arrives in a header instead:
// builder.Services.AddCertificateForwarding(o => o.CertificateHeader = "X-ARR-ClientCert");

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Orders.Read", p => p.RequireClaim("scp", "orders.read"));
    o.AddPolicy("ServiceToService", p => p.AddAuthenticationSchemes(CertificateAuthenticationDefaults.AuthenticationScheme)
                                          .RequireAuthenticatedUser());
});

var app = builder.Build();
app.UseHsts();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/orders", () => "ok").RequireAuthorization("Orders.Read");
app.MapPost("/internal/settlements", () => "ok").RequireAuthorization("ServiceToService");
app.Run();
