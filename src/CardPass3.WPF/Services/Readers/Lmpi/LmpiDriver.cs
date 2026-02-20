using Microsoft.Extensions.Logging;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace CardPass3.WPF.Services.Readers.Lmpi;

/// <summary>
/// Gestiona la conexión TCP con una Raspberry Pi y el lector LGM7720 asociado.
///
/// Mejoras respecto al sistema original:
///   - Separación clara entre transporte TCP, protocolo y ciclo de vida
///   - Reconexión con exponential backoff + jitter (evita tormentas de SYN)
///   - Canal asíncrono (Channel) para escrituras thread-safe al socket
///   - CancellationToken en todos los puntos de espera
///   - Sin async void — todos los handlers son síncronos o lanzan Task sin await perdido
/// </summary>
public sealed class LmpiDriver : IAsyncDisposable
{
    // ── Identificación ────────────────────────────────────────────────────────
    public string  Ip         { get; }
    public int     Port       { get; }
    public string  UniqueName { get; }

    // ── Estado observable ─────────────────────────────────────────────────────
    public TcpState    State       { get; private set; } = TcpState.Disconnected;
    public AppState    AppState    { get; private set; } = AppState.Control;
    public ReaderState ReaderState { get; private set; } = ReaderState.Control;

    public bool IsTcpConnected    => State == TcpState.TcpConnected || State == TcpState.ReaderConnected;
    public bool IsReaderConnected => State == TcpState.ReaderConnected;

    // ── Eventos hacia el servicio ─────────────────────────────────────────────
    public event Action<LmpiDriver, TcpState>    ConnectionStateChanged = delegate { };
    public event Action<LmpiDriver, LmpiEvent>   EventReceived          = delegate { };
    public event Action<LmpiDriver, LmpiCapacity> CapacityReceived      = delegate { };
    public event Action<LmpiDriver, AppState>    AppStateChanged        = delegate { };
    public event Action<LmpiDriver, ReaderState> ReaderStateChanged     = delegate { };

    // ── Internals ─────────────────────────────────────────────────────────────
    private readonly ILogger<LmpiDriver> _logger;
    private readonly TimeSpan _retryInterval;

    private TcpClient?    _tcp;
    private NetworkStream? _stream;

    // Canal FIFO para escrituras al socket — evita race conditions entre hilos
    private readonly Channel<byte[]>       _writeChannel = Channel.CreateBounded<byte[]>(64);
    private readonly CancellationTokenSource _cts         = new();

    // Para cancelar el loop de ConnectReader periódico al desconectar
    private CancellationTokenSource? _connectReaderCts;

    private const int MaxBackoffSeconds = 300; // techo de 5 min entre reintentos

    public LmpiDriver(string ip, int port, string uniqueName,
                       TimeSpan retryInterval,
                       ILogger<LmpiDriver> logger)
    {
        Ip            = ip;
        Port          = port;
        UniqueName    = uniqueName;
        _retryInterval = retryInterval;
        _logger        = logger;
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>
    /// Inicia el ciclo de conexión en background. Retorna inmediatamente.
    /// Los cambios de estado se notifican vía ConnectionStateChanged.
    /// </summary>
    public void StartConnect()
    {
        _ = RunConnectionLoopAsync(_cts.Token);
        _ = RunWriteLoopAsync(_cts.Token);
    }

    public void Disconnect()
    {
        _cts.Cancel();
        CleanupSocket();
        SetState(TcpState.Disconnected);
    }

    // Comandos — solo disponibles según estado
    public void Sync()             => SendIfConnected(OutgoingCommand.Sync, "");
    public void Emergency()        => SendIfConnected(OutgoingCommand.Emergency, "");
    public void EmergencyEnd()     => SendIfConnected(OutgoingCommand.EmergencyEnd, "");
    public void OpenOnce()         => SendIfReaderConnected(OutgoingCommand.OpenOnceReader, UniqueName);
    public void Restart()          => SendIfReaderConnected(OutgoingCommand.RestartReader, UniqueName);
    public void EmergencyReader()  => SendIfReaderConnected(OutgoingCommand.EmergencyReader, UniqueName);
    public void EmergencyEndReader()=> SendIfReaderConnected(OutgoingCommand.EmergencyEndReader, UniqueName);
    public void Quit()             => SendIfConnected(OutgoingCommand.Quit, "");
    public void Reboot()           => SendIfConnected(OutgoingCommand.Reboot, "");
    public void PowerOff()         => SendIfConnected(OutgoingCommand.Poweroff, "");
    public void ResetUsb()         => SendIfConnected(OutgoingCommand.ResetUsb, "");
    public void FakeScan(string card) => SendIfConnected(OutgoingCommand.FakeScan, card);

    public async ValueTask DisposeAsync()
    {
        Disconnect();
        await Task.CompletedTask;
        _cts.Dispose();
        _connectReaderCts?.Dispose();
    }

    // ── Ciclo de conexión ─────────────────────────────────────────────────────

    private async Task RunConnectionLoopAsync(CancellationToken ct)
    {
        int attempt = 0;

        while (!ct.IsCancellationRequested)
        {
            SetState(TcpState.Connecting);
            attempt++;

            try
            {
                _tcp    = new TcpClient { NoDelay = true };
                await _tcp.ConnectAsync(Ip, Port, ct);
                _stream = _tcp.GetStream();

                _logger.LogInformation("TCP connected to {Ip}:{Port} (attempt {N})", Ip, Port, attempt);
                attempt = 0; // reset backoff on success

                SetState(TcpState.TcpConnected);
                StartConnectReaderLoop(ct);

                // Bucle de lectura — bloquea hasta error o cancelación
                await RunReadLoopAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Connection to {Ip}:{Port} failed: {Msg}", Ip, Port, ex.Message);
            }

            _connectReaderCts?.Cancel();
            CleanupSocket();
            SetState(TcpState.Disconnected);

            if (ct.IsCancellationRequested) break;

            // Exponential backoff con jitter: 2^attempt * base, techo 5 min
            var backoff = TimeSpan.FromSeconds(
                Math.Min(MaxBackoffSeconds,
                         _retryInterval.TotalSeconds * Math.Pow(1.5, Math.Min(attempt - 1, 10)))
                + Random.Shared.NextDouble() * 2);

            _logger.LogDebug("Retry {Ip}:{Port} in {Sec:F1}s", Ip, Port, backoff.TotalSeconds);
            await Task.Delay(backoff, ct).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Una vez TCP conectado, envía ConnectReader periódicamente hasta confirmación.
    /// El original usaba async void en un event handler — aquí es un Task controlado.
    /// </summary>
    private void StartConnectReaderLoop(CancellationToken parentCt)
    {
        _connectReaderCts?.Cancel();
        _connectReaderCts = CancellationTokenSource.CreateLinkedTokenSource(parentCt);
        var ct = _connectReaderCts.Token;

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested && !IsReaderConnected)
            {
                _logger.LogDebug("Sending ConnectReader for '{Name}'", UniqueName);
                EnqueueCommand(OutgoingCommand.ConnectReader, UniqueName);
                try { await Task.Delay(TimeSpan.FromSeconds(60), ct); }
                catch (OperationCanceledException) { return; }
            }
        }, ct);
    }

