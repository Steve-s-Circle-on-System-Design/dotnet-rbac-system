using Microsoft.Extensions.Logging;

namespace RbacSystem.Tests.Fakes;

/// <summary>
/// Captures rendered log messages so tests can assert on what actually reaches the
/// log — in particular that personal data is masked.
/// </summary>
/// <typeparam name="TCategory">Logger category.</typeparam>
internal sealed class CapturingLogger<TCategory> : ILogger<TCategory>
{
    /// <summary>Rendered messages, in order.</summary>
    public List<string> Messages { get; } = [];

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        Messages.Add(formatter(state, exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
            // Nothing to release.
        }
    }
}
