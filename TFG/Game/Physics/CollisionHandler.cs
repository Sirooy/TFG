using System;
using Core;
using Engine.Core;
using Microsoft.Xna.Framework;

namespace Physics
{
    public static class CollisionHandler
    {
        public static bool CircleVsCircle(Vector2 c1, float r1, Vector2 c2, float r2)
        {
            float totalRadius = r1 + r2;

            return Vector2.DistanceSquared(c1, c2) <= totalRadius * totalRadius;
        }

        public static bool CircleVsCircle(Vector2 c1, float r1, Vector2 c2, float r2, 
            out CollisionManifold manifold)
        {
            manifold          = new CollisionManifold();

            Vector2 dir       = c1 - c2;
            float distSquared = dir.LengthSquared();
            float totalRadius = r1 + r2;

            if (distSquared > totalRadius * totalRadius) return false;

            float distance = MathF.Sqrt(distSquared);
            if(distance == 0.0f)
            {
                manifold.Normal = Vector2.UnitX;
                manifold.Depth  = r1;
            }
            else
            {
                manifold.Normal = dir / distance;
                manifold.Depth  = totalRadius - distance;
            }

            manifold.Collision = true;
            return true;
        }

        public static bool RectangleVSCircle(Vector2[] vertices, 
            Vector2 center, float radius,
            out CollisionManifold manifold)
        {
            manifold = new CollisionManifold();

            float minDepth    = float.MaxValue;
            Vector2 normal    = Vector2.Zero;
            bool invertNormal = false;

            Vector2 edge1 = vertices[1] - vertices[0];
            Vector2 edge2 = vertices[2] - vertices[1];
            Vector2 n1 = new Vector2(-edge1.Y, edge1.X);
            Vector2 n2 = new Vector2(-edge2.Y, edge2.X);
            n1.Normalize();
            n2.Normalize();

            float rectMin = Vector2.Dot(n1, vertices[0]);
            float rectMax = Vector2.Dot(n1, vertices[2]);
            if (rectMin > rectMax) Util.Swap(ref rectMin, ref rectMax);

            float circleMin = Vector2.Dot(n1, center) - radius;
            float circleMax = Vector2.Dot(n1, center) + radius;
            if (circleMin > circleMax) Util.Swap(ref circleMin, ref circleMax);

            if (rectMax < circleMin || circleMax < rectMin) return false;
            else
            {
                float depth = MathF.Min(rectMax - circleMin, circleMax - rectMin);
                if (depth < minDepth)
                {
                    invertNormal = circleMax > rectMax;
                    normal = n1;
                    minDepth = depth;
                }
            }

            rectMin = Vector2.Dot(n2, vertices[2]);
            rectMax = Vector2.Dot(n2, vertices[0]);
            if (rectMin > rectMax) Util.Swap(ref rectMin, ref rectMax);

            circleMin = Vector2.Dot(n2, center) - radius;
            circleMax = Vector2.Dot(n2, center) + radius;
            if (circleMin > circleMax) Util.Swap(ref circleMin, ref circleMax);

            if (rectMax < circleMin || circleMax < rectMin) return false;
            else
            {
                float depth = MathF.Min(rectMax - circleMin, circleMax - rectMin);
                if (depth < minDepth)
                {
                    invertNormal = circleMin > rectMin;

                    normal = n2;
                    minDepth = depth;
                }
            }

            //TODO: Physics engine from scratch-> minimun traslation vector
            //TODO: Project rect vertices
            int closestIndex = GetClosestPointIndex(center, vertices);
            Vector2 n3       = vertices[closestIndex] - center;
            n3.Normalize();

            ProjectVertices(vertices, n3, out rectMin, out rectMax);

            circleMin = Vector2.Dot(n3, center) - radius;
            circleMax = Vector2.Dot(n3, center) + radius;
            if (circleMin > circleMax) Util.Swap(ref circleMin, ref circleMax);

            if (rectMax < circleMin || circleMax < rectMin) return false;
            else
            {
                float depth = MathF.Min(rectMax - circleMin, circleMax - rectMin);
                if (depth < minDepth)
                {
                    normal   = n3;
                    minDepth = depth;
                    invertNormal = false;
                }
            }

            if (invertNormal)
                normal = -normal;

            manifold.Normal = normal;
            manifold.Depth = minDepth;

            return true;
        }

