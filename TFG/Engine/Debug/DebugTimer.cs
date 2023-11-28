using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Debug
{
    public static class DebugTimer
    {
        private class TimerData
        {
            public System.Diagnostics.Stopwatch Timer;
            public double CurrentUpdateTime;
            public string LastAverageTime;
            public int MaxSamples;
            public int NumSamples;
        }

        private static Dictionary<string, TimerData> timers =
            new Dictionary<string, TimerData>();

        //[System.Diagnostics.Conditional("DEBUG")]
        public static void Register(string name, int maxSamples)
        {
            timers.Add(name, new TimerData()
            {
                Timer = new System.Diagnostics.Stopwatch(),
                CurrentUpdateTime = 0.0d,
                LastAverageTime = name + ": 0",
                MaxSamples = Math.Max(1, maxSamples),
                NumSamples = 1
            });
        }

        //[System.Diagnostics.Conditional("DEBUG")]
        public static void Start(string name)
        {
            TimerData data = timers[name];
            data.Timer.Restart();
        }

        //[System.Diagnostics.Conditional("DEBUG")]
        public static void Stop(string name)
        {
            TimerData data = timers[name];
            data.Timer.Stop();
            data.NumSamples++;
            data.CurrentUpdateTime += data.Timer.Elapsed.TotalMilliseconds;

            if (data.NumSamples == data.MaxSamples)
            {
                double elapsed = data.CurrentUpdateTime / data.MaxSamples;
                data.LastAverageTime = name + ": " + Math.Round(elapsed, 4).ToString();
                data.CurrentUpdateTime = 0.0d;
                data.NumSamples = 0;
            }
        }

        //[System.Diagnostics.Conditional("DEBUG")]
        public static void Draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            Vector2 position = Vector2.Zero;

            spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            foreach (var timer in timers.Values)
            {
                Vector2 stringSize = font.MeasureString(timer.LastAverageTime);

                spriteBatch.DrawString(font, timer.LastAverageTime,
                    position, Color.White);

                position.Y += stringSize.Y;
            }
            spriteBatch.End();
        }
    }
}
