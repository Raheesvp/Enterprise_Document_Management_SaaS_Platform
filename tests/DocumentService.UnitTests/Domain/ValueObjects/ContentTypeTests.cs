using DocumentService.Domain.ValueObjects;
using DocumentService.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DocumentService.UnitTests.Domain.ValueObjects;

public class ContentTypeTests
{
    [Theory]
    [InlineData("application/pdf", DocumentType.Pdf)]
    [InlineData("image/jpeg", DocumentType.Image)]
    [InlineData("application/msword", DocumentType.Word)]
    [InlineData("unknown/type", DocumentType.Other)]
    public void Create_ShouldMapToCorrectType(string mimeType, DocumentType expectedType)
    {
        // Act
        var result = ContentType.Create(mimeType);

        // Assert
        result.DocumentType.Should().Be(expectedType);
    }
}