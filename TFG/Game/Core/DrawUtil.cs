using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;

namespace Core
{
    public static class DrawUtil
    {
        private enum ArrowPart
        {
            Start  = 0,
            Middle = 1,
            End    = 2,
        }

        private enum BarPart
        {
            Start  = 0,
            Middle = 1,
            End    = 2
        }

        private static Texture2D BlankTexture;
        private static Texture2D SkillsUITexture;
        private static Rectangle[] ArrowSourceRects;
        private static Rectangle[] BarSourceRects;

        public static void Init(GraphicsDevice graphicsDevice, ContentManager content)
        {
            BlankTexture    = new Texture2D(graphicsDevice, 1, 1);
            BlankTexture.SetData(new Color[] { Color.White });
            SkillsUITexture = content.Load<Texture2D>(
                GameContent.TexturePath("GameplayUI"));
            ArrowSourceRects = new Rectangle[3]
            {
                new Rectangle(0,  0, 32, 32),
                new Rectangle(32, 0, 32, 32),
                new Rectangle(64, 0, 32, 32)
            };
            BarSourceRects = new Rectangle[3]
            {
                new Rectangle(96,  0, 16, 16),
                new Rectangle(112, 0, 16, 16),
                new Rectangle(128, 0, 16, 16)
            };
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch,
            Vector2 position, Vector2 size, Color color)
        {
            spriteBatch.Draw(BlankTexture, position, null, color,
                0.0f, Vector2.Zero, size, SpriteEffects.None, 0.0f);
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch,
            Vector2 position, Vector2 size, Color color, float rotation,
            Vector2 origin)
        {
            spriteBatch.Draw(BlankTexture, position, null, color,
                rotation, origin / size, size, SpriteEffects.None, 0.0f);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, 
            Vector2 start, Vector2 end, float thickness, Color color)
        {
            Vector2 dir  = end - start;
            float width  = dir.Length() + thickness; //Add half the thickness on both sides
            float angle  = MathF.Atan2(dir.Y, dir.X);
            Vector2 size = new Vector2(width, thickness);

            spriteBatch.DrawRectangle(start, new Vector2(width, thickness), color,
                angle, new Vector2(thickness * 0.5f));
        }

        public static void DrawCircle(this SpriteBatch spriteBatch,
            Vector2 center, float radius, float thickness, Color color, 
            int points = 16)
        {
            DebugAssert.Success(points >= 3, 
                "Cannot draw a circle with {0} points", points);

            float angle   = MathUtil.PI2 / points;
            Vector2 start = new Vector2(radius, 0.0f);

            for(int i = 0;i < points; ++i)
            {
                Vector2 rotated = MathUtil.Rotate(start, angle);
                Vector2 p1 = start + center;
                Vector2 p2 = rotated + center;

                spriteBatch.DrawLine(p1, p2, thickness, color);
                start = rotated;
            }
        }

        public static void DrawBar(this SpriteBatch spriteBatch,
            Vector2 center, float length, float height, Color color)
        {
            Rectangle startRect  = BarSourceRects[(int)BarPart.Start];
            Rectangle middleRect = BarSourceRects[(int)BarPart.Middle];
            Rectangle endRect    = BarSourceRects[(int)BarPart.End];

            float scale    = height / startRect.Height;
            float width    = scale * startRect.Width;
            float minWidth = Math.Max(width * 2.0f, length);
            Vector2 drawPos = center - new Vector2(minWidth * 0.5f, height * 0.5f);

            spriteBatch.Draw(SkillsUITexture, drawPos, startRect, color,
                0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);

            drawPos.X         += width;
            float middleLength = length - width * 2.0f;
            while (middleLength >= width)
            {
                spriteBatch.Draw(SkillsUITexture, drawPos, middleRect, color,
                    0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);

                middleLength -= width;
                drawPos.X    += width;
            }

            if (middleLength > 0.0f)
            {
                float ratio      = middleLength / width;
                middleRect.Width = (int)MathF.Round(middleRect.Width * ratio);

                spriteBatch.Draw(SkillsUITexture, drawPos, middleRect, color,
                    0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);

                drawPos.X       += middleLength;
            }

            spriteBatch.Draw(SkillsUITexture, drawPos, endRect, color,
                0.0f, Vector2.Zero, scale, SpriteEffects.None, 0.0f);
        }

        public static void DrawArrow(this SpriteBatch spriteBatch,
            Vector2 start, Vector2 dir, float distance, float thickness, Color color)
        {
            Vector2 end = start + dir * distance;
            DrawArrow(spriteBatch, start, end, thickness, color);
        }

        public static void DrawArrow(this SpriteBatch spriteBatch,
            Vector2 start, Vector2 end, float thickness, Color color)
        {
            Rectangle startRect  = ArrowSourceRects[(int)ArrowPart.Start];
            Rectangle middleRect = ArrowSourceRects[(int)ArrowPart.Middle];
            Rectangle endRect    = ArrowSourceRects[(int)ArrowPart.End];

            Vector2 dir  = end - start;
            float scale  = thickness / endRect.Height;
            float width  = scale * endRect.Width;
            float angle  = MathF.Atan2(dir.Y, dir.X);
            float length = dir.Length();

            if (length != 0.0f)
                dir /= length;

            spriteBatch.Draw(SkillsUITexture, start, startRect, color,
                angle, new Vector2(startRect.Width, startRect.Height * 0.5f),
                scale, SpriteEffects.None, 0.0f);

            //Remove the size of the tip of the arrow from the middle part first
            float middleSize = length - width;
            Vector2 drawPos  = start;
            //Draw middle parts until it runs out of size
            while (middleSize >= width)
            {
                spriteBatch.Draw(SkillsUITexture, drawPos, middleRect, color,
                    angle, new Vector2(0.0f, startRect.Height * 0.5f),
                    scale, SpriteEffects.None, 0.0f);

                drawPos    += dir * width;
                middleSize -= width;
            }

            //Draw the remainder of the last middle part
            if(middleSize > 0.0f)
            {
                float ratio      = middleSize / width;
                middleRect.Width = (int) MathF.Round(middleRect.Width * ratio);
                spriteBatch.Draw(SkillsUITexture, drawPos, middleRect, color,
                    angle, new Vector2(0.0f, startRect.Height * 0.5f),
                    scale, SpriteEffects.None, 0.0f);
                
                drawPos += dir * middleSize;
            }

            //Draw the tip of the arrow
            spriteBatch.Draw(SkillsUITexture, drawPos, endRect, color,
                angle, new Vector2(0.0f, startRect.Height * 0.5f),
                scale, SpriteEffects.None, 0.0f);
        }
    }
}
