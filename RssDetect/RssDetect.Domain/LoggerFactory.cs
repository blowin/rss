using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace RssDetect.Domain;

public class LoggerFactory
{
    private readonly string _path;
    private readonly LogEventLevel _minimumLevel;

    public LoggerFactory(LogEventLevel minimumLevel = LogEventLevel.Warning, string path = "logs/log.txt")
    {
        _minimumLevel = minimumLevel;
        _path = path;
    }

    public Logger Create()
    {
        var dirName = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dirName) && !Directory.Exists(dirName))
            Directory.CreateDirectory(dirName);

        return new LoggerConfiguration()
            .WriteTo.File(_path, _minimumLevel, rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}