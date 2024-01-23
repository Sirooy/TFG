using Engine.Core;
using Engine.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class CameraController
    {
        public const float MinZoom  = 1.0f;
        public const float MaxZoom  = 5.0f;
        public const float ZoomStep = 0.5f;

        private Camera2D camera;
        private float targetZoom;
        private Vector2 dragStartPosition;
        private Vector2 dragCameraBasePosition;
        private bool isDragging;

        public Camera2D Camera { get { return camera; } }

        public CameraController(Camera2D camera)
        {
            this.camera = camera;
            this.targetZoom = MinZoom;
            this.dragStartPosition = Vector2.Zero;
            this.dragCameraBasePosition = Vector2.Zero;
            this.isDragging = false;    
        }

        public void Update(float dt)
        {
            int scrollDiff = Math.Sign(MouseInput.HorScrollValueDiff);
            if (scrollDiff != 0)
            {
                targetZoom = Math.Clamp(targetZoom - scrollDiff * ZoomStep,
                    MinZoom, MaxZoom);
            }

            if(MouseInput.IsLeftButtonPressed())
            {
                isDragging             = true;
                dragStartPosition      = MouseInput.GetPosition();
                dragCameraBasePosition = camera.Position;
            }
            else if(MouseInput.IsLeftButtonReleased())
            {
                isDragging = false;
            }
            else if(isDragging)
            {
                Vector2 diff    = dragStartPosition - MouseInput.GetPosition();
                diff           *= 1.0f / camera.Zoom;
                camera.Position = dragCameraBasePosition + diff;
            }

            camera.Zoom = MathHelper.Lerp(camera.Zoom, targetZoom, 5.0f * dt);
        }
    }
}
