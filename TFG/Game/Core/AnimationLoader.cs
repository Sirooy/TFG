using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System;

namespace Core
{
    public class AnimationLoader
    {
        private Dictionary<string, List<SpriteAnimation>> loadedAnimations;

        public AnimationLoader()
        {
            loadedAnimations = new Dictionary<string, List<SpriteAnimation>>();
        }

        public List<SpriteAnimation> Load(string path)
        {
            if (loadedAnimations.TryGetValue(path, out List<SpriteAnimation> ret))
                return ret;

            List<SpriteAnimation> newAnims = ReadAnimationsFromFile(path);
            loadedAnimations.Add(path, newAnims);

            return newAnims;
        }

        private List<SpriteAnimation> ReadAnimationsFromFile(string path)
        {
            List<SpriteAnimation> ret = new List<SpriteAnimation>();

            using (Stream stream = File.OpenRead(path))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int numAnimations = reader.ReadInt32();

                    for(int i = 0;i < numAnimations; ++i)
                    {
                        string name                 = reader.ReadString();
                        int numFrames               = reader.ReadInt32();
                        List<AnimationFrame> frames = ReadAnimationFrames(reader, numFrames);

                        ret.Add(new SpriteAnimation(name, frames));
                    }
                }
            }

            return ret;
        }

        private List<AnimationFrame> ReadAnimationFrames(BinaryReader reader, int numFrames)
        {
            List<AnimationFrame> frames = new List<AnimationFrame>();

            for(int i = 0;i < numFrames; ++i)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                int w = reader.ReadInt32();
                int h = reader.ReadInt32();
                float duration = reader.ReadSingle();

                frames.Add(new AnimationFrame()
                {
                    Source = new Rectangle(x, y, w, h),
                    Duration = duration
                });
            }

            return frames;
        }
    }
}
