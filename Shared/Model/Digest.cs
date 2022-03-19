using System.ComponentModel.DataAnnotations.Schema;

namespace Pinglingle.Shared.Model;

/// <summary>
/// A digest of samples taken within the period starting with its start time.
/// Easier to query and plot charts with -- avoid analysing all the samples
/// every time :)
/// </summary>
public class Digest
{
    public static readonly TimeSpan Period = TimeSpan.FromMinutes(5);

    public long Id { get; set; }

    public long? TargetId { get; set; }

    [ForeignKey("TargetId")]
    public Target? Target { get; set; }

    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// The number of samples taken during this period.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// The 5th percentile of response times of successful samples.
    /// </summary>
    public double Percentile5 { get; set; }

    /// <summary>
    /// The 50th percentile of response times of successful samples.
    /// </summary>
    public double Percentile50 { get; set; }

    /// <summary>
    /// The 95th percentile of response times of successful samples.
    /// </summary>
    public double Percentile95 { get; set; }

    /// <summary>
    /// The number of unsuccessful samples.
    /// </summary>
    public int ErrorCount { get; set; }
}