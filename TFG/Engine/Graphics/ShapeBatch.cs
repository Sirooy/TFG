using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Core;
using Engine.Debug;

namespace Engine.Graphics
{
    public class ShapeBatch : IDisposable
    {
        private const int NUM_VERTEX_PER_SHAPE = 4;
        private const int NUM_INDICES_PER_SHAPE = 6;

        private GraphicsDevice graphicsDevice;
        private BasicEffect basicEffect;
        private VertexPositionColor[] vertices;
        private int[] indices;
        private int currentVertexCount;
        private int currentIndexCount;
        private bool beginCalled;

        public ShapeBatch(GraphicsDevice graphicsDevice, int capacity = 100)
        {
            this.graphicsDevice = graphicsDevice;
            vertices = new VertexPositionColor[NUM_VERTEX_PER_SHAPE * capacity];
            indices = new int[NUM_INDICES_PER_SHAPE * capacity];

            currentVertexCount = 0;
            currentIndexCount = 0;
            beginCalled = false;

            basicEffect = new BasicEffect(graphicsDevice);
            basicEffect.VertexColorEnabled = true;
            basicEffect.LightingEnabled = false;
            basicEffect.TextureEnabled = false;
            basicEffect.FogEnabled = false;
            basicEffect.World = Matrix.Identity;
            basicEffect.View = Matrix.Identity;
            basicEffect.Projection = Matrix.Identity;
        }

        public void Dispose()
        {
            if (basicEffect == null) return;

            basicEffect.Dispose();
            basicEffect = null;
        }

        public void Begin()
        {
            DebugAssert.Success(!beginCalled, "Begin cannot be called again until End" +
                " has been called");

            Viewport viewport = graphicsDevice.Viewport;       
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter
                (0.0f, viewport.Width, viewport.Height, 0.0f, 0.0f, 1.0f);

            currentVertexCount = 0;
            currentIndexCount = 0;
            beginCalled = true;
        }

        public void Begin(Camera2D camera)
        {
            int screenWidth = camera.Screen.Width;
            int screenHeight = camera.Screen.Height;
            graphicsDevice.Viewport = new Viewport
            (
                (int)(screenWidth * camera.ViewportPosition.X),
                (int)(screenHeight * camera.ViewportPosition.Y),
                (int)(screenWidth * camera.ViewportSize.X),
                (int)(screenHeight * camera.ViewportSize.Y)
            );
            basicEffect.View = camera.GetViewTransform();

            Begin();
        }

        /*
         Winding order = 0,2,1 | 0,3,2

        +-------- +X
        |         Top
        |    3 |-----| 2
        |      |    /|
        |      |   / |  
        | Left |  /  | Right 
        +Y     | /   |  
               |/    |  
             0 |-----| 1  
                Bottom
        */

        #region Filled Rectangle
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawFilledRectangle(Rectangle rectangle, Color color,
            float layerDepth = 0.0f)
        {

            DrawFilledRectangle(
                new Vector2(rectangle.Left, rectangle.Top),
                new Vector2(rectangle.Width, rectangle.Height),
                color, layerDepth);
        }

