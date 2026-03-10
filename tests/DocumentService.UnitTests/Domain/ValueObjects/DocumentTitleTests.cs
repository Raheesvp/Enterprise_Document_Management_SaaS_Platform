using DocumentService.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DocumentService.UnitTests.Domain.ValueObjects;

public class DocumentTitleTests {
    [Fact]
    public void Create_EmptyString_ThrowsArgumentException() {
        Action act = () => DocumentTitle.Create("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ValidTitle_TrimsWhitespace() {
        var title = DocumentTitle.Create("  My Document  ");
        title.Value.Should().Be("My Document");
    }
}