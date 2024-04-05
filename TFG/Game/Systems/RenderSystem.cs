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
            DrawCharacterCmps();
            DrawSpriteCmps();
            spriteBatch.End();

            spriteBatch.Begin(camera, samplerState: SamplerState.PointClamp);
            DrawHealthCmps();
            spriteBatch.End();
            
            DebugDrawEntitiesAxis();
        }

        private void DrawHealthCmps()
        {
            entityManager.ForEachComponent((Entity e, HealthCmp health) =>
            {
                const float MAX_WIDTH = 32.0f;
                const float HEIGHT = 2.0f;
                const float MARGIN = 4.0f;

                Vector2 startPos = Vector2.Zero; 
                if (entityManager.TryGetComponent(e, out SpriteCmp sprite))
                {
                    
                    startPos = new Vector2(
                        e.Position.X - MAX_WIDTH * 0.5f,
                        sprite.Transform.CachedWorldPosition.Y - sprite.Origin.Y - MARGIN);
                }
                else
                {
                    startPos = e.Position - new Vector2(
                        MAX_WIDTH * 0.5f, HEIGHT * 0.5f);
                }

                float t = health.CurrentHealth / health.MaxHealth;
                spriteBatch.DrawRectangle(startPos, new Vector2(MAX_WIDTH, HEIGHT),
                    Color.Red);
                spriteBatch.DrawRectangle(startPos, new Vector2(MAX_WIDTH * t, HEIGHT),
                    new Color(0.0f, 1.0f, 0.0f));
            });
        }

        private void DrawCharacterCmps()
        {
            entityManager.ForEachComponent((Entity e, CharacterCmp chara) =>
            {
                DebugAssert.Success(chara.PlatformTexture != null,
                    "Cannot draw character platform with null texture");

                //Draw the circle selection
                if(chara.SelectState != SelectState.None)
                {
                    Vector2 selectPos = new Vector2(
                        e.Position.X - chara.SelectSourceRect.Width * 0.5f,
                        e.Position.Y - chara.SelectSourceRect.Height * 0.5f);

                    Color color = Color.White;
                    switch (chara.SelectState)
                    {
                        case SelectState.Selected:
                            color = new Color(0, 255, 0);
                            break;
                        case SelectState.Hover:
                            color = Color.Yellow;
                            break;
                    }

                    spriteBatch.Draw(chara.PlatformTexture, selectPos, 
                        chara.SelectSourceRect, color);
                }

                Vector2 platformPos = new Vector2(
                    e.Position.X - chara.PlatformSourceRect.Width * 0.5f,
                    e.Position.Y - chara.PlatformSourceRect.Height * 0.5f);

                spriteBatch.Draw(chara.PlatformTexture, platformPos,
                    chara.PlatformSourceRect, Color.White);
            });
        }

        private void DrawSpriteCmps()
        {
            entityManager.ForEachComponent((Entity e, SpriteCmp sprite) =>
            {
                DebugAssert.Success(sprite.Texture != null,
                    "Cannot draw sprite with null texture");

                spriteBatch.Draw(sprite.Texture, sprite.Transform.CachedWorldPosition,
                    sprite.SourceRect, sprite.Color, sprite.Transform.CachedWorldRotation,
                    sprite.Origin, sprite.Transform.CachedWorldScale, SpriteEffects.None,
                    sprite.LayerDepth);
            });
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
