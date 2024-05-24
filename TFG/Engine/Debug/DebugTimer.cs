using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Engine.Debug
{
    public static class DebugTimer
    {
        public const string DEFINE = "DEBUG1";

        private class TimerData
        {
            public Stopwatch Timer;
            public double CurrentUpdateTime;
            public string LastAverageTime;
            public int MaxSamples;
            public int NumSamples;
            public bool IsActive;
        }

        private static Dictionary<string, TimerData> timers =
            new Dictionary<string, TimerData>();

        [Conditional(DEFINE)]
        public static void Register(string name, int maxSamples)
        {
            timers.Add(name, new TimerData()
            {
                Timer = new Stopwatch(),
                CurrentUpdateTime = 0.0d,
                LastAverageTime = name + ": 0",
                MaxSamples = Math.Max(1, maxSamples),
                NumSamples = 1,
                IsActive   = false
            });
        }

        [Conditional(DEFINE)]
        public static void Start(string name)
        {
            TimerData data = timers[name];
            data.IsActive = true;
            data.Timer.Restart();
        }

        [Conditional(DEFINE)]
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

        [Conditional(DEFINE)]
        public static void Draw(SpriteBatch spriteBatch, SpriteFont font, float scale = 0.5f)
        {
            Vector2 position = Vector2.Zero;

            spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
            foreach (var timer in timers.Values)
            {
                if (!timer.IsActive) continue;
                timer.IsActive = false;

                Vector2 stringSize = font.MeasureString(timer.LastAverageTime) * scale;

                spriteBatch.DrawString(font, timer.LastAverageTime,
                    position, Color.White, 0.0f, Vector2.Zero, scale, 
                    SpriteEffects.None, 0.0f);

                position.Y += stringSize.Y;

            }
            spriteBatch.End();
        }
    }
}
