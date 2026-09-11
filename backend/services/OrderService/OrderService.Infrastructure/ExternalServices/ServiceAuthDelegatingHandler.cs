using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace OrderService.Infrastructure.ExternalServices;

/// <summary>Mints a short-lived service-identity JWT (role "service") signed with the same shared signing key
/// both APIs trust, and attaches it to every outgoing call to Product Service. This is the local-dev stand-in
/// for a proper service-to-service credential (e.g. client-credentials OAuth2 flow against the IdP chosen in
/// research.md item 5) — swap the token source here once a real IdP issues service tokens; callers of
/// IProductServiceClient do not change.</summary>
public sealed class ServiceAuthDelegatingHandler(IConfiguration configuration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateServiceToken());
        return await base.SendAsync(request, ct);
    }

    private string CreateServiceToken()
    {
        var key = configuration["Auth:JwtSigningKey"] ?? "local-dev-signing-key-change-me-please-32bytes+";
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim("sub", "order-service"),
                new Claim("role", "service")
            },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
