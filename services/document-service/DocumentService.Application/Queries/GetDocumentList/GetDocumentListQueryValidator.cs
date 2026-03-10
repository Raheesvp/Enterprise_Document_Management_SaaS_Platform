using FluentValidation;

namespace DocumentService.Application.Queries.GetDocumentList;

// Queries can also have validators — not just commands
// This prevents invalid pagination values hitting the database
public sealed class GetDocumentListQueryValidator
    : AbstractValidator<GetDocumentListQuery>
{
    public GetDocumentListQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100");

        // Date range validation — FromDate cannot be after ToDate
        When(x => x.FromDate.HasValue && x.ToDate.HasValue, () =>
        {
            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(x => x.ToDate)
                .WithMessage("FromDate cannot be after ToDate");
        });

        // SearchTerm length limit — prevents massive query strings
        When(x => x.SearchTerm is not null, () =>
        {
            RuleFor(x => x.SearchTerm!)
                .MaximumLength(200)
                .WithMessage("Search term cannot exceed 200 characters");
        });
    }
}