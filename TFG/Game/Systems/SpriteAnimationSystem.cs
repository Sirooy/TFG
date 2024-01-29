using Engine.Ecs;
using Engine.Debug;
using Core;
using Cmps;

namespace Systems
{
    public class SpriteAnimationSystem : GameSystem
    {
        private EntityManager<Entity> entityManager;

        public SpriteAnimationSystem(EntityManager<Entity> entityManager)
        {
            this.entityManager = entityManager;
        }

        public override void Update(float dt)
        {
            ReadOnlyMSA<SpriteCmp> sprites = entityManager.GetComponents<SpriteCmp>();

            entityManager.ForEachComponent((Entity e, AnimationControllerCmp controller) =>
            {
                DebugAssert.Success(controller.SpriteIndex < sprites.GetItemCount(e.Id),
                    "Invalid sprite index {0}. Entity has {1} sprite components",
                    controller.SpriteIndex, sprites.GetItemCount(e.Id));

                SpriteAnimation anim = controller.CurrentAnimation;
                
                if (!controller.IsPaused && anim != null)
                {
                    SpriteCmp sprite = sprites.Get(e.Id, controller.SpriteIndex);

                    switch (controller.PlayState)
                    {
                        case AnimationPlayState.Loop:
                            UpdateAnimationWithLoop(controller, anim, sprite, dt);
                            break;
                        case AnimationPlayState.LoopAndReverse:
                            UpdateAnimationWithLoopReversed(controller, anim, sprite, dt);
                            break;
                        case AnimationPlayState.Reverse:
                            UpdateAnimationReversed(controller, anim, sprite, dt);
                            break;
                        default:
                            UpdateAnimation(controller, anim, sprite, dt);
                            break;
                    }
                }
            });
        }

        //Animation without looping and not reversed
        private static void UpdateAnimation(AnimationControllerCmp controller,
            SpriteAnimation anim, SpriteCmp sprite, float dt)
        {
            //If animation is not in the last frame
            if(controller.CurrentFrameIndex < anim.NumFrames - 1)
            {
                AnimationFrame currentFrame  = anim.Frames[controller.CurrentFrameIndex];
                controller.CurrentFrameTime += dt * controller.PlaySpeedMult;

                if (controller.CurrentFrameTime >= currentFrame.Duration)
                {
                    controller.CurrentFrameTime = 0.0f;
                    controller.CurrentFrameIndex++;

                    if (controller.CurrentFrameIndex == anim.NumFrames - 1)
                        controller.AnimationHasFinished = true;

                    sprite.SourceRect = anim.Frames[controller.CurrentFrameIndex].Source;
                }
            }
        }

        private static void UpdateAnimationReversed(AnimationControllerCmp controller,
            SpriteAnimation anim, SpriteCmp sprite, float dt)
        {
            //If animation is not in the last frame
            if (controller.CurrentFrameIndex > 0)
            {
                AnimationFrame currentFrame  = anim.Frames[controller.CurrentFrameIndex];
                controller.CurrentFrameTime += dt * controller.PlaySpeedMult;

                if (controller.CurrentFrameTime >= currentFrame.Duration)
                {
                    controller.CurrentFrameTime = 0.0f;
                    controller.CurrentFrameIndex--;

                    if (controller.CurrentFrameIndex == 0)
                        controller.AnimationHasFinished = true;

                    sprite.SourceRect = anim.Frames[controller.CurrentFrameIndex].Source;
                }
            }
        }

        //Animation with looping and not reversed
        private static void UpdateAnimationWithLoop(AnimationControllerCmp controller,
            SpriteAnimation anim, SpriteCmp sprite, float dt)
        {
            AnimationFrame currentFrame     = anim.Frames[controller.CurrentFrameIndex];
            controller.AnimationHasFinished = false;

            controller.CurrentFrameTime += dt * controller.PlaySpeedMult;
            if (controller.CurrentFrameTime >= currentFrame.Duration)
            {
                controller.CurrentFrameTime = 0.0f;
                controller.CurrentFrameIndex++;

                if(controller.CurrentFrameIndex == anim.NumFrames)
                {
                    controller.CurrentFrameIndex    = 0;
                    controller.AnimationHasFinished = true;
                }

                sprite.SourceRect = anim.Frames[controller.CurrentFrameIndex].Source;
            }
        }

        //Update animation with looping and reversed
        private static void UpdateAnimationWithLoopReversed(AnimationControllerCmp controller,
            SpriteAnimation anim, SpriteCmp sprite, float dt)
        {
            AnimationFrame currentFrame = anim.Frames[controller.CurrentFrameIndex];
            controller.AnimationHasFinished = false;

            controller.CurrentFrameTime += dt * controller.PlaySpeedMult;
            if (controller.CurrentFrameTime >= currentFrame.Duration)
            {
                controller.CurrentFrameTime = 0.0f;
                controller.CurrentFrameIndex--;

                if (controller.CurrentFrameIndex == -1)
                {
                    controller.CurrentFrameIndex    = anim.NumFrames - 1;
                    controller.AnimationHasFinished = true;
                }

                sprite.SourceRect = anim.Frames[controller.CurrentFrameIndex].Source;
            }
        }
    }
}
