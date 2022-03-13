using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pinglingle.Shared;

namespace Pinglingle.Shared.Tests;

[TestClass]
public class MathUtilTests
{
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(-1000, 0)]
    [DataRow(101, 10)]
    [DataRow(1101, 10)]
    [DataRow(0, 0)]
    [DataRow(100, 10)]
    [DataRow(5, 0.5)]
    [DataRow(50, 5)]
    [DataRow(95, 9.5)]
    public void CalculatesCorrectPercentileOfNoughtToTen(
        double percentile, double expectedValue)
    {
        var numbers = Enumerable.Range(0, 11).ToList();
        var value = MathUtil.Percentile(numbers, percentile);
        value.Should().Be(expectedValue);
    }
}