using HouseofCat.Utilities.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using S = Serilog; // For debugging call.

namespace HouseofCat.Serilog;

public static class HostExtensions
{
    /// <summary>
    /// Bootstrap Serilog for IHost that enriches with context, basic overrides, minimum level to debug, and enable self-log if in debug mode.
    /// <para>It also receives the Configuration so that you can override based on appsettings.json/Configuration.</para>
    /// </summary>
    /// <param name="host"></param>
    /// <param name="applicationName"></param>
    public static void CreateSerilogLogger(this IHost host, string applicationName)
    {
        using var scope = host.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", applicationName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                theme: AnsiConsoleTheme.Literate)
            // Configure LogLevel defaults with appsettings.json `Serilog` section and `Logging` section.
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        if (App.IsDebug) { S.Debugging.SelfLog.Enable(Console.WriteLine); }
    }
}
