using Gateway.API.Extensions;
using Gateway.API.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────────
// Serilog structured logging — every log line has TenantId, RequestId etc.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ── YARP Reverse Proxy ─────────────────────────────────────────────────────
// Reads route + cluster config from appsettings.json
// Hot-reload supported — change routes without restarting
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ── Authentication + Authorization ────────────────────────────────────────
// JWT validated HERE at gateway — downstream services trust the headers
builder.Services.AddGatewayAuthentication(builder.Configuration);

// ── Rate Limiting ──────────────────────────────────────────────────────────
builder.Services.AddGatewayRateLimiting();

// ── Health Checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── CORS ───────────────────────────────────────────────────────────────────
// Configured once at gateway — downstream services don't need CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",  // React dev server
                "http://localhost:5173")  // Vite dev server
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());         // Required for SignalR
});

var app = builder.Build();

// ── Middleware Pipeline (ORDER MATTERS) ────────────────────────────────────
// 1. Serilog request logging — log every request
app.UseSerilogRequestLogging();

// 2. CORS — must be before auth
app.UseCors("AllowFrontend");

// 3. Rate limiting — reject before spending auth resources
app.UseRateLimiter();

// 4. Authentication — validate JWT
app.UseAuthentication();

// 5. Authorization — check policies
app.UseAuthorization();

// 6. Tenant resolution — extract tenant from JWT, add headers
app.UseMiddleware<TenantResolutionMiddleware>();

// 7. Health check endpoint
app.MapHealthChecks("/health");

// 8. YARP — forward to downstream services
app.MapReverseProxy();

app.Run();