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
using Physics;

namespace Systems
{
    public class RenderSystem : Engine.Ecs.System
    {
        private EntityManager<Entity> entityManager;
        private SpriteBatch spriteBatch;
        private ShapeBatch shapeBatch;
        private Camera2D camera;

        public RenderSystem(EntityManager<Entity> entityManager, 
            SpriteBatch spriteBatch, ShapeBatch shapeBatch, 
            Camera2D camera)
        {
            this.entityManager = entityManager;
            this.spriteBatch   = spriteBatch;
            this.shapeBatch    = shapeBatch;
            this.camera        = camera;
        }

        public override void Update()
        {
            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            entityManager.ForEachComponent((Entity e, SpriteCmp sprite) =>
            {
                DebugAssert.Success(sprite.Texture != null, 
                    "Cannot draw sprite with null texture");

                spriteBatch.Draw(sprite.Texture, sprite.Transform.GetWorldPosition(e),
                    sprite.SourceRect, sprite.Color, sprite.Transform.GetWorldRotation(e),
                    sprite.Origin, sprite.Transform.GetWorldScale(e), SpriteEffects.None, 
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
