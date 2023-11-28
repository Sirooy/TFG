using System;
using System.Diagnostics;

namespace Engine.Debug
{
    public static class DebugLog
    {
        [Conditional("DEBUG")]
        public static void LogInfo(string message, params object[] args)
        {
            WriteLogHeader("INFO", ConsoleColor.Blue,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void LogWarning(string message, params object[] args)
        {
            WriteLogHeader("WARNING", ConsoleColor.DarkYellow,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void LogSuccess(string message, params object[] args)
        {
            WriteLogHeader("SUCCESS", ConsoleColor.Green,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void LogFail(string message, params object[] args)
        {
            WriteLogHeader("FAIL", ConsoleColor.Red,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void LogInfoIf(bool condition, string message, params object[] args)
        {
            if (condition) LogInfo(message, args);
        }

        [Conditional("DEBUG")]
        public static void LogWarningIf(bool condition, string message, params object[] args)
        {
            if (condition) LogWarning(message, args);
        }

        [Conditional("DEBUG")]
        public static void LogSuccessIf(bool condition, string message, params object[] args)
        {
            if (condition) LogSuccess(message, args);
        }

        [Conditional("DEBUG")]
        public static void LogFailIf(bool condition, string message, params object[] args)
        {
            if (condition) LogFail(message, args);
        }

        internal static void WriteLogHeader(string messageType, ConsoleColor back,
            ConsoleColor fore)
        {
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
            Console.Write(string.Format("[{0}]", messageType));
            Console.ResetColor();
            Console.Write(" ");
        }
    }
}
