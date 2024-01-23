using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Engine.Graphics;

namespace Engine.Debug
{
    public static class DebugDraw
    {
        public const string DEBUG_DEFINE = "DEBUG";

        #region Command Classes
        private enum CommandType
        {
            Point = 0,
            Line,
            Vector,
            Rectangle,
            FilledRectangle,
            Circle,
            OrientedCircle
        }

        private abstract class DrawCommand
        {
            public CommandType Type;
            public Color Color;

            public DrawCommand(CommandType type, Color color) 
            { 
                Type = type;
                Color = color;
            }
            public abstract void Draw(ShapeBatch shapeBatch, LayerData layer);
        }

        private class PointCommand : DrawCommand
        {
            public Vector2 Position;

            public PointCommand(Vector2 pos, Color color) : base(CommandType.Point, color) 
            {
                Position = pos;
            }

            public override void Draw(ShapeBatch shapeBatch, LayerData layer)
            {
                Vector2 pos = Position - new Vector2(layer.PointSize * 0.5f);
                shapeBatch.DrawFilledRectangle(pos, new Vector2(layer.PointSize),
                    Color, layer.Depth);
            }
        }

        private class LineCommand : DrawCommand
        {
            public Vector2 Start;
            public Vector2 End;

            public LineCommand(Vector2 start, Vector2 end, Color color) :
                base(CommandType.Line, color)
            {
                Start = start;
                End = end;
            }

            public override void Draw(ShapeBatch shapeBatch, LayerData layer)
            {
                shapeBatch.DrawLine(Start, End, layer.LineThickness, Color,
                    layer.Depth);
            }
        }

        private class RectangleCommand : DrawCommand
        {
            public Vector2 Position;
            public Vector2 Size;
            public Vector2 Origin;
            public float Rotation;

            public RectangleCommand(Vector2 pos, Vector2 size,
                Vector2 origin, float rotation, Color color) :
                base(CommandType.Rectangle, color)
            {
                Position = pos;
                Size     = size;
                Origin   = origin;
                Rotation = rotation;
            }

            public override void Draw(ShapeBatch shapeBatch, LayerData layer)
            {
                shapeBatch.DrawRectangle(Position, Size, layer.LineThickness, 
                    Rotation, Origin, Color, layer.Depth);
            }
        }

        private class FilledRectangleCommand : DrawCommand
        {
            public Vector2 Position;
            public Vector2 Size;
            public Vector2 Origin;
            public float Rotation;

            public FilledRectangleCommand(Vector2 pos, Vector2 size, 
                Vector2 origin, float rotation, Color color) :
                base(CommandType.FilledRectangle, color)
            {
                Position = pos;
                Size     = size;
                Origin   = origin;
                Rotation = rotation; 
            }

            public override void Draw(ShapeBatch shapeBatch, LayerData layer)
            {
                shapeBatch.DrawFilledRectangle(Position, Size, Rotation, Origin,
                    Color, layer.Depth);
            }
        }

        private class CircleCommand : DrawCommand
        {
            public Vector2 Center;
            public float Radius;

            public CircleCommand(Vector2 center, float radius,
                Color color) : base(CommandType.Circle, color)
            {
                Center = center;
                Radius = radius;
            }

            public override void Draw(ShapeBatch shapeBatch, LayerData layer)
            {
                shapeBatch.DrawCircle(Center, Radius, layer.CirclePoints,
                    layer.LineThickness, Color, layer.Depth);
            }
        }

        private class OrientedCircleCommand : DrawCommand
        {
            public Vector2 Center;
            public float Radius;
            public float Rotation;

            public OrientedCircleCommand(Vector2 center, float radius,
                float rotation, Color color) : base(CommandType.OrientedCircle, color)
            {
                Center   = center;
                Radius   = radius;
                Rotation = rotation;
            }

