using System;

namespace OutfitStudio.Services
{
    internal static class ScheduleDebug
    {
        internal static Action<string>? TraceLog;
        internal static Action<string>? DebugLog;

        internal static void Trace(string message) => TraceLog?.Invoke(message);
        internal static void Debug(string message) => DebugLog?.Invoke(message);
    }
}
