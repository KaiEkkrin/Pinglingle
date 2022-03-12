using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Pinglingle.Server;

internal static class AddressUtility
{
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(2);

    public static async Task<(IPStatus Status, int? Time)> MeasurePingMillisAsync(
        string address)
    {
        using Ping pingSender = new();
        var reply = await pingSender.SendPingAsync(
            address,
            (int)Math.Ceiling(PingTimeout.TotalMilliseconds),
            Guid.NewGuid().ToByteArray());
        
        var time = reply.Status switch
        {
            IPStatus.Success => (int?)reply.RoundtripTime,
            _ => null
        };

        return (reply.Status, time);
    }

    public static async Task<IPAddress?> ResolveAddressAsync(string address)
    {
        var hostEntry = await Dns.GetHostEntryAsync(address);
        return hostEntry.AddressList.FirstOrDefault(a =>
            a.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6);
    }
}