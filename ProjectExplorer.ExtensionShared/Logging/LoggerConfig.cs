#region Using Directives

using System;
using System.IO;

using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

#endregion

namespace IInspectable.ProjectExplorer.Extension;

static class LoggerConfig {

    // ReSharper disable InconsistentNaming
    const           long   KB        = 1024;
    const           long   MB        = 1024 * KB;
    const           string LogName   = "ProjectExplorer.Extension";
    static readonly string LogFolder = Path.GetTempPath();
    // ReSharper restore InconsistentNaming

    public static NLog.Logger GetLogger(Type type) {
        return LogFactory.GetLogger(type.FullName);
    }

    static readonly LogFactory LogFactory = CreateLogFactory();

    static LogFactory CreateLogFactory() {
        var logFactory = new LogFactory();
        var config     = new LoggingConfiguration(logFactory);

        var fileTarget = new FileTarget {
            FileName         = Path.Combine(LogFolder, $"{LogName}.log.xml"),
            ArchiveFileName  = Path.Combine(LogFolder, $"{LogName}.log.xml.{{#####}}"),
            ArchiveNumbering = ArchiveNumberingMode.Rolling,
            MaxArchiveFiles  = 3,
            ArchiveAboveSize = 10 * MB,
            Layout           = new Log4JXmlEventLayout()
        };

        config.AddTarget("file", fileTarget);
        config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, fileTarget));

        // **WICHTIG: Konfiguration aktivieren**
        logFactory.Configuration = config;

        return logFactory;
    }

}