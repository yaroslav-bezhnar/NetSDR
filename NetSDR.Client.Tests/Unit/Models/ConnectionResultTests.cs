using FluentAssertions;
using NetSDR.Client.Models;

namespace NetSDR.Client.Tests.Unit.Models;

public class ConnectionResultTests
{
    #region methods

    [Fact]
    public void SuccessResult_IsMarkedCorrectly()
    {
        var result = new ConnectionResult(true);
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().BeNull();
        result.ToString().Should().Be("Success");
    }

    [Fact]
    public void ErrorResult_IsMarkedCorrectly()
    {
        var result = new ConnectionResult(false, "Timeout");
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Timeout");
        result.ToString().Should().Be("Error: Timeout");
    }

    [Fact]
    public void ErrorResult_NullMessage_ReturnsUnknown()
    {
        var result = new ConnectionResult(false);
        result.ToString().Should().Be("Error: Unknown");
    }

    [Theory]
    [InlineData(true, null, "Success")]
    [InlineData(false, "Connection lost", "Error: Connection lost")]
    [InlineData(false, null, "Error: Unknown")]
    public void ToString_FormatsCorrectly(bool success, string? msg, string expected)
    {
        var result = new ConnectionResult(success, msg);
        result.ToString().Should().Be(expected);
    }

    #endregion
}