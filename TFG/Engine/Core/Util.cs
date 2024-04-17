using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Graphics;
using System;

namespace Engine.Core
{
    public static class Util
    {
        private static Texture2D BlankTexture;

        public static void Init(GraphicsDevice graphicsDevice)
        {
            BlankTexture = new Texture2D(graphicsDevice, 1, 1);
            BlankTexture.SetData(new Color[] { Color.White });
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(ref T value1, ref T value2)
        {
            T aux  = value1;
            value1 = value2;
            value2 = aux;
        }
    }
}
