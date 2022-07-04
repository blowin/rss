using System.ComponentModel;
using RssDetect.Domain;
using Spectre.Console;
using Spectre.Console.Cli;

namespace RssDetect.CLI;

public sealed class AppCommand : AsyncCommand<AppCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to detect")]
        [CommandOption("-p|--path")]
        public string DetectPath { get; init; } = string.Empty;

        [CommandOption("-r|--rss_path")]
        public string[] RssPath { get; init; } = new[]
        {
            "index.xml",
            "index.json",
            "rss.xml",
            "rss.json",
            "feed"
        };

        [CommandOption("-u|--user_agent")]
        [DefaultValue("Dotnet")]
        public string UserAgent { get; init; } = string.Empty;

        [CommandOption("-t|--timeout")]
        [DefaultValue(typeof(TimeSpan), "00:00:02")]
        public TimeSpan Timeout { get; init; }

        public RssConfiguration ToRssConfiguration()
        {
            return new RssConfiguration(RssPath, Timeout, UserAgent);
        }
    }
    
    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        if(string.IsNullOrEmpty(settings.DetectPath))
            return ValidationResult.Error("\"-p|--path\" can't be empty. Specify path to detect.");
        
        return base.Validate(context, settings);
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var result = await AnsiConsole.Progress()
            .AutoClear(true)
            .AutoRefresh(true)
            .StartAsync(progressContext =>
            {
                var task = progressContext.AddTask("[green]Detect[/]");

                var progress = new Progress<DetectProgress>(detectProgress =>
                {
                    detectProgress.Match(
                        startDetectProgress =>
                        {
                            task.MaxValue = startDetectProgress.MaxOperation;
                            progressContext.Refresh();
                        },
                        increaseDetectProgress => task.Increment(1),
                        finishDetectProgress => {}
                    );
                });

                using var log = new LoggerFactory().Create();
                var rss = new Rss(progress, settings.ToRssConfiguration(), log);
                return rss.DetectAsync(new Uri(settings.DetectPath));
            });

        DisplayResult(result);
        return 0;
    }

    public void DisplayResult(ICollection<RssLink> result)
    {
        if (result.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Can\'t detect any rss path[/]");
            return;
        }
            
        var table = new Table();
        table.AddColumn(new TableColumn("[blue]Link[/]"));
        foreach (var rssLink in result)
            table.AddRow(rssLink.Link);

        AnsiConsole.Write(table);
    }
}