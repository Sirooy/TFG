using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;

namespace Engine.Graphics
{
    public class RenderScreen : IDisposable
    {
        public const int MinSize = 64;
        public const int MaxSize = 4096;

        private GraphicsDevice graphicsDevice;
        private RenderTarget2D renderTarget;
        private Rectangle destinationRect;
        private int width;
        private int height;
        private int halfWidth;
        private int halfHeight;
        private float aspectRatio;

        public GraphicsDevice GraphicsDevice { get { return graphicsDevice; } }
        public RenderTarget2D RenderTarget { get { return renderTarget; } }
        public Rectangle DestinationRect { get { return destinationRect; } }
        public int Width { get { return width; } }
        public int Height { get { return height; } }
        public int HalfWidth { get { return halfWidth; } }
        public int HalfHeight { get { return halfHeight; } }
        public Vector2 Size { get { return new Vector2(width, height); } }

        public RenderScreen(GraphicsDevice graphicsDevice, int width, int height) 
        {
            this.graphicsDevice = graphicsDevice;
            Create(width, height);
        }

        public void Resize(int width, int height)
        {
            this.renderTarget.Dispose();
            Create(width, height);
        }

        private void Create(int width, int height)
        {
            DebugAssert.Success(width >= MinSize && width <= MaxSize,
                "Width must be between {0} and {1}. Width provided: {2}",
                MinSize, MaxSize, width);
            DebugAssert.Success(height >= MinSize && height <= MaxSize,
                "Height must be between {0} and {1}. Width provided: {2}",
                MinSize, MaxSize, height);

            this.width        = width;
            this.height       = height;
            this.halfWidth    = (int)(width * 0.5f);
            this.halfHeight   = (int)(height * 0.5f);
            this.aspectRatio  = (float)width / height;
            this.renderTarget = new RenderTarget2D(graphicsDevice, width, height);
            UpdateDestinationRect();
        }

        public Vector2 WindowToScreenCoords(Vector2 coords)
        {
            coords.X -= destinationRect.X;
            coords.Y -= destinationRect.Y;
            coords.X *= (float) renderTarget.Width / destinationRect.Width;
            coords.Y *= (float) renderTarget.Height / destinationRect.Height;

            return coords;
        }

        public void WindowToScreenCoords(ref Vector2 coords)
        {
            coords.X -= destinationRect.X;
            coords.Y -= destinationRect.Y;
            coords.X *= (float) renderTarget.Width / destinationRect.Width;
            coords.Y *= (float) renderTarget.Height / destinationRect.Height;
        }

        public void Attach()
        {
            graphicsDevice.SetRenderTarget(renderTarget);
        }

        public void Present(SpriteBatch spriteBatch, SamplerState sampler, Color clearColor)
        {
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Clear(clearColor);

            spriteBatch.Begin(samplerState: sampler, blendState: BlendState.Opaque);
            spriteBatch.Draw(renderTarget, destinationRect, null, Color.White);
            spriteBatch.End();
        }

        public void UpdateDestinationRect()
        {
            int windowWidth    = graphicsDevice.PresentationParameters.BackBufferWidth;
            int windowHeight   = graphicsDevice.PresentationParameters.BackBufferHeight;
            float windowAspect = (float)windowWidth / windowHeight;

            float finalWidth   = windowWidth;
            float finalHeight  = windowHeight;
            float x            = 0.0f;
            float y            = 0.0f;

            //Width must match the windows width
            //So scale the height
            if (aspectRatio > windowAspect) 
            {
                finalHeight = finalWidth / aspectRatio;
                y           = windowHeight * 0.5f - finalHeight * 0.5f;
            }
            //Otherwise scale the width
            else if (aspectRatio < windowAspect)
            {
                finalWidth = finalHeight * aspectRatio;
                x          = windowWidth * 0.5f - finalWidth * 0.5f;
            }

            destinationRect = new Rectangle((int)x, (int)y, 
                (int)finalWidth, (int)finalHeight);
        }

        public void Dispose()
        {
            if (renderTarget == null) return;

            renderTarget.Dispose();
            renderTarget = null;
        }
    }
}
