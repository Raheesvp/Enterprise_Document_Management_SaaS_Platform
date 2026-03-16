using Gateway.API.Extensions;
using Gateway.API.Middleware;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
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
builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
   

 
    // 1. Define the Security Scheme (How the lock button works)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// ── CORS ───────────────────────────────────────────────────────────────────
// Configured once at gateway — downstream services don't need CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",  
                "http://localhost:5173"
                )  // Vite dev server
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());        
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        // Same-origin path via gateway proxy (avoids browser CORS issues)
        c.SwaggerEndpoint("/identity-swagger/v1/swagger.json", "Identity Service API");

        c.SwaggerEndpoint("/workflow-swagger/v1/swagger.json", "Workflow Service");

        // This shows the Gateway's own (empty) definition
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service(via Gateway)");

        c.RoutePrefix = "swagger";
    });
}

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

// 8. YARP — forward to downstream services
app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
