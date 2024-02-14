using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;
using Engine.Ecs;
using Engine.Graphics;
using Cmps;
using Core;

namespace Systems
{
    public class RenderSystem : GameSystem
    {
        private EntityManager<Entity> entityManager;
        private SpriteBatch spriteBatch;
        private Camera2D camera;

        public RenderSystem(EntityManager<Entity> entityManager, 
            SpriteBatch spriteBatch, Camera2D camera)
        {
            this.entityManager = entityManager;
            this.spriteBatch   = spriteBatch;
            this.camera        = camera;
        }

        public override void Update(float _)
        {
            spriteBatch.Begin(camera, sortMode: SpriteSortMode.FrontToBack, 
                samplerState: SamplerState.PointClamp);
            entityManager.ForEachComponent((Entity e, SpriteCmp sprite) =>
            {
                DebugAssert.Success(sprite.Texture != null, 
                    "Cannot draw sprite with null texture");
                
                spriteBatch.Draw(sprite.Texture, sprite.Transform.CachedWorldPosition,
                    sprite.SourceRect, sprite.Color, sprite.Transform.CachedWorldRotation,
                    sprite.Origin, sprite.Transform.CachedWorldScale, SpriteEffects.None,
                    sprite.LayerDepth);
            });
            spriteBatch.End();

            DebugDrawEntitiesAxis();
        }

        #region Debug Draw

        [Conditional(DebugDraw.DEBUG_DEFINE)]
        private void DebugDrawEntitiesAxis()
        {
            if (!DebugDraw.IsMainLayerEnabled()) return;

            entityManager.ForEachEntity((Entity e) =>
            {
                const float AXIS_LENGTH = 32.0f;

                Vector2 xAxis = new Vector2(MathF.Cos(e.Rotation),
                    MathF.Sin(e.Rotation));
                Vector2 yAxis = new Vector2(-MathF.Sin(e.Rotation),
                    MathF.Cos(e.Rotation));

                DebugDraw.Line(e.Position, e.Position + 
                    xAxis * AXIS_LENGTH, Color.Red);
                DebugDraw.Line(e.Position, e.Position + 
                    yAxis * AXIS_LENGTH, new Color(0, 255, 0));
                DebugDraw.Point(e.Position, Color.Blue);
            });
        }

        #endregion
    }
}
