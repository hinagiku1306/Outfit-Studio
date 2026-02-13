using StardewModdingAPI;

namespace OutfitStudio
{
    public static class DebugLogger
    {
        private static IMonitor? monitor;
        private static ModConfig? config;

        public static void Initialize(IMonitor logMonitor, ModConfig logConfig)
        {
            monitor = logMonitor;
            config = logConfig;
        }

        public static void Log(string message, LogLevel level)
        {
            if (monitor == null) return;

            if (level >= LogLevel.Warn)
            {
                monitor.Log(message, level);
                return;
            }

            if (config?.EnableDebugLogging == true)
            {
                monitor.Log(message, level);
            }
        }

        /// <summary>Log at Trace level (debug-only). Safe to call from static methods tested without SMAPI.</summary>
        public static void Trace(string message)
        {
            if (monitor == null || config?.EnableDebugLogging != true) return;
            LogInternal(message, LogLevel.Trace);
        }

        /// <summary>Log at Debug level (debug-only). Safe to call from static methods tested without SMAPI.</summary>
        public static void Debug(string message)
        {
            if (monitor == null || config?.EnableDebugLogging != true) return;
            LogInternal(message, LogLevel.Debug);
        }

        private static void LogInternal(string message, LogLevel level)
        {
            monitor!.Log(message, level);
        }
    }
}
