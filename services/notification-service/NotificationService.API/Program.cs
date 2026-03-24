using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NotificationService.Application;
using NotificationService.API.Hubs;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Services;
using Serilog;
using Shared.Domain.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// SignalR — real-time WebSocket connections
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors   = true;
    options.KeepAliveInterval      = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval  = TimeSpan.FromSeconds(30);
});

// CORS — required for SignalR from Angular :4200
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:4200",
                "http://localhost:5000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// JWT
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"]!;
var jwtIssuer    = builder.Configuration["JwtSettings:Issuer"]!;
var jwtAudience  = builder.Configuration["JwtSettings:Audience"]!;
var signingKey   = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(jwtSecretKey));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata       = false;
    options.UseSecurityTokenValidators = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtIssuer,
        ValidAudience            = jwtAudience,
        IssuerSigningKey         = signingKey,
        IssuerSigningKeyResolver = (token, secToken, kid, parameters) =>
            new List<SecurityKey> { signingKey },
        ClockSkew = TimeSpan.Zero
    };

    // CRITICAL — SignalR sends JWT via query string
    // not Authorization header for WebSocket connections
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request
                .Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Notification Service API", Version = "v1"
    });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType
                        .SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration
            .GetConnectionString("NotificationDb")!,
        name: "postgresql");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json",
        "Notification Service v1");
    c.RoutePrefix = "swagger";
});

app.UseSerilogRequestLogging();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// SignalR Hub endpoint
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
