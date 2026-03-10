using Shared.Domain.Common;

namespace DocumentService.Domain.Errors;

// Centralized error catalog — same pattern as IdentityErrors
// Every possible domain failure has a named constant here
// No magic strings anywhere in the codebase
//
// Senior rule: if you are typing an error message inline
// inside a handler, that is a bug — it belongs here
public static class DocumentErrors
{
    public static class Document
    {
        public static Error NotFound(Guid id) =>
            new("Document.NotFound",
                $"Document with ID '{id}' was not found");

        public static readonly Error Archived =
            new("Document.Archived",
                "This document has been archived and cannot be modified");

        public static readonly Error InvalidStatusTransition =
            new("Document.InvalidStatusTransition",
                "This status transition is not allowed");

        public static readonly Error VersionUploadNotAllowed =
            new("Document.VersionUploadNotAllowed",
                "New versions can only be added to Active documents");

        public static readonly Error FileSizeExceeded =
            new("Document.FileSizeExceeded",
                "File size cannot exceed 500MB");

        public static readonly Error UnsupportedFileType =
            new("Document.UnsupportedFileType",
                "The provided file type is not supported");

        public static readonly Error TitleRequired =
            new("Document.TitleRequired",
                "Document title is required");

        public static readonly Error AlreadyUnderReview =
            new("Document.AlreadyUnderReview",
                "Document is already in the review workflow");
    }

    public static class Version
    {
        public static Error NotFound(Guid documentId, int versionNumber) =>
            new("Version.NotFound",
                $"Version {versionNumber} not found on document '{documentId}'");

        public static readonly Error OcrFailed =
            new("Version.OcrFailed",
                "Text extraction failed for this document version");
    }
}