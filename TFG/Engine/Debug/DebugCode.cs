using System;
using System.Diagnostics;

namespace Engine.Debug
{
    public static class DebugCode
    {
        [Conditional("DEBUG")]
        public static void Execute(Action action)
        {
            action();
        }

        [Conditional("DEBUG")]
        public static void ExecuteIf(bool condition, Action action)
        {
            if(condition)
                action();
        }
    }
}
