using GSBC.ChurchApp.Platform.Client.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GSBC.ChurchApp.Features.Analytics.Client;

internal sealed class ErrorLoggerProvider(IServiceProvider services) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ErrorLogger(services);

    public void Dispose()
    {
    }

    private sealed class ErrorLogger(IServiceProvider services) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel) && exception is not null and not OperationCanceledException)
                services.GetService<IClientDiagnostics>()?.ExceptionThrown(exception);
        }
    }
}