        private static int GetClosestPointIndex(Vector2 p, Vector2[] vertices)
        {
            float minDist = Vector2.DistanceSquared(vertices[0], p);
            int minIndex  = 0;

            for(int i = 1; i < vertices.Length; ++i)
            {
                float dist = Vector2.DistanceSquared(vertices[i], p);
                if(dist < minDist)
                {
                    minDist  = dist;
                    minIndex = i;
                }
            }

            return minIndex;
        }

        public static void ProjectVertices(Vector2[] vertices, Vector2 normal, 
            out float min, out float max)
        {
            float proj = Vector2.Dot(vertices[0], normal);
            min = proj;
            max = proj;

            for(int i = 1;i <  vertices.Length; ++i)
            {
                proj = Vector2.Dot(vertices[i], normal);

                if (proj < min) min = proj;
                if (proj > max) max = proj;
            }
        }

        //SAT Implementation
        public static bool RectangleVsRectangle(
            Vector2 center1, Vector2[] vertices1,
            Vector2 center2, Vector2[] vertices2, 
            out CollisionManifold manifold)
        {
            manifold = new CollisionManifold();

            Vector2 normal = Vector2.Zero;
            float minDepth = float.MaxValue;

            Vector2 edge1 = vertices1[1] - vertices1[0];
            Vector2 edge2 = vertices1[2] - vertices1[1];
            Vector2 n1    = new Vector2(-edge1.Y, edge1.X);
            Vector2 n2    = new Vector2(-edge2.Y, edge2.X);
            n1.Normalize();
            n2.Normalize();

            Vector2 edge3 = vertices2[1] - vertices2[0];
            Vector2 edge4 = vertices2[2] - vertices2[1];
            Vector2 n3 = new Vector2(-edge3.Y, edge3.X);
            Vector2 n4 = new Vector2(-edge4.Y, edge4.X);
            n3.Normalize();
            n4.Normalize();

            //First edge of vertices 1
            float min1 = Vector2.Dot(n1, vertices1[0]);
            float max1 = Vector2.Dot(n1, vertices1[2]);
            if (min1 > max1) Util.Swap(ref min1, ref max1);

            GetMinAndMaxProjections(vertices2, n1, 
                out float min2, out float max2);
            if (max1 < min2 || max2 < min1 ) return false;
            else
            {
                float depth = MathF.Min(max1 - min2, max2 - min1);
                if (depth < minDepth)
                {
                    normal   = n1;
                    minDepth = depth;
                }
            }

            //Second edge of vertices 1
            min1 = Vector2.Dot(n2, vertices1[2]);
            max1 = Vector2.Dot(n2, vertices1[0]);
            if (min1 > max1) Util.Swap(ref min1, ref max1);

            GetMinAndMaxProjections(vertices2, n2,
                out min2, out max2);
            if (max1 < min2 || max2 < min1) return false;
            else
            {
                float depth = MathF.Min(max1 - min2, max2 - min1);
                if (depth < minDepth)
                {
                    normal = n2;
                    minDepth = depth;
                }
            }
            //####

            //First edge of vertices 2
            min1 = Vector2.Dot(n3, vertices2[0]);
            max1 = Vector2.Dot(n3, vertices2[2]);
            if (min1 > max1) Util.Swap(ref min1, ref max1);

            GetMinAndMaxProjections(vertices1, n3,
                out min2, out max2);
            if (max1 < min2 || max2 < min1) return false;
            else
            {
                float depth = MathF.Min(max1 - min2, max2 - min1);
                if (depth < minDepth)
                {
                    normal = -n3;
                    minDepth = depth;
                }
            }
            //Second edge of vertices 2
            min1 = Vector2.Dot(n4, vertices2[2]);
            max1 = Vector2.Dot(n4, vertices2[0]);
            if (min1 > max1) Util.Swap(ref min1, ref max1);

            GetMinAndMaxProjections(vertices1, n4,
                out min2, out max2);
            if (max1 < min2 || max2 < min1) return false;
            else
            {
                float depth = MathF.Min(max1 - min2, max2 - min1);
                if (depth < minDepth)
                {
                    normal = -n4;
                    minDepth = depth;
                }
            }

            if (Vector2.Dot(center2 - center1, normal) > 0.0f)
                normal = -normal;

            manifold.Normal = normal;
            manifold.Depth  = minDepth;

            return true;
        }

        private static void GetMinAndMaxProjections(
            Vector2[] vertices, Vector2 normal,
            out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            for(int i = 0;i < vertices.Length; ++i)
            {
                float proj = Vector2.Dot(vertices[i], normal);
                if (proj < min) min = proj;
                if (proj > max) max = proj; 
            }
        }
    }
}
