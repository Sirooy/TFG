using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core
{
    public static class Util
    {
        public static void Begin(this SpriteBatch spriteBatch, Camera2D camera,
            SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendState blendState = null,
            SamplerState samplerState = null, DepthStencilState depthStencilState = null,
            RasterizerState rasterizerState = null, Effect effect = null)
        {
            int screenWidth  = camera.Screen.Width;
            int screenHeight = camera.Screen.Height;
            spriteBatch.GraphicsDevice.Viewport = new Viewport
            (
                (int)(screenWidth * camera.ViewportPosition.X),
                (int)(screenHeight * camera.ViewportPosition.Y),
                (int)(screenWidth * camera.ViewportSize.X),
                (int)(screenHeight * camera.ViewportSize.Y)
            );
            Matrix transform = camera.GetViewTransform();

            spriteBatch.Begin(sortMode, blendState, samplerState,
                depthStencilState, rasterizerState,
                effect, transform);
        }
    }
}