        public void DrawFilledRectangle(Vector2 position, Vector2 size, Color color,
            float layerDepth = 0.0f)
        {
            DebugAssert.Success(beginCalled, "Cannot draw without calling begin first");
            FlushIfNeeded();

            float left = position.X;
            float right = position.X + size.X;
            float top = position.Y;
            float bottom = position.Y + size.Y;

            vertices[currentVertexCount + 0] = new VertexPositionColor(
                new Vector3(left, bottom, layerDepth), color);
            vertices[currentVertexCount + 1] = new VertexPositionColor(
                new Vector3(right, bottom, layerDepth), color);
            vertices[currentVertexCount + 2] = new VertexPositionColor(
                new Vector3(right, top, layerDepth), color);
            vertices[currentVertexCount + 3] = new VertexPositionColor(
                new Vector3(left, top, layerDepth), color);

            AppendRectangleIndices();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawFilledRectangle(Rectangle rectangle, float rotation,
            Vector2 origin, Color color, float layerDepth = 0.0f)
        {
            DrawFilledRectangle(
                new Vector2(rectangle.Left, rectangle.Top),
                new Vector2(rectangle.Width, rectangle.Height),
                rotation, origin, color, layerDepth);
        }

        public void DrawFilledRectangle(Vector2 position, Vector2 size,
            float rotation, Vector2 origin, Color color, float layerDepth = 0.0f)
        {
            DebugAssert.Success(beginCalled, "Cannot draw without calling begin first");
            FlushIfNeeded();

            CreateRectangleRotatedVertices(position, size, rotation, origin,
                out Vector2 bottomLeft,
                out Vector2 bottomRight,
                out Vector2 topLeft,
                out Vector2 topRight);

            vertices[currentVertexCount + 0] = new VertexPositionColor(
                new Vector3(bottomLeft.X, bottomLeft.Y, layerDepth), color);
            vertices[currentVertexCount + 1] = new VertexPositionColor(
                new Vector3(bottomRight.X, bottomRight.Y, layerDepth), color);
            vertices[currentVertexCount + 2] = new VertexPositionColor(
                new Vector3(topRight.X, topRight.Y, layerDepth), color);
            vertices[currentVertexCount + 3] = new VertexPositionColor(
                new Vector3(topLeft.X, topLeft.Y, layerDepth), color);

            AppendRectangleIndices();
        }
        #endregion

        #region Rectangle
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(Rectangle rectangle, float thickness,
            Color color, float layerDepth = 0.0f)
        {
            DrawRectangle(
                new Vector2(rectangle.Left, rectangle.Top),
                new Vector2(rectangle.Width, rectangle.Height),
                thickness, color, layerDepth);
        }

        public void DrawRectangle(Vector2 position, Vector2 size,
            float thickness, Color color, float layerDepth = 0.0f)
        {
            DebugAssert.Success(beginCalled, "Cannot draw without calling begin first");
            float left = position.X;
            float right = position.X + size.X;
            float top = position.Y;
            float bottom = position.Y + size.Y;

            DrawLine(new Vector2(left, top), new Vector2(right, top),
                thickness, color, layerDepth);
            DrawLine(new Vector2(right, top), new Vector2(right, bottom),
                thickness, color, layerDepth);
            DrawLine(new Vector2(right, bottom), new Vector2(left, bottom),
                thickness, color, layerDepth);
            DrawLine(new Vector2(left, bottom), new Vector2(left, top),
                thickness, color, layerDepth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawRectangle(Rectangle rectangle, float thickness,
            float rotation, Vector2 origin, Color color, float layerDepth = 0.0f)
        {
            DrawRectangle(
                new Vector2(rectangle.Left, rectangle.Top),
                new Vector2(rectangle.Width, rectangle.Height),
                thickness, rotation, origin, color, layerDepth);
        }

        public void DrawRectangle(Vector2 position, Vector2 size, float thickness,
            float rotation, Vector2 origin, Color color, float layerDepth = 0.0f)
        {
            DebugAssert.Success(beginCalled, "Cannot draw without calling begin first");
            FlushIfNeeded();

            CreateRectangleRotatedVertices(position, size, rotation, origin,
                out Vector2 bottomLeft,
                out Vector2 bottomRight,
                out Vector2 topLeft,
                out Vector2 topRight);

            DrawLine(bottomLeft, bottomRight, thickness, color, layerDepth);
            DrawLine(bottomRight, topRight, thickness, color, layerDepth);
            DrawLine(topRight, topLeft, thickness, color, layerDepth);
            DrawLine(topLeft, bottomLeft, thickness, color, layerDepth);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CreateRectangleRotatedVertices(Vector2 position,
            Vector2 size, float rotation, Vector2 origin, out Vector2 bottomLeft,
            out Vector2 bottomRight, out Vector2 topLeft, out Vector2 topRight)
        {
            float cos = MathF.Cos(rotation);
            float sin = MathF.Sin(rotation);

            //Rotate the vertices around the origin
            float left = -origin.X;
            float right = left + size.X;
            float top = -origin.Y;
            float bottom = top + size.Y;
            float lCos = left * cos;
            float lSin = left * sin;
            float rCos = right * cos;
            float rSin = right * sin;
            float tCos = top * cos;
            float tSin = top * sin;
            float bCos = bottom * cos;
            float bSin = bottom * sin;

            //Add the traslation to the rotated vertices
            bottomLeft = new Vector2(
                lCos - bSin + position.X,
                bCos + lSin + position.Y);
            bottomRight = new Vector2(
                rCos - bSin + position.X,
                bCos + rSin + position.Y);
            topLeft = new Vector2(
                lCos - tSin + position.X,
                tCos + lSin + position.Y);
            topRight = new Vector2(
                rCos - tSin + position.X,
                tCos + rSin + position.Y);
        }
        #endregion

        #region Line
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawLine(Vector2 start, Vector2 end, Color color,
            float layerDepth = 0.0f)
        {
            DrawLine(start, end, 1.0f, color, layerDepth);
        }

        public void DrawLine(Vector2 start, Vector2 end,
            float thickness, Color color, float layerDepth = 0.0f)
        {
            DebugAssert.Success(beginCalled, "Cannot draw without calling begin first");
            FlushIfNeeded();

            Vector2 dir = end - start;
            float length = dir.Length();

            if (length != 0.0f)
            {
                dir /= length;
                float halfThickness = thickness * 0.5f;

                float ex = dir.X * halfThickness;  //Line edge/direction
                float ey = dir.Y * halfThickness;
                float nx = -ey;                    //Line normal
                float ny = ex;
                //ex *= 0.5f;
                //ey *= 0.5f;

                float xTL = start.X - ex - nx; //Top Left
                float xBL = start.X - ex + nx; //Bottom Left
                float xTR = end.X + ex - nx;   //Top Right
                float xBR = end.X + ex + nx;   //Bottom Right

                float yTL = start.Y - ey - ny;
                float yBL = start.Y - ey + ny;
                float yTR = end.Y + ey - ny;
                float yBR = end.Y + ey + ny;

                vertices[currentVertexCount + 0] = new VertexPositionColor(
                    new Vector3(xBL, yBL, layerDepth), color);
                vertices[currentVertexCount + 1] = new VertexPositionColor(
                    new Vector3(xBR, yBR, layerDepth), color);
                vertices[currentVertexCount + 2] = new VertexPositionColor(
                    new Vector3(xTR, yTR, layerDepth), color);
                vertices[currentVertexCount + 3] = new VertexPositionColor(
                    new Vector3(xTL, yTL, layerDepth), color);

                AppendRectangleIndices();
            }
        }
        #endregion

        #region Circle
        public void DrawCircle(Vector2 center, float radius, int points, float thickness,
            Color color, float layerDepth = 0.0f)
        {
            DebugAssert.Success(beginCalled, "Cannot draw without calling begin first");
            DebugAssert.Success(points >= 3, "Cannot draw a circle with " +
                "less than 3 points. Points: {0}", points);

            float angle = MathF.PI * 2.0f / points;
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);

            Vector2 first = new Vector2(radius, 0.0f);
            for (int i = 0; i < points; ++i)
            {
                Vector2 next = new Vector2(
                    first.X * cos - first.Y * sin,
                    first.X * sin + first.Y * cos);

                DrawLine(new Vector2(first.X + center.X, first.Y + center.Y),
                    new Vector2(next.X + center.X, next.Y + center.Y),
                    thickness, color, layerDepth);

                first = next;
            }
        }
        #endregion

        private void Flush()
        {
            //Return if there is nothing to draw
            if (currentVertexCount == 0) return;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
            }

            graphicsDevice.DrawUserIndexedPrimitives(
                PrimitiveType.TriangleList, vertices, 0,
                currentVertexCount, indices, 0, currentIndexCount / 3);

            currentIndexCount = 0;
            currentVertexCount = 0;
        }

        public void End()
        {
            DebugAssert.Success(beginCalled, "Cannot call End without calling Begin first");

            Flush();
            beginCalled      = false;
            basicEffect.View = Matrix.Identity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushIfNeeded()
        {
            if (currentVertexCount >= vertices.Length)
                Flush();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AppendRectangleIndices()
        {
            indices[currentIndexCount + 0] = currentVertexCount + 0;
            indices[currentIndexCount + 1] = currentVertexCount + 2;
            indices[currentIndexCount + 2] = currentVertexCount + 1;
            indices[currentIndexCount + 3] = currentVertexCount + 0;
            indices[currentIndexCount + 4] = currentVertexCount + 3;
            indices[currentIndexCount + 5] = currentVertexCount + 2;

            currentIndexCount += NUM_INDICES_PER_SHAPE;
            currentVertexCount += NUM_VERTEX_PER_SHAPE;
        }
    }
}