            public override void Draw(ShapeBatch shapeBatch, LayerData layer)
            {
                float cos = MathF.Cos(Rotation);
                float sin = MathF.Sin(Rotation);
                Vector2 end = Center + new Vector2(cos, sin) * Radius;

                shapeBatch.DrawCircle(Center, Radius, layer.CirclePoints,
                    layer.LineThickness, Color, layer.Depth);
                shapeBatch.DrawLine(Center, end, layer.LineThickness, Color,
                    layer.Depth);
            }
        }
        #endregion

        private class LayerData
        {
            public float PointSize;
            public float LineThickness;
            public float Depth;
            public int   CirclePoints;
            public bool IsEnabled;
            public List<DrawCommand> Commands;

            public LayerData()
            {
                PointSize     = 4.0f;
                LineThickness = 2.0f;
                Depth         = 0.0f;
                CirclePoints  = 16;
                IsEnabled     = true;
                Commands      = new List<DrawCommand>();
            }
        };

        private static ShapeBatch shapeBatch;
        private static Dictionary<string, LayerData> layers;
        private static LayerData mainLayer;
        public static  Camera2D Camera;

        [Conditional(DEBUG_DEFINE)]
        public static void Init(GraphicsDevice graphicsDevice)
        {
            shapeBatch = new ShapeBatch(graphicsDevice, 200);
            layers     = new Dictionary<string, LayerData>();
            mainLayer  = new LayerData();
            Camera     = null;
        }

