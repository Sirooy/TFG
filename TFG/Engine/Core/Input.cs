using Engine.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core
{
    public static class KeyboardInput
    {
        private static KeyboardState keyboardState     = default;
        private static KeyboardState lastKeyboardState = default;
        public static KeyboardState State { get { return keyboardState; } }

        public static void Update()
        {
            lastKeyboardState = keyboardState;
            keyboardState     = Keyboard.GetState();
        }

        public static bool IsKeyDown(Keys key)
        {
            return keyboardState.IsKeyDown(key);
        }

        public static bool IsKeyPressed(Keys key)
        {
            return keyboardState.IsKeyDown(key) &&
                !lastKeyboardState.IsKeyDown(key);
        }

        public static bool IsKeyUp(Keys key)
        {
            return keyboardState.IsKeyUp(key);
        }

        public static bool IsKeyReleased(Keys key)
        {
            return keyboardState.IsKeyUp(key) &&
                !lastKeyboardState.IsKeyUp(key);
        }
    };

    public static class MouseInput
    {
        private static MouseState mouseState     = default;
        private static MouseState lastMouseState = default;

        public static Vector2 LastPosition
            { get { return lastMouseState.Position.ToVector2(); } }
        public static float X 
            { get { return mouseState.X; } }
        public static float Y 
            { get { return mouseState.Y; } }
        public static float LastX
            { get { return lastMouseState.X; } }
        public static float LastY
            { get { return lastMouseState.Y; } }
        public static int ScrollValue 
            { get { return mouseState.ScrollWheelValue; } }
        public static int ScrollValueDiff
            { get { return lastMouseState.ScrollWheelValue - 
                        mouseState.ScrollWheelValue; } }
        public static int HorScrollValue 
            { get { return mouseState.ScrollWheelValue; } }
        public static int HorScrollValueDiff
            { get { return lastMouseState.ScrollWheelValue -
                        mouseState.ScrollWheelValue; } }

        public static void Update()
        {
            lastMouseState = mouseState;
            mouseState     = Mouse.GetState();
        }

        public static Vector2 GetPosition()
        {
            return mouseState.Position.ToVector2();
        }

        public static Vector2 GetPosition(RenderScreen screen)
        {
            return screen.WindowToScreenCoords(
                mouseState.Position.ToVector2());
        }

        public static Vector2 GetPosition(Camera2D camera)
        {
            return camera.WindowToCameraCoords(
                mouseState.Position.ToVector2());
        }

        public static Vector2 GetLastPosition()
        {
            return lastMouseState.Position.ToVector2();
        }

        public static Vector2 GetLastPosition(RenderScreen screen)
        {
            return screen.WindowToScreenCoords(
                lastMouseState.Position.ToVector2());
        }

        public static Vector2 GetLastPosition(Camera2D camera)
        {
            return camera.WindowToCameraCoords(
                lastMouseState.Position.ToVector2());
        }

        public static bool ScrollHasChanged()
        {
            return mouseState.ScrollWheelValue != 
                lastMouseState.ScrollWheelValue;
        }

        #region Left Button
        public static bool IsLeftButtonDown()
        {
            return mouseState.LeftButton == ButtonState.Pressed;
        }

        public static bool IsLeftButtonPressed()
        {
            return mouseState.LeftButton == ButtonState.Pressed &&
                lastMouseState.LeftButton == ButtonState.Released;
        }

        public static bool IsLeftButtonUp()
        {
            return mouseState.LeftButton == ButtonState.Released;
        }

        public static bool IsLeftButtonReleased()
        {
            return mouseState.LeftButton == ButtonState.Released &&
                lastMouseState.LeftButton == ButtonState.Pressed;
        }
        #endregion

        #region Middle Button
        public static bool IsMiddleButtonDown()
        {
            return mouseState.MiddleButton == ButtonState.Pressed;
        }

        public static bool IsMiddleButtonPressed()
        {
            return mouseState.MiddleButton == ButtonState.Pressed &&
                lastMouseState.MiddleButton == ButtonState.Released;
        }

        public static bool IsMiddleButtonUp()
        {
            return mouseState.MiddleButton == ButtonState.Released;
        }

        public static bool IsMiddleButtonReleased()
        {
            return mouseState.MiddleButton == ButtonState.Released &&
                lastMouseState.MiddleButton == ButtonState.Pressed;
        }
        #endregion

        #region Right Button
        public static bool IsRightButtonDown()
        {
            return mouseState.RightButton == ButtonState.Pressed;
        }

        public static bool IsRightButtonPressed()
        {
            return mouseState.RightButton == ButtonState.Pressed &&
                lastMouseState.RightButton == ButtonState.Released;
        }

        public static bool IsRightButtonUp()
        {
            return mouseState.RightButton == ButtonState.Released;
        }

        public static bool IsRightButtonReleased()
        {
            return mouseState.RightButton == ButtonState.Released &&
                lastMouseState.RightButton == ButtonState.Pressed;
        }
        #endregion
    };

    public static class Input
    {
        public static void Update()
        {
            KeyboardInput.Update();
            MouseInput.Update();
        }
    }
}
