namespace DSpace.ControlDesk.Mcp.ControlDesk;

/// <summary>
/// Represents a failure that occurred while driving ControlDesk through its COM
/// automation interface. Carries a machine-parseable <see cref="Code"/> and a
/// <see cref="Transient"/> flag so callers can distinguish retryable problems
/// (e.g. ControlDesk not yet reachable) from permanent ones (e.g. unknown layout).
/// </summary>
public sealed class ControlDeskException : Exception
{
    public ControlDeskException(string code, string message, bool transient, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Transient = transient;
    }

    /// <summary>Stable, machine-parseable error code (snake_case).</summary>
    public string Code { get; }

    /// <summary>True if the operation may succeed on retry; false if it is permanent.</summary>
    public bool Transient { get; }
}
