using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CardPass3.WPF.Services.Readers.Lmpi;

/// <summary>
/// Cliente UDP para comandos de emergencia broadcast a toda la red.
/// Opera independientemente de las conexiones TCP individuales —
/// un solo broadcast llega a todas las Raspberries del segmento de red.
/// </summary>
public sealed class LmpiUdpClient : IDisposable
{
    private readonly ILogger<LmpiUdpClient> _logger;
    private readonly IPEndPoint _broadcastEndpoint;
    private UdpClient? _udp;
    private bool _disposed;

    public LmpiUdpClient(string networkCidr, int port, ILogger<LmpiUdpClient> logger)
    {
        _logger = logger;
        var broadcastIp = GetBroadcastAddress(networkCidr);
        _broadcastEndpoint = new IPEndPoint(broadcastIp, port);
        CreateClient();
    }

    public void Emergency()    => Send("emergency");
    public void EmergencyEnd() => Send("emergency_end");
    public void Sync()         => Send("sync");

    private void Send(string command)
    {
        if (_udp is null) return;

        try
        {
            var payload = Encoding.UTF8.GetBytes(command);
            _udp.Send(payload, payload.Length, _broadcastEndpoint);
            _logger.LogDebug("UDP broadcast '{Command}' → {Endpoint}", command, _broadcastEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("UDP send '{Command}' failed: {Msg}", command, ex.Message);
        }
    }

    private void CreateClient()
    {
        try
        {
            _udp = new UdpClient();
            _udp.EnableBroadcast = true;
            _logger.LogInformation("UDP client ready → {Endpoint}", _broadcastEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create UDP client for {Endpoint}", _broadcastEndpoint);
        }
    }

    /// <summary>
    /// Calcula la dirección de broadcast a partir de un CIDR (e.g. "192.168.1.0/24").
    /// </summary>
    private static IPAddress GetBroadcastAddress(string cidr)
    {
        try
        {
            var parts   = cidr.Split('/');
            var ip      = IPAddress.Parse(parts[0]);
            var prefix  = parts.Length > 1 ? int.Parse(parts[1]) : 24;
            var ipBytes = ip.GetAddressBytes();
            var mask    = prefix == 0 ? 0 : ~((1u << (32 - prefix)) - 1);
            var network = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0) & mask;
            var bcast   = network | ~mask;
            var bcastBytes = BitConverter.GetBytes(bcast).Reverse().ToArray();
            return new IPAddress(bcastBytes);
        }
        catch
        {
            return IPAddress.Broadcast;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _udp?.Dispose();
    }
}