    // ── Bucle de lectura del stream ───────────────────────────────────────────

    private async Task RunReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var msgType = await ReadIncomingMessageTypeAsync(ct);

                switch (msgType)
                {
                    case IncomingMessage.Connected:
                        await HandleConnectedAsync(ct);
                        break;
                    case IncomingMessage.Event:
                        await HandleEventAsync(ct);
                        break;
                    case IncomingMessage.LogMessage:
                        await HandleLogMessageAsync(ct);
                        break;
                    case IncomingMessage.Capacity:
                        await HandleCapacityAsync(ct);
                        break;
                    case IncomingMessage.AppState:
                        await HandleAppStateAsync(ct);
                        break;
                    case IncomingMessage.ReaderState:
                        await HandleReaderStateAsync(ct);
                        break;
                    default:
                        _logger.LogWarning("Unknown message type {T} from {Ip}", (int)msgType, Ip);
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { /* normal */ }
        catch (IOException ex)
        {
            _logger.LogWarning("Socket read error from {Ip}:{Port}: {Msg}", Ip, Port, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reading from {Ip}:{Port}", Ip, Port);
        }
    }

    // ── Handlers de mensajes entrantes ────────────────────────────────────────

    private async Task HandleConnectedAsync(CancellationToken ct)
    {
        var uniqueName = await ReadStringAsync(ct);
        if (uniqueName != UniqueName) return;

        _logger.LogInformation("Reader '{Name}' connected at {Ip}:{Port}", UniqueName, Ip, Port);
        _connectReaderCts?.Cancel(); // ya conectado, parar el loop
        SetState(TcpState.ReaderConnected);
    }

    private async Task HandleEventAsync(CancellationToken ct)
    {
        var uniqueName   = await ReadStringAsync(ct);
        var userId       = await ReadInt32Async(ct);
        var incidence    = await ReadStringAsync(ct);
        var readerId     = await ReadInt32Async(ct);
        var datetimeUtc  = await ReadStringAsync(ct);
        var datetimeLocal= await ReadStringAsync(ct);

        if (uniqueName != UniqueName) return;

        var ev = new LmpiEvent
        {
            UserId        = userId,
            Incidence     = incidence,
            ReaderId      = readerId,
            DatetimeUtc   = datetimeUtc,
            DatetimeLocal = datetimeLocal
        };

        _logger.LogDebug("Event from '{Name}': {Ev}", UniqueName, ev);

        try { EventReceived(this, ev); }
        catch (Exception ex) { _logger.LogError(ex, "Error in EventReceived handler"); }
    }

    private async Task HandleLogMessageAsync(CancellationToken ct)
    {
        var level = await ReadInt32Async(ct);
        var msg   = await ReadStringAsync(ct);
        _logger.LogDebug("[Pi:{Ip}] level={Level} {Msg}", Ip, level, msg);
    }

    private async Task HandleCapacityAsync(CancellationToken ct)
    {
        var current = await ReadInt32Async(ct);
        var maximum = await ReadInt32Async(ct);
        var cap = new LmpiCapacity { Current = current, Maximum = maximum };
        try { CapacityReceived(this, cap); }
        catch (Exception ex) { _logger.LogError(ex, "Error in CapacityReceived handler"); }
    }

    private async Task HandleAppStateAsync(CancellationToken ct)
    {
        var raw   = await ReadInt32Async(ct);
        var state = raw == 0 ? AppState.Control : AppState.Emergency;
        AppState  = state;
        try { AppStateChanged(this, state); }
        catch (Exception ex) { _logger.LogError(ex, "Error in AppStateChanged handler"); }
    }

    private async Task HandleReaderStateAsync(CancellationToken ct)
    {
        var uniqueName = await ReadStringAsync(ct);
        var raw        = await ReadInt32Async(ct);
        var state      = raw == 0 ? ReaderState.Control : ReaderState.Emergency;

        if (uniqueName != UniqueName) return;

        ReaderState = state;
        try { ReaderStateChanged(this, state); }
        catch (Exception ex) { _logger.LogError(ex, "Error in ReaderStateChanged handler"); }
    }

    // ── Protocolo binario — lectura ───────────────────────────────────────────

    private async Task<IncomingMessage> ReadIncomingMessageTypeAsync(CancellationToken ct)
    {
        var value = await ReadInt32Async(ct);
        return (IncomingMessage)value;
    }

    private async Task<int> ReadInt32Async(CancellationToken ct)
    {
        var buf = await ReadBytesAsync(4, ct);
        if (BitConverter.IsLittleEndian) Array.Reverse(buf);
        return BitConverter.ToInt32(buf, 0);
    }

    private async Task<string> ReadStringAsync(CancellationToken ct)
    {
        var length = await ReadInt32Async(ct);
        var buf    = await ReadBytesAsync(length, ct);
        // Protocol strings are null-terminated
        return Encoding.UTF8.GetString(buf, 0, Math.Max(0, length - 1));
    }

    private async Task<byte[]> ReadBytesAsync(int count, CancellationToken ct)
    {
        var buf     = new byte[count];
        var received = 0;

        while (received < count)
        {
            var n = await _stream!.ReadAsync(buf.AsMemory(received, count - received), ct);
            if (n == 0) throw new IOException("Connection closed by remote host.");
            received += n;
        }

        return buf;
    }

    // ── Protocolo binario — escritura ─────────────────────────────────────────

    /// <summary>
    /// Bucle de escritura separado — consume el Channel y escribe al socket
    /// secuencialmente, eliminando race conditions de múltiples hilos escribiendo.
    /// </summary>
    private async Task RunWriteLoopAsync(CancellationToken ct)
    {
        await foreach (var payload in _writeChannel.Reader.ReadAllAsync(ct))
        {
            try
            {
                if (_stream is { CanWrite: true })
                    await _stream.WriteAsync(payload, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Write error to {Ip}:{Port}: {Msg}", Ip, Port, ex.Message);
            }
        }
    }

    private void EnqueueCommand(OutgoingCommand cmd, string param)
    {
        var payload = BuildCommandPayload(cmd, param);
        _writeChannel.Writer.TryWrite(payload); // non-blocking; drops if full (backpressure)
    }

    private static byte[] BuildCommandPayload(OutgoingCommand cmd, string param)
    {
        var cmdStr  = cmd.ToString().ToLowerInvariant() + param;
        var strBytes = Encoding.UTF8.GetBytes(cmdStr);
        var lenBytes = BitConverter.GetBytes(strBytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);

        var result = new byte[4 + strBytes.Length];
        lenBytes.CopyTo(result, 0);
        strBytes.CopyTo(result, 4);
        return result;
    }

    private void SendIfConnected(OutgoingCommand cmd, string param)
    {
        if (!IsTcpConnected)
        {
            _logger.LogWarning("Cannot send '{Cmd}' — not TCP connected ({Ip}:{Port})", cmd, Ip, Port);
            return;
        }
        EnqueueCommand(cmd, param);
    }

    private void SendIfReaderConnected(OutgoingCommand cmd, string param)
    {
        if (!IsReaderConnected)
        {
            _logger.LogWarning("Cannot send '{Cmd}' — reader not connected ({Ip}:{Port})", cmd, Ip, Port);
            return;
        }
        EnqueueCommand(cmd, param);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetState(TcpState newState)
    {
        if (State == newState) return;
        State = newState;
        try { ConnectionStateChanged(this, newState); }
        catch (Exception ex) { _logger.LogError(ex, "Error in ConnectionStateChanged handler"); }
    }

    private void CleanupSocket()
    {
        try { _stream?.Close(); } catch { /* ignored */ }
        try { _tcp?.Close();    } catch { /* ignored */ }
        _stream = null;
        _tcp    = null;
    }
}
