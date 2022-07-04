// See https://aka.ms/new-console-template for more information

using Spectre.Console;
using Spectre.Console.Cli;

namespace RssDetect.CLI
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var app = new CommandApp<AppCommand>();
            
            try
            {
                return await app.RunAsync(args);
            }
            catch (Exception e)
            {
                AnsiConsole.WriteException(e, ExceptionFormats.ShortenEverything);
                return -20;
            }
        }
    }
}