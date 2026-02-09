using System;
using System.IO;

namespace OutfitStudio.Tests.Helpers
{
    internal static class SourceScanner
    {
        private static readonly Lazy<string> sourceRoot = new(() =>
        {
            string? dir = AppContext.BaseDirectory;
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "OutfitStudio", "UI");
                if (Directory.Exists(candidate))
                    return Path.Combine(dir, "OutfitStudio");
                dir = Path.GetDirectoryName(dir);
            }
            throw new InvalidOperationException(
                "Could not find OutfitStudio source directory. " +
                "Ensure the test project is within the same repo as the OutfitStudio project.");
        });

        public static string SourceRoot => sourceRoot.Value;

        public static string ReadSourceFile(string relativePath)
        {
            string fullPath = Path.Combine(SourceRoot, relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Source file not found: {fullPath}");
            return File.ReadAllText(fullPath);
        }

        /// <summary>
        /// Extracts the body of a method (including braces) by finding the method signature
        /// and matching braces. Works for well-structured C# code.
        /// </summary>
        public static string ExtractMethodBody(string source, string methodSignature)
        {
            int sigIndex = source.IndexOf(methodSignature, StringComparison.Ordinal);
            if (sigIndex == -1)
                return "";

            int braceStart = source.IndexOf('{', sigIndex);
            if (braceStart == -1)
                return "";

            int depth = 0;
            for (int i = braceStart; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}') depth--;
                if (depth == 0)
                    return source.Substring(braceStart, i - braceStart + 1);
            }

            return "";
        }

        /// <summary>
        /// Checks whether a method body contains a specific pattern.
        /// </summary>
        public static bool MethodContains(string source, string methodSignature, string pattern)
        {
            string body = ExtractMethodBody(source, methodSignature);
            return body.Contains(pattern, StringComparison.Ordinal);
        }
    }
}