        [Conditional(DEBUG_DEFINE)]
        public static void RegisterLayer(string layerName, float? pointSize = null, 
            float? lineThickness = null, int? circlePoints = null, float? layerDepth = null)
        {
            DebugAssert.Success(!layers.ContainsKey(layerName),
                "Cannot register layer with name {0}." +
                "Layer has already been registered", layerName);

            LayerData layer = new LayerData();
            layers.Add(layerName, layer);

            SetLayerData(layerName, pointSize, lineThickness, circlePoints, layerDepth);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void SetMainLayerEnabled(bool isEnabled)
        {
            mainLayer.IsEnabled = isEnabled;

            if (!isEnabled) mainLayer.Commands.Clear();
        }

        public static bool IsMainLayerEnabled()
        {
            return mainLayer.IsEnabled;
        }

        [Conditional(DEBUG_DEFINE)]
        public static void SetLayerEnabled(string layerName, bool isEnabled)
        {
            LayerData layer = GetLayer(layerName);
            layer.IsEnabled = isEnabled;

            if (!isEnabled) layer.Commands.Clear();
        }

        public static bool IsLayerEnabled(string layerName)
        {
            return GetLayer(layerName).IsEnabled;
        }

        [Conditional(DEBUG_DEFINE)]
        public static void SetMainLayerData(float? pointSize = null,
            float? lineThickness = null, int? circlePoints = null, float? layerDepth = null)
        {
            if (pointSize.HasValue)     mainLayer.PointSize     = pointSize.Value;
            if (layerDepth.HasValue)    mainLayer.Depth         = layerDepth.Value;
            if (circlePoints.HasValue)  mainLayer.CirclePoints  = circlePoints.Value;
            if (lineThickness.HasValue) mainLayer.LineThickness = lineThickness.Value;
        }

        [Conditional(DEBUG_DEFINE)]
        public static void SetLayerData(string layerName, float? pointSize = null, 
            float? lineThickness = null, int? circlePoints = null, float? layerDepth = null)
        {
            LayerData layer = GetLayer(layerName);
            if (pointSize.HasValue)     layer.PointSize     = pointSize.Value;
            if (layerDepth.HasValue)    layer.Depth         = layerDepth.Value;
            if (circlePoints.HasValue)  layer.CirclePoints  = circlePoints.Value;
            if (lineThickness.HasValue) layer.LineThickness = lineThickness.Value;
        }

        #region Point
        [Conditional(DEBUG_DEFINE)]
        public static void PointIf(bool condition, Vector2 point, Color color)
        {
            if (condition)
                Point(point, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void PointIf(bool condition, string layerName,
            Vector2 point, Color color)
        {
            if (condition)
                Point(layerName, point, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Point(Vector2 point, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new PointCommand(point, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Point(string layerName, Vector2 point, 
            Color color)
        {
            LayerData layer = GetLayer(layerName);

            if(layer.IsEnabled)
                layer.Commands.Add(new PointCommand(point, color));
        }
        #endregion 

        #region Line
        [Conditional(DEBUG_DEFINE)]
        public static void LineIf(bool condition, Vector2 start, Vector2 end, Color color)
        {
            if (condition)
                Line(start, end, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void LineIf(bool condition, string layerName,
            Vector2 start, Vector2 end, Color color)
        {
            if (condition)
                Line(layerName, start, end, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Line(Vector2 start, Vector2 end, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new LineCommand(start, end, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Line(string layerName, Vector2 start,
            Vector2 end, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new LineCommand(start, end, color));
        }
        #endregion

        #region Rectangle
        [Conditional(DEBUG_DEFINE)]
        public static void RectIf(bool condition, Vector2 pos, 
            Vector2 size, Color color)
        {
            if(condition)
                Rect(pos, size, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void RectIf(bool condition, Vector2 pos,
            Vector2 size, float rotation, Color color)
        {
            if (condition)
                Rect(pos, size, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void RectIf(bool condition, string layerName, Vector2 pos,
            Vector2 size, Color color)
        {
            if (condition)
                Rect(layerName, pos, size, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void RectIf(bool condition, string layerName, Vector2 pos,
            Vector2 size, float rotation, Color color)
        {
            if (condition)
                Rect(layerName, pos, size, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredRectIf(bool condition, string layerName, 
            Vector2 pos, Vector2 size, Color color)
        {
            if (condition)
                CenteredRect(layerName, pos, size, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredRectIf(bool condition, string layerName, 
            Vector2 pos, Vector2 size, float rotation, Color color)
        {
            if (condition)
                CenteredRect(layerName, pos, size, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Rect(Vector2 pos, Vector2 size, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new RectangleCommand(pos, size, Vector2.Zero,
                    0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Rect(Vector2 pos, Vector2 size, 
            float rotation, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new RectangleCommand(pos, size, Vector2.Zero,
                    rotation, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredRect(Vector2 pos, Vector2 size, Color color)
        {
            if(mainLayer.IsEnabled)
               mainLayer.Commands.Add(new RectangleCommand(pos, size, 
                    size * 0.5f, 0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredRect(Vector2 pos, Vector2 size, 
            float rotation, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new RectangleCommand(pos, size,
                    size * 0.5f, rotation, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Rect(string layerName, Vector2 pos, Vector2 size, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new RectangleCommand(pos, size, Vector2.Zero,
                    0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Rect(string layerName, Vector2 pos, Vector2 size,
            float rotation, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new RectangleCommand(pos, size, Vector2.Zero,
                    rotation, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredRect(string layerName, Vector2 pos, Vector2 size, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new RectangleCommand(pos, size,
                    size * 0.5f, 0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredRect(string layerName,Vector2 pos, Vector2 size,
            float rotation, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new RectangleCommand(pos, size,
                    size * 0.5f, rotation, color));
        }
        #endregion

        #region FilledRectangle
        [Conditional(DEBUG_DEFINE)]
        public static void FilledRectIf(bool condition, Vector2 pos,
            Vector2 size, Color color)
        {
            if (condition)
                FilledRect(pos, size, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void FilledRectIf(bool condition, Vector2 pos,
            Vector2 size, float rotation, Color color)
        {
            if (condition)
                FilledRect(pos, size, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void FilledRectIf(bool condition, string layerName, 
            Vector2 pos, Vector2 size, Color color)
        {
            if (condition)
                FilledRect(layerName, pos, size, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void FilledRectIf(bool condition, string layerName, 
            Vector2 pos, Vector2 size, float rotation, Color color)
        {
            if (condition)
                FilledRect(layerName, pos, size, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredFilledRectIf(bool condition, string layerName,
            Vector2 pos, Vector2 size, Color color)
        {
            if (condition)
                CenteredFilledRect(layerName, pos, size, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredFilledRectIf(bool condition, string layerName,
            Vector2 pos, Vector2 size, float rotation, Color color)
        {
            if (condition)
                CenteredFilledRect(layerName, pos, size, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void FilledRect(Vector2 pos, Vector2 size, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new FilledRectangleCommand(pos, size, 
                    Vector2.Zero, 0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void FilledRect(Vector2 pos, Vector2 size,
            float rotation, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new FilledRectangleCommand(pos, size, 
                    Vector2.Zero, rotation, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredFilledRect(Vector2 pos, Vector2 size, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new FilledRectangleCommand(pos, size,
                    size * 0.5f, 0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredFilledRect(Vector2 pos, Vector2 size,
            float rotation, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new FilledRectangleCommand(pos, size,
                    size * 0.5f, rotation, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void FilledRect(string layerName, Vector2 pos, 
            Vector2 size, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new FilledRectangleCommand(pos, size, 
                    Vector2.Zero, 0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void FilledRect(string layerName, Vector2 pos, Vector2 size,
            float rotation, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new FilledRectangleCommand(pos, size, Vector2.Zero,
                    rotation, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredFilledRect(string layerName, Vector2 pos, 
            Vector2 size, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new FilledRectangleCommand(pos, size,
                    size * 0.5f, 0.0f, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CenteredFilledRect(string layerName, Vector2 pos, 
            Vector2 size, float rotation, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new FilledRectangleCommand(pos, size,
                    size * 0.5f, rotation, color));
        }
        #endregion

        #region Circle
        public static void CircleIf(bool condition, Vector2 center, 
            float radius, Color color)
        {
            if (condition)
                Circle(center, radius, color);
        }

        public static void CircleIf(bool condition, Vector2 center,
            float radius, float rotation, Color color)
        {
            if (condition)
                Circle(center, radius, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CircleIf(bool condition, string layerName,
            Vector2 center, float radius, Color color)
        {
            if (condition)
                Circle(layerName, center, radius, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void CircleIf(bool condition, string layerName,
            Vector2 center, float radius, float rotation, Color color)
        {
            if (condition)
                Circle(layerName, center, radius, rotation, color);
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Circle(Vector2 center, float radius, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new CircleCommand(center, radius, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Circle(Vector2 center, float radius, 
            float rotation, Color color)
        {
            if (mainLayer.IsEnabled)
                mainLayer.Commands.Add(new OrientedCircleCommand
                    (center, radius, rotation, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Circle(string layerName, Vector2 center,
            float radius, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new CircleCommand(center, radius, color));
        }

        [Conditional(DEBUG_DEFINE)]
        public static void Circle(string layerName, Vector2 center,
            float radius, float rotation, Color color)
        {
            LayerData layer = GetLayer(layerName);

            if (layer.IsEnabled)
                layer.Commands.Add(new OrientedCircleCommand
                    (center, radius, rotation, color));
        }
        #endregion

        [Conditional(DEBUG_DEFINE)]
        public static void Draw()
        {
            if (Camera == null)
                shapeBatch.Begin();
            else
                shapeBatch.Begin(Camera);

            DrawLayer(mainLayer);
            foreach (LayerData layer in layers.Values)
            {
                DrawLayer(layer);
            }

            shapeBatch.End();
        }

        private static void DrawLayer(LayerData layer)
        {
            if (!layer.IsEnabled) return;

            foreach (DrawCommand command in layer.Commands)
            {
                command.Draw(shapeBatch, layer);
            }

            layer.Commands.Clear();
        }

        private static LayerData GetLayer(string layerName)
        {
            DebugAssert.Success(layers.ContainsKey(layerName));

            return layers[layerName];
        }
    }
}
