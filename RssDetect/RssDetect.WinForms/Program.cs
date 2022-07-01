using System.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Authentication;

namespace RssDetect.WinForms;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        var appMessageBox = new AppMessageBox();
        Application.ThreadException += (sender, args) =>
        {
            var ex = Trim(args.Exception?.ToString(), 2048) ?? "Unhandled exception";
            appMessageBox.ShowError(ex.ToString());
        };

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(appMessageBox));
    }

    private static string Trim(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length > maxLen ? value[..maxLen] : value;
    }
}