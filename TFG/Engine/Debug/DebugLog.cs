using System;
using System.Diagnostics;

namespace Engine.Debug
{
    public static class DebugLog
    {
        [Conditional("DEBUG")]
        public static void Info(string message, params object[] args)
        {
            WriteLogHeader("INFO", ConsoleColor.Blue,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void Warning(string message, params object[] args)
        {
            WriteLogHeader("WARNING", ConsoleColor.DarkYellow,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void Success(string message, params object[] args)
        {
            WriteLogHeader("SUCCESS", ConsoleColor.Green,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void Error(string message, params object[] args)
        {
            WriteLogHeader("ERROR", ConsoleColor.Red,
                ConsoleColor.White);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }

        [Conditional("DEBUG")]
        public static void InfoIf(bool condition, string message, params object[] args)
        {
            if (condition) Info(message, args);
        }

        [Conditional("DEBUG")]
        public static void WarningIf(bool condition, string message, params object[] args)
        {
            if (condition) Warning(message, args);
        }

        [Conditional("DEBUG")]
        public static void SuccessIf(bool condition, string message, params object[] args)
        {
            if (condition) Success(message, args);
        }

        [Conditional("DEBUG")]
        public static void ErrorIf(bool condition, string message, params object[] args)
        {
            if (condition) Error(message, args);
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
