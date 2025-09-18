using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Events;
using McpClientDemo.Configuration;

namespace McpClientDemo.Services;

public static class LoggingService
{
    public static ILoggerFactory CreateLoggerFactory(LoggingConfig config)
    {
        var loggerConfiguration = new LoggerConfiguration();

        // Set minimum log level
        var logLevel = Enum.Parse<LogEventLevel>(config.LogLevel, true);
        loggerConfiguration.MinimumLevel.Is(logLevel);

        // Configure console logging
        if (config.EnableConsoleLogging)
        {
            if (config.EnableStructuredLogging)
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                loggerConfiguration.WriteTo.Console();
            }
        }

        // Configure file logging
        if (config.EnableFileLogging)
        {
            var logDirectory = Path.GetDirectoryName(config.LogFilePath);
            if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            if (config.EnableStructuredLogging)
            {
                loggerConfiguration.WriteTo.File(
                    path: config.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
            }
            else
            {
                loggerConfiguration.WriteTo.File(config.LogFilePath);
            }
        }

        // Enrich with context
        loggerConfiguration
            .Enrich.FromLogContext();

        Log.Logger = loggerConfiguration.CreateLogger();

        return LoggerFactory.Create(builder =>
        {
            builder.AddSerilog();

            if (config.EnableConsoleLogging)
            {
                builder.AddConsole(options =>
                {
                    options.FormatterName = ConsoleFormatterNames.Simple;
                });
            }
        });
    }

    public static void ConfigureGlobalExceptionHandling(Microsoft.Extensions.Logging.ILogger logger)
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            logger.LogCritical(e.ExceptionObject as Exception, "Unhandled exception occurred");
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            logger.LogError(e.Exception, "Unobserved task exception occurred");
            e.SetObserved();
        };
    }
}