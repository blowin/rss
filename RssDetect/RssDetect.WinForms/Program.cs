using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;
using Serilog;
using Serilog.Events;

namespace RssDetect.WinForms;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        if (!Directory.Exists("logs"))
            Directory.CreateDirectory("logs");

        using var log = new LoggerConfiguration()
            .WriteTo.File("logs/log.txt", LogEventLevel.Warning, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var appMessageBox = new AppMessageBox();
        Application.ThreadException += (sender, args) =>
        {
            log.Error(args.Exception, "ThreadException {Sender}", sender?.GetType().Name);
            appMessageBox.ShowError("Unhandled exception");
        };

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(appMessageBox, log));
    }
}