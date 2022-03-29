using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pinglingle.Shared.Tests;

[TestClass]
public class LocalDateUtilTests
{
    // This test will work on a system in Europe (London) time only. :P
    // In this time zone Daylight Savings time began on 2022-03-27 01:00:00,
    // going from offset +00:00 to +01:00.
    [TestMethod]
    [DataRow("2022-03-29 22:44:00+01:00", 0, "2022-03-29 00:00:00+01:00")]
    [DataRow("2022-03-29 22:44:00+01:00", 1, "2022-03-30 00:00:00+01:00")]
    [DataRow("2022-03-29 22:44:00+01:00", -1, "2022-03-28 00:00:00+01:00")]
    [DataRow("2022-03-29 22:44:00+01:00", -2, "2022-03-27 00:00:00+00:00")]
    [DataRow("2022-03-29 22:44:00+01:00", -3, "2022-03-26 00:00:00+00:00")]
    [DataRow("2022-03-29 01:00:00+01:00", 1, "2022-03-30 00:00:00+01:00")]
    [DataRow("2022-03-29 01:00:00+01:00", -1, "2022-03-28 00:00:00+01:00")]
    [DataRow("2022-03-29 01:00:00+01:00", -2, "2022-03-27 00:00:00+00:00")]
    [DataRow("2022-03-29 01:00:00+01:00", -3, "2022-03-26 00:00:00+00:00")]
    [DataRow("2022-03-26 22:44:00+00:00", 0, "2022-03-26 00:00:00+00:00")]
    [DataRow("2022-03-26 22:44:00+00:00", 1, "2022-03-27 00:00:00+00:00")]
    [DataRow("2022-03-26 22:44:00+00:00", 2, "2022-03-28 00:00:00+01:00")]
    [DataRow("2022-03-26 22:44:00+00:00", 3, "2022-03-29 00:00:00+01:00")]
    public void LocalDateUtilWorksAsExpected(
        string dateTimeString, int daysDelta, string expectedDateTimeString)
    {
        var dateTime = DateTimeOffset.Parse(dateTimeString);
        var midnightDateTime = LocalDateUtil.LocalMidnightOn(
            dateTime, TimeSpan.FromDays(daysDelta));
        
        var expectedDateTime = DateTimeOffset.Parse(expectedDateTimeString);
        midnightDateTime.Should().Be(expectedDateTime);
    }
}