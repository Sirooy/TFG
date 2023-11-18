using Cmps;
using Engine.Core;
using Engine.Ecs;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TFG;

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
                Debug.Assert(sprite.Texture != null, 
                    "Cannot draw sprite with null texture");

                spriteBatch.Draw(sprite.Texture, sprite.Transform.GetWorldPosition(e),
                    sprite.SourceRect, sprite.Color, sprite.Transform.GetWorldRotation(e),
                    sprite.Origin, sprite.Transform.GetWorldScale(e), SpriteEffects.None, 
                    sprite.LayerDepth);
            });
            spriteBatch.End();

            DrawEntitiesAxis();
            DrawEntitiesPhysics();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DrawEntitiesAxis()
        {
            shapeBatch.Begin(camera);
            entityManager.ForEachEntity((Entity e) =>
            {
                float length = 30.0f * camera.InvZoom;
                float thickness = 4.0f * camera.InvZoom;

                Vector2 xAxis = new Vector2(MathF.Cos(e.Rotation),
                    MathF.Sin(e.Rotation));
                Vector2 yAxis = new Vector2(-MathF.Sin(e.Rotation),
                    MathF.Cos(e.Rotation));

                shapeBatch.DrawLine(e.Position, xAxis * e.Scale * length +
                    e.Position, thickness, Color.Red, 0.0f);
                shapeBatch.DrawLine(e.Position, yAxis * e.Scale * length +
                    e.Position, thickness, new Color(0, 255, 0), 0.0f);
                shapeBatch.DrawFilledRectangle(e.Position,
                    new Vector2(thickness * 2.0f), e.Rotation,
                    new Vector2(thickness), Color.Blue, 0.0f);
            });
            shapeBatch.End();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void DrawEntitiesPhysics()
        {
            shapeBatch.Begin(camera);
            entityManager.ForEachComponent((Entity e, PhysicsCmp physics) =>
            {
                Vector2 velocity = physics.LinearVelocity;
                Vector2 accel    = physics.Acceleration;

                shapeBatch.DrawLine(e.Position, e.Position + accel, 2.0f, 
                    Color.Magenta, 0.0f);
                shapeBatch.DrawLine(e.Position, e.Position + velocity, 1.0f,
                    Color.Cyan, 0.0f);
            });
            shapeBatch.End();
        }
    }
}
