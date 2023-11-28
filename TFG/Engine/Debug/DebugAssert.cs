using System;
using System.Diagnostics;

namespace Engine.Debug
{
    public static class DebugAssert
    {
        [Conditional("DEBUG")]
        //Forces the debugger to step out of the
        //function when the exception is thrown
        [DebuggerNonUserCode()]
        public static void Success(bool condition, string message = "", params object[] args)
        {
            if (condition) return;

            DebugLog.WriteLogHeader("ASSERTION FAILED", ConsoleColor.DarkBlue,
                ConsoleColor.Red);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message, args);
            Console.ResetColor();

            throw new ApplicationException("Assert Error");
        }

        public static void Fail(bool condition, string message = "", params object[] args)
        {
            if (!condition) return;

            DebugLog.WriteLogHeader("ASSERTION FAILED", ConsoleColor.DarkBlue,
                ConsoleColor.Red);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message, args);
            Console.ResetColor();

            throw new ApplicationException("Assert Error");
        }

        [Conditional("DEBUG")]
        [DebuggerNonUserCode()]
        public static void ThrowError(string message = "", params object[] args)
        {
            DebugLog.WriteLogHeader("ERROR", ConsoleColor.DarkMagenta,
                ConsoleColor.Yellow);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message, args);
            Console.ResetColor();

            throw new ApplicationException("Error");
        }
    }
}
