using Engine.Debug;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Core
{
    public struct AnimationFrame
    {
        public Rectangle Source;
        public float Duration;
    }

    public class SpriteAnimation
    {
        private string name;
        private List<AnimationFrame> frames;
        private float duration;

        public string Name { get { return name; } }
        public List<AnimationFrame> Frames { get { return frames; } }
        public int NumFrames { get {  return frames.Count; } }
        public float Duration { get { return duration; } }

        public SpriteAnimation(string name, List<AnimationFrame> frames)
        {
            DebugAssert.Success(frames != null, 
                "Cannot create animation with null frames");
            DebugAssert.Success(frames.Count > 0, 
                "Cannot create animation with 0 frames");

            this.name    = name;
            this.frames  = frames;
            this.duration = 0.0f;

            foreach(AnimationFrame frame in frames)
            {
                duration += frame.Duration;
            }
        }

        public SpriteAnimation(string name, List<Rectangle> frameSources, float duration)
        {
            DebugAssert.Success(frameSources != null, 
                "Cannot create animation with null frames");
            DebugAssert.Success(frameSources.Count > 0, 
                "Cannot create animation with 0 frames");

            this.name     = name;
            this.frames   = new List<AnimationFrame>(frameSources.Count);
            this.duration = duration;

            float frameDuration = duration / frameSources.Count;
            foreach (Rectangle rect in frameSources)
            {
                frames.Add(new AnimationFrame()
                {
                    Source   = rect,
                    Duration = frameDuration,
                });
            }
        }
    }
}
