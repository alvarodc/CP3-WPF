using CardPass3.WPF.Data.Models;

namespace CardPass3.WPF.Services.Readers;

/// <summary>
/// Abstraction over the hardware TCP/IP communication library.
/// Each driver type (LPFT3, etc.) implements this interface.
/// This makes it possible to mock hardware in tests and to support multiple device families.
/// </summary>
public interface IReaderDriver
{
    int ReaderId { get; }
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
    Task<bool> PingAsync(CancellationToken ct = default);
    Task OpenRelayAsync(int seconds, CancellationToken ct = default);
    Task RestartAsync(CancellationToken ct = default);

    // Events from device → software
    event EventHandler<ReaderEventArgs>? EventReceived;
}

public class ReaderEventArgs(int readerId, string cardNumber, string incidence) : EventArgs
{
    public int ReaderId { get; } = readerId;
    public string CardNumber { get; } = cardNumber;
    public string Incidence { get; } = incidence;
    public DateTime ReceivedAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Factory that returns the correct driver implementation based on Reader.Driver field.
/// The existing TCP/IP library is wrapped here — centralising all hardware coupling.
/// </summary>
public interface IReaderDriverFactory
{
    IReaderDriver GetDriver(Reader reader);
}

// ─── Placeholder implementation (replace with actual library calls) ──────────

public class DefaultReaderDriverFactory : IReaderDriverFactory
{
    private readonly Dictionary<int, IReaderDriver> _cache = new();

    public IReaderDriver GetDriver(Reader reader)
    {
        if (!_cache.TryGetValue(reader.IdReader, out var driver))
        {
            // TODO: instantiate the correct driver class based on reader.Driver enum value
            // e.g. driver = reader.Driver == 0 ? new Lpft3Driver(reader) : new OtherDriver(reader);
            driver = new StubReaderDriver(reader);
            _cache[reader.IdReader] = driver;
        }
        return driver;
    }
}

/// <summary>
/// Stub driver for development / testing without physical hardware.
/// Simulates a successful connection with a short delay.
/// </summary>
internal class StubReaderDriver(Reader reader) : IReaderDriver
{
    public int ReaderId => reader.IdReader;
    public bool IsConnected { get; private set; }

    public event EventHandler<ReaderEventArgs>? EventReceived;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        // Simulate variable network latency
        var delay = Random.Shared.Next(200, 2000);
        await Task.Delay(delay, ct);

        // Simulate ~80% success rate
        if (Random.Shared.NextDouble() < 0.2)
            throw new TimeoutException($"Stub: connection timeout for {reader.EffectiveIp}:{reader.Port}");

        IsConnected = true;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<bool> PingAsync(CancellationToken ct = default)
        => Task.FromResult(IsConnected);

    public Task OpenRelayAsync(int seconds, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RestartAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
