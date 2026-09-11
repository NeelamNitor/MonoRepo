using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProductService.Api.Auth;
using ProductService.Api.Middleware;
using ProductService.Application;
using ProductService.Application.Common.Interfaces;
using ProductService.Infrastructure;
using ProductService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddProductApplication();
builder.Services.AddProductInfrastructure(builder.Configuration);

var jwtKey = builder.Configuration["Auth:JwtSigningKey"] ?? "local-dev-signing-key-change-me-please-32bytes+";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, the JWT handler's default inbound claim mapping rewrites the short "role" claim to
        // the long ClaimTypes.Role URI before TokenValidationParameters.RoleClaimType ever sees it, silently
        // breaking [Authorize(Roles=...)] checks (FR-012).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// Dev-only permissive CORS so order-app (Angular, Vite dev server) and product-app (Vue, Vite dev server) can
// call this API directly from the browser — both run on localhost but on different ports than this API, and
// browsers block cross-origin fetch() without an explicit policy even though curl/server-to-server calls are
// unaffected (that's why backend-only smoke tests didn't catch this). No API Gateway in v1 (architecture.md §1).
const string DevCorsPolicy = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
        policy.SetIsOriginAllowed(origin => new Uri(origin).Host is "localhost" or "127.0.0.1")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Local convenience only: apply pending EF Core migrations on startup so `dotnet run` against a fresh
    // product_db "just works" without a separate `dotnet ef database update` step during development.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors(DevCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ProductService" })).AllowAnonymous();

app.Run();

public partial class Program { } // exposed for WebApplicationFactory-based integration tests
