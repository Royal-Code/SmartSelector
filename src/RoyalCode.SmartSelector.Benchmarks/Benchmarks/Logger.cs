using Microsoft.Extensions.Logging;

namespace RoyalCode.SmartSelector.Benchmarks.Benchmarks;

internal static class Logger
{
    public static ILoggerFactory CreateLoggerFactory() =>
        LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
}
