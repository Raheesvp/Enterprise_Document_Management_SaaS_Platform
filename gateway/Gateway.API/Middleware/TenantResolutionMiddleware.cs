
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;


namespace Gateway.API.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;   

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if(context.User.Identity?.IsAuthenticated == true) //first check the request has valid JWT token, if not, skip tenant resolution
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id"); //unique id of company/client.
             var userIdClaim = context.User.FindFirst(JwtRegisteredClaimNames.Sub);
             var emailClaim = context.User.FindFirst(JwtRegisteredClaimNames.Email);
             
             if(tenantIdClaim is not null)
            {
                 context.Request.Headers["X-Tenant-Id"] = tenantIdClaim.Value; 
                 context.Request.Headers["X-User-Id"] = userIdClaim?.Value ?? string.Empty;
                 context.Request.Headers["X-User-Email"] = emailClaim?.Value ?? string.Empty;

                 _logger.LogDebug(
                    "Tenant resolved: {TenantId} for user {UserId}",
                    tenantIdClaim.Value,
                    userIdClaim?.Value);
            }
             else
            {
                _logger.LogWarning(
                    "Authenticated request missing tenant_id claim. Path: {Path}",
                    context.Request.Path);
            }
                   
                   
                    }
                    await _next(context);
    }
    
}