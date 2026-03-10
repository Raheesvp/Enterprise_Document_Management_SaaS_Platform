using FluentValidation;

namespace DocumentService.Application.Commands.AddDocumentVersion;

public sealed class AddDocumentVersionCommandValidator
    : AbstractValidator<AddDocumentVersionCommand>
{
    public AddDocumentVersionCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("Document ID is required");

        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.UploadedByUserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0)
            .WithMessage("File cannot be empty")
            .LessThanOrEqualTo(500L * 1024 * 1024)
            .WithMessage("File size cannot exceed 500MB");

        RuleFor(x => x.MimeType)
            .NotEmpty()
            .WithMessage("File type is required");
    }
}