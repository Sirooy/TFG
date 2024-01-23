using System;
using System.Diagnostics;

namespace Engine.Debug
{
    public static class DebugCode
    {
        public const string DEFINE = "DEBUG";

        [Conditional(DEFINE)]
        public static void Execute(Action action)
        {
            action();
        }

        [Conditional(DEFINE)]
        public static void ExecuteIf(bool condition, Action action)
        {
            if(condition)
                action();
        }
    }
}
