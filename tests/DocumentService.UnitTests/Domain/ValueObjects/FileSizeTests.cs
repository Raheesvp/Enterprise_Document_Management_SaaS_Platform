using DocumentService.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace DocumentService.UnitTests.Domain.ValueObjects;

public class FileSizeTests {
    [Fact]
    public void FromBytes_ValidSize_CreatesCorrectly() {
        var size = FileSize.FromBytes(1024);
        size.Bytes.Should().Be(1024);
    }

    [Fact]
    public void FromBytes_Above500MB_ThrowsArgumentException() {
        long tooBig = 501L * 1024 * 1024;
        Action act = () => FileSize.FromBytes(tooBig);
        act.Should().Throw<ArgumentException>();
    }
}