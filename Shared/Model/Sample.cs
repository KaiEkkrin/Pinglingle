using System.Net.NetworkInformation;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pinglingle.Shared.Model;

/// <summary>
/// One single result of pinging a target. 
/// </summary>
public class Sample
{
    public long Id { get; set; }

    public long? TargetId { get; set; }

    [ForeignKey("TargetId")]
    public Target? Target { get; set; }

    /// <summary>
    /// When the ping occurred. 
    /// </summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// The whole number of milliseconds it took to get a response back.
    /// If null, no response was received within the timeout.
    /// </summary>
    public int? ResponseTimeMillis { get; set; }

    /// <summary>
    /// The IPStatus value that came back from the ping.
    /// </summary>
    public IPStatus Status { get; set; }
}