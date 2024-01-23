using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Engine.Graphics;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Engine.Core;

namespace Engine.Graphics
{
    public class Camera2D
    {
        [Flags]
        private enum DirtyFlags
        {
            None        = 0,
            Translation = 0x01,
            Rotation    = 0x02,
            Scale       = 0x04,
            All         = Translation | Rotation | Scale
        }

        public const float MinZoom = 0.01f;
        public const float MaxZoom = 100.0f;
        public const float MinViewportSize = 0.000001f;
        public const float MaxViewportSize = 1.0f;

        private RenderScreen screen;
        private Matrix view;
        private Matrix inverseView;
        private Matrix translationMatrix;
        private Matrix rotationMatrix;
        private Matrix scaleMatrix;
        private Vector2 position;
        private Vector2 viewportPosition;
        private Vector2 viewportSize;
        private Vector2 positionAnchor;
        private Vector2 rotationAnchor;
        private float rotation;
        private float zoom;
        private float invZoom;
        private DirtyFlags isDirty;

        public RenderScreen Screen 
        { 
            get { return screen; } 
        }

        public Vector2 Position
        {
            get { return position; }
            set 
            { 
                isDirty |= DirtyFlags.Translation; 
                position = value; 
            }
        }

        public Vector2 ViewportPosition
        {
            get { return viewportPosition; }
            set
            {
                viewportPosition.X = value.X;
                viewportPosition.Y = value.Y;
            }
        }

        public Vector2 ViewportSize
        {
            get { return viewportSize; }
            set
            {
                isDirty |= DirtyFlags.Scale;
                viewportSize.X = Math.Clamp(value.X, MinViewportSize, MaxViewportSize);
                viewportSize.Y = Math.Clamp(value.Y, MinViewportSize, MaxViewportSize);
            }
        }

        public Vector2 PositionAnchor
        {
            get { return positionAnchor; }
            set
            {
                isDirty |= DirtyFlags.Translation;
                positionAnchor.X = Math.Clamp(value.X, 0.0f, 1.0f);
                positionAnchor.Y = Math.Clamp(value.Y, 0.0f, 1.0f);
            }
        }

        public Vector2 RotationAnchor
        {
            get { return rotationAnchor; }
            set
            {
                isDirty |= DirtyFlags.Rotation;
                rotationAnchor.X = Math.Clamp(value.X, 0.0f, 1.0f);
                rotationAnchor.Y = Math.Clamp(value.Y, 0.0f, 1.0f);
            }
        }

        public float Rotation
        {
            get { return rotation; }
            set 
            {
                isDirty |= DirtyFlags.Rotation;
                rotation = value; 
            }
        }

        public float Zoom
        {
            get { return zoom; }
            set 
            { 
                isDirty = DirtyFlags.All; 
                zoom    = Math.Clamp(value, MinZoom, MaxZoom);
                invZoom = 1.0f / zoom;
            }
        }

        public float InvZoom
        {
            get { return invZoom; }
        }

        public Camera2D(RenderScreen screen)
        {
            this.screen = screen;

            view              = Matrix.Identity;
            inverseView       = Matrix.Identity;
            translationMatrix = Matrix.Identity;
            rotationMatrix    = Matrix.Identity;
            scaleMatrix       = Matrix.Identity;
            position          = Vector2.Zero;
            viewportPosition  = Vector2.Zero;
            viewportSize      = Vector2.One;
            rotationAnchor    = new Vector2(0.5f);
            positionAnchor    = new Vector2(0.0f);
            rotation          = 0.0f;
            zoom              = 1.0f;
            invZoom           = 1.0f;
            isDirty           = DirtyFlags.All;
        }

        public Vector2 WindowToCameraCoords(Vector2 coords)
        {
            RecalculateView();

            screen.WindowToScreenCoords(ref coords);

            //Apply the inverse viewport position manually
            //because is not contained in the view matrix
            float x = coords.X - viewportPosition.X * screen.Width;
            float y = coords.Y - viewportPosition.Y * screen.Height;

            //Perform the matrix multiplication
            coords.X = x * inverseView.M11 + y * inverseView.M21 + inverseView.M41;
            coords.Y = x * inverseView.M12 + y * inverseView.M22 + inverseView.M42;

            return coords;
        }

        public AABB GetBounds()
        {
            RecalculateView();

            float w = screen.Width * viewportSize.X;
            float h = screen.Height * viewportSize.Y;

            //Camera Rect points
            Vector2[] points = new Vector2[4]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(w, 0.0f),
                new Vector2(0.0f, h),
                new Vector2(w, h)
            };

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            //Transform all the points in the rect with the inverse view
            //and get the min and max values to create an AABB that represents
            //the camera bounds
            for(int i = 0;i < points.Length; ++i)
            {
                Vector2 p = points[i];

                float x = p.X * inverseView.M11 + p.Y * inverseView.M21 + inverseView.M41;
                float y = p.X * inverseView.M12 + p.Y * inverseView.M22 + inverseView.M42;

                if (x < min.X) min.X = x;
                if (x > max.X) max.X = x;
                if (y < min.Y) min.Y = y;
                if (y > max.Y) max.Y = y;
            }

            return new AABB(min.X, max.X, min.Y, max.Y);
        }

        public Matrix GetViewTransform()
        {
            RecalculateView();

            return view;
        }

        private void RecalculateView()
        {
            if(isDirty != DirtyFlags.None)
            {
                if((isDirty & DirtyFlags.Translation) == DirtyFlags.Translation)
                {
                    translationMatrix = Matrix.CreateTranslation(
                        position.X - (positionAnchor.X * screen.Width * invZoom),
                        position.Y - (positionAnchor.Y * screen.Height * invZoom),
                        0.0f);
                }

                if ((isDirty & DirtyFlags.Rotation) == DirtyFlags.Rotation)
                {
                    //Translate first to the anchor point, perform the rotation
                    //and traslate back to the original position
                    rotationMatrix = Matrix.CreateTranslation(
                        -(rotationAnchor.X * screen.Width * invZoom),
                        -(rotationAnchor.Y * screen.Height * invZoom),
                         0.0f) *
                    Matrix.CreateRotationZ(rotation) *
                    Matrix.CreateTranslation(
                        (rotationAnchor.X * screen.Width * invZoom),
                        (rotationAnchor.Y * screen.Height * invZoom),
                        0.0f);
                }

                if ((isDirty & DirtyFlags.Scale) == DirtyFlags.Scale)
                {
                    scaleMatrix = Matrix.CreateScale(
                        (1.0f / viewportSize.X) * invZoom,
                        (1.0f / viewportSize.Y) * invZoom,
                        1.0f);
                }

                inverseView = scaleMatrix * rotationMatrix * translationMatrix;
                view        = Matrix.Invert(inverseView);
                isDirty     = DirtyFlags.None;
            }
        }
    }
}
