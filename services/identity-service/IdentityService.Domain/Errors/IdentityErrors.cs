

using Shared.Domain.Common;

namespace IdentityService.Domain.Errors;

public static class IdentityErrors
{
    public static class Tenant
    {
        public static Error NotFound(Guid id)
        => Error.NotFound("Tenant", id);

        public static  Error SubdomainTaken =
       new("Tenant.SubdomainTaken",
                "This subdomain is already taken by another tenant");

        public static readonly Error Inactive =
            new("Tenant.Inactive",
                "This tenant account has been deactivated");
    }

    public static class User
    {
        public static Error NotFound(Guid id)
            => Error.NotFound("User", id);

        public static readonly Error EmailTaken =
            new("User.EmailTaken",
                "A user with this email already exists in this tenant");

        public static readonly Error InvalidCredentials =
            new("User.InvalidCredentials",
                "The email or password provided is incorrect");

        public static readonly Error Inactive =
            new("User.Inactive",
                "This user account has been deactivated");
    }

    public static class Token
    {
        public static readonly Error Invalid =
            new("Token.Invalid",
                "The provided token is invalid or has expired");

        public static readonly Error Revoked =
            new("Token.Revoked",
                "The provided token has been revoked");
    }
}