using System;
using System.Collections.Generic;
using Engine.Debug;
using Core;

namespace Cmps
{
    [Flags]
    public enum AnimationPlayState
    {
        None           = 0,
        Loop           = 0x01,
        Reverse        = 0x02,
        LoopAndReverse = Loop | Reverse
    }

    public class AnimationControllerCmp
    {
        private Dictionary<string, SpriteAnimation> animations;

        public int SpriteIndex;
        public SpriteAnimation CurrentAnimation;
        public float PlaySpeedMult;
        public float CurrentFrameTime;
        public int CurrentFrameIndex;
        public AnimationPlayState PlayState;
        public bool IsPaused;
        public bool AnimationHasFinished;

        public AnimationControllerCmp(int spriteIndex)
        {
            animations           = new Dictionary<string, SpriteAnimation>();
            SpriteIndex          = spriteIndex;
            CurrentAnimation     = null;
            PlaySpeedMult        = 1.0f;
            CurrentFrameTime     = 0.0f;
            CurrentFrameIndex    = 0;
            PlayState            = AnimationPlayState.Loop;        
            IsPaused             = false;
            AnimationHasFinished = false;
        }

        public void Play(string name, AnimationPlayState playState = AnimationPlayState.Loop)
        {
            if(animations.TryGetValue(name, out SpriteAnimation anim))
            {
                CurrentAnimation     = anim;
                CurrentFrameTime     = 0.0f;
                CurrentFrameIndex    = 0;
                PlayState            = playState;
                IsPaused             = false;
                AnimationHasFinished = false;
            }
            else
            {
                CurrentAnimation = null;
                DebugLog.Warning("Trying to play unknow animation \"{0}\"", name);
            }
        }

        public void AddAnimation(string name, SpriteAnimation animation)
        {
            animations.Add(name, animation);
        }

        public void SetLooping(bool loop)
        {
            if (loop)
                PlayState |= AnimationPlayState.Loop;
            else
                PlayState &= (~AnimationPlayState.Loop);
        }

        public void SetReverse(bool reverse)
        {
            if (reverse)
                PlayState |= AnimationPlayState.Reverse;
            else
                PlayState &= (~AnimationPlayState.Reverse);
        }
    }
}
