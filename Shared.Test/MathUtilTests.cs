using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pinglingle.Shared.Tests;

[TestClass]
public class MathUtilTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(50)]
    [DataRow(100)]
    public void PercentileOfNoNumbersIsZero(double percentile)
    {
        var value = MathUtil.Percentile(Array.Empty<int>(), percentile);
        value.Should().BeApproximately(0, 0.00001);
    }

    [TestMethod]
    [DataRow(4, 0)]
    [DataRow(5, 50)]
    [DataRow(-1, 100)]
    public void PercentileOfOneNumberIsThatNumber(int number, double percentile)
    {
        var value = MathUtil.Percentile(new[] { number }, percentile);
        value.Should().BeApproximately(number, 0.00001);
    }
    
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(-1000, 0)]
    [DataRow(101, 10)]
    [DataRow(1101, 10)]
    [DataRow(0, 0)]
    [DataRow(100, 10)]
    [DataRow(2.5, 0.25)]
    [DataRow(5, 0.5)]
    [DataRow(7.5, 0.75)]
    [DataRow(50, 5)]
    [DataRow(92.5, 9.25)]
    [DataRow(95, 9.5)]
    [DataRow(97.5, 9.75)]
    public void CalculatesCorrectPercentileOfNoughtToTen(
        double percentile, double expectedValue)
    {
        var numbers = Enumerable.Range(0, 11).ToList();
        var value = MathUtil.Percentile(numbers, percentile);
        value.Should().BeApproximately(expectedValue, 0.00001);
    }

    [TestMethod]
    [DataRow("2022-03-19 14:59:32Z", "2022-03-19 14:55:00Z")]
    [DataRow("2022-03-19 15:00:00Z", "2022-03-19 15:00:00Z")]
    [DataRow("2022-03-19 15:01:03Z", "2022-03-19 15:00:00Z")]
    [DataRow("2022-03-19 15:02:24Z", "2022-03-19 15:00:00Z")]
    [DataRow("2022-03-19 15:03:00Z", "2022-03-19 15:00:00Z")]
    [DataRow("2022-03-19 15:04:59Z", "2022-03-19 15:00:00Z")]
    [DataRow("2022-03-19 15:05:00Z", "2022-03-19 15:05:00Z")]
    [DataRow("2022-03-19 15:13:42Z", "2022-03-19 15:10:00Z")]
    [DataRow("2022-03-19 15:13:42+01:00", "2022-03-19 14:10:00Z")]
    public void CalculatesCorrectFiveMinuteFloor(
        string dateTimeString, string expectedDateTimeString)
    {
        var dateTime = DateTimeOffset.Parse(dateTimeString);
        var expectedDateTime = DateTimeOffset.Parse(expectedDateTimeString);

        var fiveMinuteFloor = MathUtil.FiveMinuteFloor(dateTime);
        fiveMinuteFloor.Should().Be(expectedDateTime);
    }
}