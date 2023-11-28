using System;
using System.Globalization;
using Core;
using Engine.Core;
using Microsoft.Xna.Framework;

namespace Physics
{
    public static class CollisionHandler
    {
        public static bool AABBVsAABB(in AABB a1, in AABB a2)
        {
            return a1.Right > a2.Left && a1.Left < a2.Right &&
                a1.Bottom > a2.Top && a1.Top < a2.Bottom;
        }

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

            manifold.NumContacts = 1;
            manifold.Contact1    = c1 - manifold.Normal * r1;

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

            manifold.NumContacts = 1;
            manifold.Contact1    = center + normal * (radius - minDepth);

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

        //SAT Implementation
        public static bool RectangleVsRectangle(
            Vector2 center1, Vector2[] vertices1, Vector2[] normals1,
            Vector2 center2, Vector2[] vertices2, Vector2[] normals2, 
            out CollisionManifold manifold)
        {
            manifold = new CollisionManifold();

            Vector2 normal = Vector2.Zero;
            float minDepth = float.MaxValue;

            Vector2 n1 = normals1[0];
            Vector2 n2 = normals1[1];
            Vector2 n3 = normals2[0];
            Vector2 n4 = normals2[1];

            //First edge of vertices 1
            float min1 = Vector2.Dot(n1, vertices1[0]);
            float max1 = Vector2.Dot(n1, vertices1[2]);
            //if (min1 > max1) Util.Swap(ref min1, ref max1);

            ProjectVertices(vertices2, n1, 
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
            min1 = Vector2.Dot(n2, vertices1[0]);
            max1 = Vector2.Dot(n2, vertices1[2]);
            //if (min1 > max1) Util.Swap(ref min1, ref max1);

            ProjectVertices(vertices2, n2,
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
            //if (min1 > max1) Util.Swap(ref min1, ref max1);

            ProjectVertices(vertices1, n3,
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
            min1 = Vector2.Dot(n4, vertices2[0]);
            max1 = Vector2.Dot(n4, vertices2[2]);
            //if (min1 > max1) Util.Swap(ref min1, ref max1);

            ProjectVertices(vertices1, n4,
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

            FindRectangleVsRectangleContactPoints(vertices1,
                vertices2, ref manifold);

            return true;
        }

        private static void FindRectangleVsRectangleContactPoints(
            Vector2[] vertices1,
            Vector2[] vertices2,
            ref CollisionManifold manifold)
        {
            manifold.NumContacts = 1;
            float minDistSquared = float.MaxValue;

            for(int i = 0;i < vertices1.Length; ++i)
            {
                Vector2 vertex = vertices1[i];

                for(int j = 0;j < vertices2.Length; ++j)
                {
                    ClosestPointToLineSegment(vertex, vertices2[j],
                        vertices2[(j + 1) % vertices2.Length],
                        out float distSquared, out Vector2 point);

                    if(NearlyEqual(minDistSquared, distSquared))
                    {
                        if(!NearlyEqual(point, manifold.Contact1))
                        {
                            manifold.NumContacts = 2;
                            manifold.Contact2 = point;
                        }
                    }
                    else if(distSquared <  minDistSquared)
                    {
                        manifold.Contact1 = point;
                        minDistSquared = distSquared;
                    }
                }
            }

            for (int i = 0; i < vertices2.Length; ++i)
            {
                Vector2 vertex = vertices2[i];

                for (int j = 0; j < vertices1.Length; ++j)
                {
                    ClosestPointToLineSegment(vertex, vertices1[j],
                        vertices1[(j + 1) % vertices1.Length],
                        out float distSquared, out Vector2 point);

                    if (NearlyEqual(minDistSquared, distSquared))
                    {
                        if (!NearlyEqual(point, manifold.Contact1))
                        {
                            manifold.NumContacts = 2;
                            manifold.Contact2 = point;

                        }
                    }
                    else if (distSquared < minDistSquared)
                    {
                        manifold.Contact1 = point;
                        minDistSquared = distSquared;
                    }
                }
            }
        }

        private static bool NearlyEqual(float a, float b)
        {
            const float EPSILON = 0.05f;

            return MathF.Abs(a - b) < EPSILON;
        }

        private static bool NearlyEqual(Vector2 a, Vector2 b) 
        {
            return NearlyEqual(a.X, b.X) && NearlyEqual(a.Y, b.Y);
        }

        private static void FindRectangleVsRectangleContactPoints1(
            Vector2[] vertices1,
            Vector2[] vertices2, 
            ref CollisionManifold manifold)
        {
            //Normal alwais points from object 2 to object 1
            int v1Index = FindFurthestVertexIndexAlongNormal(vertices1, 
                -manifold.Normal); 
            int v2Index = FindFurthestVertexIndexAlongNormal(vertices2,
                manifold.Normal);
        }

        //The significant face must satisfy:
        //* The face includes the selected vertex
        //* The face normal is the most parallel with the collision normal
        private static int FindSignificantFace(Vector2[] vertices, 
            Vector2 normal, int selectedVertex)
        {
            int nextVertex = (selectedVertex + 1) % vertices.Length;

            int prevVertex = selectedVertex - 1;
            if (prevVertex < 0) prevVertex = vertices.Length - 1;

            Vector2 edge1       = vertices[selectedVertex] - vertices[prevVertex];
            Vector2 edge2       = vertices[nextVertex] - vertices[selectedVertex];
            Vector2 faceNormal1 = Vector2.Normalize(new Vector2(-edge1.Y, edge1.X));
            Vector2 faceNormal2 = Vector2.Normalize(new Vector2(-edge2.Y, edge2.X));

            if (Vector2.Dot(faceNormal1, normal) > Vector2.Dot(faceNormal2, normal))
                return prevVertex;
            else
                return nextVertex;
        }



        private static int FindFurthestVertexIndexAlongNormal(Vector2[] vertices, 
            Vector2 normal)
        {
            int index     = 0;
            float maxProj = Vector2.Dot(vertices[0], normal);

            for(int i = 1;i < vertices.Length; ++i)
            {
                float proj = Vector2.Dot(vertices[i], normal);

                if(proj > maxProj)
                {
                    index   = i;
                    maxProj = proj;
                }
            }

            return index;
        }

        public static void ClosestPointToLineSegment(Vector2 point, 
            Vector2 start, Vector2 end, out float distSquared, out Vector2 result)
        {
            Vector2 se = end - start;
            Vector2 sp = point - start;

            float t = Vector2.Dot(se, sp) / se.LengthSquared();
            t       = Math.Clamp(t, 0.0f, 1.0f);

            result      = start + se * t;
            distSquared = Vector2.DistanceSquared(point, result);
        }

        public static void ProjectVertices(Vector2[] vertices, Vector2 normal,
            out float min, out float max)
        {
            float proj = Vector2.Dot(vertices[0], normal);
            min = proj;
            max = proj;

            for (int i = 1; i < vertices.Length; ++i)
            {
                proj = Vector2.Dot(vertices[i], normal);

                if (proj < min) min = proj;
                if (proj > max) max = proj;
            }
        }
    }
}
