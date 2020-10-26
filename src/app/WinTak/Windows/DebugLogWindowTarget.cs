using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace STak.WinTak
{
    [Target("DebugLogWindow")]
    public class DebugLogWindowTarget : TargetWithLayout
    {
        [RequiredParameter]
        public LogWindow LogWindow { get; set; }
        public LogLevel  LogLevel  { get; set; } = LogLevel.Off;


        public DebugLogWindowTarget(LogWindow logWindow)
        {
            LogWindow = logWindow;
            this.Name = logWindow.Name;
        }


        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent.Level >= this.LogLevel)
            {
                LogWindow.AppendText(Layout.Render(logEvent));
            }
        }


        public void Register()
        {
            Target.Register(Name, GetType());
            Layout="${longdate} [${processid}] [${threadid}] ${level:uppercase=true}"
                                           + " ${logger:shortName=true} - ${message}\n";
            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, this));
            LogManager.Configuration.AddTarget(Name, this);
            LogManager.ReconfigExistingLoggers();
        }
    }
}
