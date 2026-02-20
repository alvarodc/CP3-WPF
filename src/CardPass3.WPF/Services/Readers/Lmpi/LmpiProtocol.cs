namespace CardPass3.WPF.Services.Readers.Lmpi;

// ─── Connection states ────────────────────────────────────────────────────────

/// <summary>
/// Estado de la conexión TCP con la Raspberry Pi.
/// Note: TCPConnected ≠ ReaderConnected — la Pi puede estar accesible
/// pero el lector LGM7720 aún no estar activo.
/// </summary>
public enum TcpState
{
    Disconnected,
    Connecting,
    TcpConnected,      // TCP OK, esperando confirmación del lector
    ReaderConnected,   // Lector LGM7720 operativo — estado funcional completo
    Disconnecting
}

public enum AppState  { Control, Emergency }
public enum ReaderState { Control, Emergency }

// ─── Incoming message types (protocol framing) ────────────────────────────────

internal enum IncomingMessage
{
    Connected    = 0,
    Event        = 1,
    LogMessage   = 2,
    Capacity     = 3,
    AppState     = 4,
    ReaderState  = 5
}

// ─── Outgoing commands ────────────────────────────────────────────────────────

internal enum OutgoingCommand
{
    Sync,
    Emergency,
    EmergencyEnd,
    ConnectReader,
    OpenOnceReader,
    RestartReader,
    EmergencyReader,
    EmergencyEndReader,
    Quit,
    Reboot,
    Poweroff,
    ResetUsb,
    FakeScan,
    Crash
}

// ─── Event data ───────────────────────────────────────────────────────────────

/// <summary>
/// Fichaje / evento recibido desde el lector en tiempo real.
/// </summary>
public sealed class LmpiEvent
{
    public required int    UserId        { get; init; }
    public required string Incidence     { get; init; }
    public required int    ReaderId      { get; init; }
    public required string DatetimeUtc   { get; init; }
    public required string DatetimeLocal { get; init; }

    public override string ToString()
        => $"userId={UserId} incidence={Incidence} readerId={ReaderId} utc={DatetimeUtc}";
}

/// <summary>
/// Información de capacidad del lector (usuarios sincronizados, máximo).
/// </summary>
public sealed class LmpiCapacity
{
    public required int Current { get; init; }
    public required int Maximum { get; init; }
}
