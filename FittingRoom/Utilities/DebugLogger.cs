using StardewModdingAPI;

namespace FittingRoom
{
    /// <summary>
    /// Centralized debug logging wrapper that respects ModConfig.EnableDebugLogging.
    /// Ensures Warn/Error logs always appear, while Debug/Trace respect config.
    /// Enable debug logging by setting "EnableDebugLogging": true in config.json.
    /// </summary>
    public static class DebugLogger
    {
        private static IMonitor? monitor;
        private static ModConfig? config;

        /// <summary>
        /// Initialize the debug logger with monitor and config.
        /// Call this once in ModEntry.Entry() after config is loaded.
        /// </summary>
        public static void Initialize(IMonitor logMonitor, ModConfig logConfig)
        {
            monitor = logMonitor;
            config = logConfig;
        }

        /// <summary>
        /// Log a message with debug filtering.
        /// Warn/Error/Alert always appear. Debug/Trace/Info respect EnableDebugLogging setting.
        /// </summary>
        public static void Log(string message, LogLevel level)
        {
            if (monitor == null) return;

            // Always log Warn/Error/Alert
            if (level >= LogLevel.Warn)
            {
                monitor.Log(message, level);
                return;
            }

            // For Debug/Trace/Info, check config
            if (config?.EnableDebugLogging == true)
            {
                monitor.Log(message, level);
            }
        }
    }
}
