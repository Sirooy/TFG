using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Graphics;

namespace Engine.Debug
{
    public static class DebugDraw
    {
        public class LayerData
        {
            public float PointSize;
            public float LineThickness;
            public float LayerDepth;
            public bool IsDisabled;

            public LayerData()
            {
                PointSize     = 2.0f;
                LineThickness = 2.0f;
                LayerDepth    = 0.0f;
                IsDisabled    = false;
            }
        };

        private static ShapeBatch shapeBatch;
        private static Dictionary<string, LayerData> layers;

        [Conditional("DEBUG")]
        public static void Init(GraphicsDevice graphicsDevice)
        {
            shapeBatch = new ShapeBatch(graphicsDevice, 200);
            layers     = new Dictionary<string, LayerData>();
        }

        [Conditional("DEBUG")]
        public static void Point(string layerName, Vector2 point, 
            Color color)
        {

        }

        [Conditional("DEBUG")]
        public static void Line(string layerName, Vector2 start, Vector2 end, 
            Color color)
        {

        }

        [Conditional("DEBUG")]
        public static void Vector(string layerName, Vector2 start, Vector2 dir, 
            Color color)
        {

        }

        [Conditional("DEBUG")]
        public static void EnableLayer(string layerName)
        {

        }

        [Conditional("DEBUG")]
        public static void DisableLayer(string layerName) 
        { 
        
        }

        [Conditional("DEBUG")]
        public static void Draw()
        {
            foreach(LayerData layer in layers.Values)
            {
                if (layer.IsDisabled) continue;
            }
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
