using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


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

        private static Texture2D BlankTexture;
        private static Texture2D SkillsUITexture;
        private static Rectangle[] ArrowSourceRects;

        public static void Init(GraphicsDevice graphicsDevice, ContentManager content)
        {
            BlankTexture    = new Texture2D(graphicsDevice, 1, 1);
            BlankTexture.SetData(new Color[] { Color.White });
            SkillsUITexture = content.Load<Texture2D>("SkillsUI");
            ArrowSourceRects = new Rectangle[3]
            {
                new Rectangle(0,  0, 32, 32),
                new Rectangle(32, 0, 32, 32),
                new Rectangle(64, 0, 32, 32)
            };
        }

        public static void DrawRectangle(this SpriteBatch spriteBatch,
            Vector2 position, Vector2 size, Color color)
        {
            spriteBatch.Draw(BlankTexture, position, null, color,
                0.0f, Vector2.Zero, size, SpriteEffects.None, 0.0f);
        }

        public static void DrawArrow(this SpriteBatch spriteBatch,
            Vector2 start, Vector2 end, float scale, Color color)
        {
            Rectangle startRect  = ArrowSourceRects[(int)ArrowPart.Start];
            Rectangle middleRect = ArrowSourceRects[(int)ArrowPart.Middle];
            Rectangle endRect    = ArrowSourceRects[(int)ArrowPart.End];

            Vector2 dir  = end - start;
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
