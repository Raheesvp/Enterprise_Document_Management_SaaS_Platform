using FluentValidation;


namespace IdentityService.Application.Commands.RegisterTenant;

public class RegisterTenantCommandValidator
    : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(100).WithMessage("Tenant name cannot exceed 100 characters");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required")
            .MaximumLength(50).WithMessage("Subdomain cannot exceed 50 characters")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Subdomain can only contain lowercase letters, numbers, and hyphens");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one number");
    }
}