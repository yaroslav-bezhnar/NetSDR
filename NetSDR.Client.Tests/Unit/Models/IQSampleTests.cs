using FluentAssertions;
using NetSDR.Client.Models;

namespace NetSDR.Client.Tests.Unit.Models;

public class IQSampleTests
{
    #region methods

    [Fact]
    public void Constructor_SetsValuesCorrectly()
    {
        var sample = new IQSample(3, -4);
        sample.I.Should().Be(3);
        sample.Q.Should().Be(-4);
    }

    [Fact]
    public void Magnitude_ComputesCorrectly()
    {
        var sample = new IQSample(3, 4);
        sample.Magnitude.Should().BeApproximately(5.0, 0.001);
    }

    [Fact]
    public void Phase_ComputesCorrectly()
    {
        var sample = new IQSample(0, 1);
        sample.Phase.Should().BeApproximately(Math.PI / 2, 0.001);
    }

    #endregion
}