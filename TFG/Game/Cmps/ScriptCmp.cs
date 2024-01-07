using System;
using Core;

namespace Cmps
{
    public class ScriptCmp
    {
        public Action<Entity> Execute;

        public ScriptCmp(Action<Entity> execute)
        {
            Execute = execute;
        }
    }
}
