namespace Pinglingle.Shared;

public static class LocalDateUtil
{
    /// <summary>
    /// Gets a DateTimeOffset representing local midnight (00:00:00) upon
    /// the given date.
    /// </summary>
    public static DateTimeOffset LocalMidnightOn(
        DateTimeOffset dateTimeOffset, TimeSpan? delta = null)
    {
        var localDate = delta is { } d
            ? dateTimeOffset.LocalDateTime.Date.Add(d)
            : dateTimeOffset.LocalDateTime.Date;
        return new DateTimeOffset(
            localDate, TimeZoneInfo.Local.GetUtcOffset(localDate));
    }
}