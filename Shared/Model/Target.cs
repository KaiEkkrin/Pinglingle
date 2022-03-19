using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Pinglingle.Shared.Model;

/// <summary>
/// A target address that will be regularly pinged by Pinglingle.
/// </summary>
public class Target
{
    public long Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public int Frequency { get; set; } = 1;

    [JsonIgnore]
    public virtual List<Digest> Digests { get; set; } = new();

    [JsonIgnore]
    public virtual List<Sample> Samples { get; set; } = new();
}