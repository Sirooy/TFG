using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Graphics;


namespace Engine.Debug
{
    public static class DebugDraw
    {
        public const string DEFINE = "DEBUG";

        #region Command Classes
        private enum CommandType
        {
            Point = 0,
            Line,
            Vector,

        }

        private abstract class DrawCommand
        {
            public CommandType Type;

            public DrawCommand(CommandType type) { Type = type; }
            public abstract void Draw(ShapeBatch shapeBatch, LayerData layer);
        }

        private class PointCommand : DrawCommand
        {
            public Vector2 Position;
            public PointCommand(Vector2 pos) : base(CommandType.Point) 
            {
                Position = pos;
            }

            public override void Draw(ShapeBatch shapeBatch, LayerData layer)
            {
                //shapeBatch.DrawRectangle()
            }
        }
        #endregion

        private class LayerData
        {
            public float PointSize;
            public float LineThickness;
            public float LayerDepth;
            public bool IsDisabled;
            public List<DrawCommand> Commands;

            public LayerData()
            {
                PointSize     = 2.0f;
                LineThickness = 2.0f;
                LayerDepth    = 0.0f;
                IsDisabled    = false;
                Commands      = new List<DrawCommand>();
            }
        };

        private static ShapeBatch shapeBatch;
        private static Dictionary<string, LayerData> layers;

        [Conditional(DEFINE)]
        public static void Init(GraphicsDevice graphicsDevice)
        {
            shapeBatch = new ShapeBatch(graphicsDevice, 200);
            layers     = new Dictionary<string, LayerData>();
        }

        [Conditional(DEFINE)]
        public static void Point(string layerName, Vector2 point, 
            Color color)
        {

        }

        [Conditional(DEFINE)]
        public static void Line(string layerName, Vector2 start, Vector2 end, 
            Color color)
        {

        }

        [Conditional(DEFINE)]
        public static void Vector(string layerName, Vector2 start, Vector2 dir, 
            Color color)
        {

        }

        [Conditional(DEFINE)]
        public static void EnableLayer(string layerName)
        {

        }

        [Conditional(DEFINE)]
        public static void DisableLayer(string layerName) 
        { 
        
        }

        [Conditional(DEFINE)]
        public static void Draw(Camera2D camera)
        {
            shapeBatch.Begin(camera);
            
            foreach(LayerData layer in layers.Values)
            {
                if (layer.IsDisabled) continue;

                

                layer.Commands.Clear();
            }

            shapeBatch.End();
        }

        private static LayerData GetLayer(string layerName)
        {
            if(!layers.TryGetValue(layerName, out LayerData layer))
            {
                layer = new LayerData();
                layers.Add(layerName, layer);
            }

            return layer;
        }
    }
}
