namespace Pinglingle.Shared.Model;

/// <summary>
/// A target address that will be regularly pinged by Pinglingle.
/// </summary>
public class Target
{
    public long Id { get; set; }
    public string Address { get; set; } = string.Empty;

    public virtual List<Sample> Samples { get; set; } = new();
}