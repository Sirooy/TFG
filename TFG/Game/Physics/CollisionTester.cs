using System;
using Cmps;
using Core;
using Engine.Core;
using Microsoft.Xna.Framework;

namespace Physics
{
    public static class CollisionTester
    {
        #region Collision Jump Table
        private delegate bool CheckCollisionFunc(
            Vector2 pos1, ColliderShape collider1,
            Vector2 pos2, ColliderShape collider2,
            out Manifold manifold);

        private static readonly CheckCollisionFunc[,] CheckCollisionTable
            = new CheckCollisionFunc
            [(int)ColliderShapeType.MaxTypes,
             (int)ColliderShapeType.MaxTypes]
        {
            { CircleVsCircleTableFunc, CircleVsRectTableFunc },
            { RectVsCircleTableFunc,   RectVsRectTableFunc   }
        };

        public static bool Collides(Vector2 pos1, ColliderShape collider1,
            Vector2 pos2, ColliderShape collider2, out Manifold manifold)
        {
            int index1 = (int)collider1.Type;
            int index2 = (int)collider2.Type;

            return CheckCollisionTable[index1, index2]
                (pos1, collider1, pos2, collider2, out manifold);
        }

        private static bool CircleVsCircleTableFunc(Vector2 pos1, ColliderShape collider1,
            Vector2 pos2, ColliderShape collider2, out Manifold manifold)
        {
            CircleCollider circle1 = (CircleCollider)collider1;
            CircleCollider circle2 = (CircleCollider)collider2;

            return CollisionTester.CircleVsCircle(
                pos1, circle1.CachedRadius,
                pos2, circle2.CachedRadius,
                out manifold);
        }

        private static bool CircleVsRectTableFunc(Vector2 pos1, ColliderShape collider1,
            Vector2 pos2, ColliderShape collider2, out Manifold manifold)
        {
            CircleCollider circle  = (CircleCollider)    collider1;
            RectangleCollider rect = (RectangleCollider) collider2;

            bool ret = CollisionTester.RectangleVSCircle(
                pos2, rect.Vertices, rect.Normals,
                pos1, circle.CachedRadius,
                out manifold);

            manifold.Normal = -manifold.Normal;

            return ret;
        }

        private static bool RectVsCircleTableFunc(Vector2 pos1, ColliderShape collider1,
            Vector2 pos2, ColliderShape collider2, out Manifold manifold)
        {
            RectangleCollider rect = (RectangleCollider) collider1;
            CircleCollider circle  = (CircleCollider)    collider2;

            return CollisionTester.RectangleVSCircle(
                pos1, rect.Vertices, rect.Normals,
                pos2, circle.CachedRadius,
                out manifold);
        }

        private static bool RectVsRectTableFunc(Vector2 pos1, ColliderShape collider1,
            Vector2 pos2, ColliderShape collider2, out Manifold manifold)
        {
            RectangleCollider rect1 = (RectangleCollider) collider1;
            RectangleCollider rect2 = (RectangleCollider) collider2;

            return CollisionTester.RectangleVsRectangle(
                pos1, rect1.Vertices, rect1.Normals,
                pos2, rect2.Vertices, rect2.Normals,
                out manifold);
        }
        #endregion Collision Jump Table

        #region Raycast Jump Table

        private delegate bool CheckRaycastFunc(
            Vector2 rayStart, Vector2 rayDir,
            Vector2 pos, ColliderShape collider,
            out float distance);

        private static readonly CheckRaycastFunc[] CheckRaycastTable
            = new CheckRaycastFunc[(int)ColliderShapeType.MaxTypes]
            { 
                RayVsCircleTableFunc, 
                RayVsRectTableFunc 
            };

        public static bool RayCollides(Vector2 rayStart, Vector2 rayDir,
            Vector2 pos, ColliderShape collider, out float distance)
        {
            int index = (int)collider.Type;

            return CheckRaycastTable[index]
                (rayStart, rayDir, pos, collider, out distance);
        }

        private static bool RayVsCircleTableFunc(
            Vector2 rayStart, Vector2 rayDir,
            Vector2 pos, ColliderShape collider,
            out float distance)
        {
            CircleCollider circle = (CircleCollider) collider;

            return RayVsCircle(rayStart, rayDir, 
                pos, circle.Radius, out distance);
        }

        private static bool RayVsRectTableFunc(
            Vector2 rayStart, Vector2 rayDir,
            Vector2 _, ColliderShape collider,
            out float distance)
        {
            RectangleCollider rect = (RectangleCollider)collider;

            return RayVsRectangle(rayStart, rayDir,
                rect.Vertices, rect.Normals, out distance);
        }

        #endregion Raycast Jump Table

        #region Basic Overlaps
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
        #endregion

        public static bool CircleVsCircle(Vector2 c1, float r1, Vector2 c2, float r2, 
            out Manifold manifold)
        {
            manifold          = new Manifold();

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

        //SAT Algorithm optimized for rect vs circle
        public static bool RectangleVSCircle(
            Vector2 rectCenter, Vector2[] rectVertices, Vector2[] rectNormals, 
            Vector2 circleCenter, float circleRadius, out Manifold m)
        {
            m = new Manifold();

            //Check overlap along the rect normals
            if(FindRectVsCircleOverlapAlongNormal(rectVertices, 
                circleCenter, circleRadius, rectNormals[0], ref m) &&
                FindRectVsCircleOverlapAlongNormal(rectVertices, 
                circleCenter, circleRadius, rectNormals[1], ref m))
            {
                //Find the closest vertex from the rect to the circle
                //and calculate a normal 
                int closestIndex     = GetClosestPointIndex(circleCenter, rectVertices);
                Vector2 circleNormal = rectVertices[closestIndex] - circleCenter;
                if (circleNormal == Vector2.Zero)
                    circleNormal = Vector2.UnitX;
                else
                    circleNormal.Normalize();

                //Test overlap with the calculated normal 
                if (FindCircleVsRectOverlapAlongNormal(circleCenter, circleRadius,
                    rectVertices, circleNormal, ref m))
                {
                    if (Vector2.Dot(rectCenter - circleCenter, m.Normal) < 0.0f)
                        m.Normal = -m.Normal;

                    m.NumContacts = 1;
                    m.Contact1    = circleCenter + m.Normal * (circleRadius - m.Depth);

                    return true;
                }
            }

            return false;
        }

        //Returns true if the rect and the circle are overlapping in the given normal
        private static bool FindRectVsCircleOverlapAlongNormal(Vector2[] rectVertices,
            Vector2 circleCenter, float circleRadius, Vector2 normal, ref Manifold manifold)
        {
            //The normal param is the normal of one of the faces of the vertices
            //of the rect, so the first min and max values are guaranteed
            //to be correct without having to project every single vertex
            float rectMin = Vector2.Dot(rectVertices[0], normal);
            float rectMax = Vector2.Dot(rectVertices[2], normal);

            float circleProj = Vector2.Dot(circleCenter, normal);
            float circleMin  = circleProj - circleRadius;
            float circleMax  = circleProj + circleRadius;

            if (rectMax < circleMin || circleMax < rectMin) return false;

            float depth = MathF.Min(rectMax - circleMin, circleMax - rectMin);
            if (depth < manifold.Depth)
            {
                manifold.Normal = normal;
                manifold.Depth  = depth;
            }

            return true;
        }

        //Returns true if the circle and the rect are overlapping in the given normal
        private static bool FindCircleVsRectOverlapAlongNormal(Vector2 circleCenter, 
            float circleRadius, Vector2[] rectVertices, Vector2 normal, ref Manifold manifold)
        {
            //The normal param is an arbitrary normal, so every vertex of the
            //rect must be projected
            ProjectVertices(rectVertices, normal, out float rectMin, out float rectMax);

            float circleProj = Vector2.Dot(circleCenter, normal);
            float circleMin = circleProj - circleRadius;
            float circleMax = circleProj + circleRadius;

            if (rectMax < circleMin || circleMax < rectMin) return false;

            float depth = MathF.Min(rectMax - circleMin, circleMax - rectMin);
            if (depth < manifold.Depth)
            {
                manifold.Normal = normal;
                manifold.Depth = depth;
            }

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

        //SAT Algorithm optimized for rect vs rect
        public static bool RectangleVsRectangle(
            Vector2 center1, Vector2[] vertices1, Vector2[] normals1,
            Vector2 center2, Vector2[] vertices2, Vector2[] normals2, 
            out Manifold m)
        {
            m = new Manifold();

            //Return true if there is overlap in every normal
            if(FindRectVsRectOverlapAlongNormal(vertices1, vertices2, normals1[0], ref m) &&
               FindRectVsRectOverlapAlongNormal(vertices1, vertices2, normals1[1], ref m) &&
               FindRectVsRectOverlapAlongNormal(vertices2, vertices1, normals2[0], ref m) &&
               FindRectVsRectOverlapAlongNormal(vertices2, vertices1, normals2[1], ref m))
            {
                //Check if the calculated normal is pointing in the right direction
                if (Vector2.Dot(center2 - center1, m.Normal) > 0.0f)
                    m.Normal = -m.Normal;

                FindRectangleVsRectangleContactPoints(vertices1, vertices2, ref m);

                return true;
            }

            return false;
        }

        //Returns true if two rects are overlapping in a given axis
        private static bool FindRectVsRectOverlapAlongNormal(Vector2[] vertices1,
            Vector2[] vertices2, Vector2 normal, ref Manifold manifold)
        {
            //The normal param is the normal of one of the faces of the vertices
            //of the first rect, so the first min and max values are guaranteed
            //to be correct without having to project every single vertex
            float min1 = Vector2.Dot(vertices1[0], normal);
            float max1 = Vector2.Dot(vertices1[2], normal);

            ProjectVertices(vertices2, normal,
                out float min2, out float max2);

            if (max1 < min2 || max2 < min1) return false;
            
            float depth = MathF.Min(max1 - min2, max2 - min1);
            if (depth < manifold.Depth)
            {
                manifold.Normal = normal;
                manifold.Depth  = depth;
            }

            return true;
        }

        private static void FindRectangleVsRectangleContactPoints(
            Vector2[] vertices1, Vector2[] vertices2, ref Manifold m)
        {
            //Invert the normal because it always points from rect 2 to rect 1
            FindSignificantFace(vertices1, -m.Normal, 
                out int v1Index1, out int v1Index2);
            FindSignificantFace(vertices2, m.Normal, 
                out int v2Index1, out int v2Index2);

            Vector2 face1 = vertices1[v1Index1] - vertices1[v1Index2];
            Vector2 face2 = vertices2[v2Index1] - vertices2[v2Index2];
            //Test wheter the first vertices are the reference face or the 
            //incident face and perform clipping
            if (MathF.Abs(Vector2.Dot(Vector2.Normalize(face1), m.Normal)) < 
                MathF.Abs(Vector2.Dot(Vector2.Normalize(face2), m.Normal)))
            {
                //face of vertices 1 is the reference face
                ClipRectangle(v1Index1, v1Index2, v2Index1, v2Index2, 
                    vertices1, vertices2, ref m);
            }
            else
            {
                //face of vertices 2 is the reference face
                ClipRectangle(v2Index1, v2Index2, v1Index1, v1Index2, 
                    vertices2, vertices1, ref m);
            }
        }

        private static void ClipRectangle(int refFaceIndex1, int refFaceIndex2,
            int incFaceIndex1, int incFaceIndex2, 
            Vector2[] refVertices, Vector2[] incVertices, ref Manifold m)
        {
            Vector2 incStart = incVertices[incFaceIndex1];
            Vector2 incEnd   = incVertices[incFaceIndex2];

            int nextRefIndex = (refFaceIndex2 + 1) % refVertices.Length;
            int prevRefIndex = (refFaceIndex1 - 1);
            if (prevRefIndex < 0) prevRefIndex = refVertices.Length - 1;

            float t1 = GetLineIntersection(incStart, incEnd,
                refVertices[prevRefIndex], refVertices[refFaceIndex1]);
            float t2 = GetLineIntersection(incStart, incEnd,
                refVertices[refFaceIndex2], refVertices[nextRefIndex]);
            t1 = Math.Clamp(t1, 0.0f, 1.0f);
            t2 = Math.Clamp(t2, 0.0f, 1.0f);

            Span<Vector2> contacts = stackalloc Vector2[2];
            contacts[0] = incStart + (incEnd - incStart) * t1;
            contacts[1] = incStart + (incEnd - incStart) * t2;

            Vector2 refFace   = refVertices[refFaceIndex2] - refVertices[refFaceIndex1];
            Vector2 refNormal = MathUtil.Rotate90(refFace);

            m.NumContacts    = 0;
            if (Vector2.Dot(contacts[0] - refVertices[refFaceIndex1], refNormal) <= 0.0f)
            {
                m.NumContacts++;
                m.Contact1 = contacts[0];
            }

            if (Vector2.Dot(contacts[1] - refVertices[refFaceIndex1], refNormal) <= 0.0f)
            {
                m.NumContacts++;
                if (m.NumContacts > 1)
                    m.Contact2 = contacts[1];
                else
                    m.Contact1 = contacts[1];
            }
        }

        //The intersection is calculated converting the system of equations into
        //a matrix and calculating the inverse
        public static float GetLineIntersection(Vector2 start1, Vector2 end1, 
            Vector2 start2, Vector2 end2)
        {
            Vector2 dir1   = end1 - start1;
            Vector2 dir2   = end2 - start2;
            Vector2 diff12 = start1 - start2;
            float det      = dir2.Y * dir1.X - dir2.X * dir1.Y; //dir2.X * -dir1.Y - dir2.Y * -dir1.X 

            return (dir2.X * diff12.Y - dir2.Y * diff12.X) / det; 
        }

        public static bool GetLineIntersection(Vector2 start1, Vector2 end1,
            Vector2 start2, Vector2 end2, out float t)
        {
            Vector2 dir1 = end1 - start1;
            Vector2 dir2 = end2 - start2;
            Vector2 diff12 = start1 - start2;
            float det = dir2.X * -dir1.Y + dir2.Y * dir1.X;

            if (det == 0.0f)
            {
                t = 0.0f;
                return false;
            }

            t = (dir2.X * diff12.Y - dir2.Y * diff12.X) / det;
            return true;
        }

        //The significant face must satisfy:
        //* The face includes the selected vertex
        //* The face normal is the most parallel with the collision normal
        private static void FindSignificantFace(Vector2[] vertices, 
            Vector2 normal, out int vertex1, out int vertex2)
        {
            int selectedVertex = FindFurthestVertexIndexAlongNormal(vertices, normal);
            int nextVertex = (selectedVertex + 1) % vertices.Length;

            int prevVertex = selectedVertex - 1;
            if (prevVertex < 0) prevVertex = vertices.Length - 1;

            Vector2 edge1       = vertices[selectedVertex] - vertices[prevVertex];
            Vector2 edge2       = vertices[nextVertex] - vertices[selectedVertex];
            Vector2 faceNormal1 = Vector2.Normalize(MathUtil.Rotate90(edge1));
            Vector2 faceNormal2 = Vector2.Normalize(MathUtil.Rotate90(edge2));

            if (Vector2.Dot(faceNormal1, normal) > Vector2.Dot(faceNormal2, normal))
            {
                vertex1 = prevVertex;
                vertex2 = selectedVertex;
            }
            else
            {
                vertex1 = selectedVertex;
                vertex2 = nextVertex;
            }
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

        public static bool RayVsCircle(Vector2 rayStart, Vector2 rayDir, 
            Vector2 circleCenter, float circleRadius, out float distance)
        {
            float radiusSquared = circleRadius * circleRadius;
            Vector2 rayToCircle = circleCenter - rayStart;

            //Ray is inside the circle
            if(rayToCircle.LengthSquared() <= radiusSquared)
            {
                distance = 0.0f;
                return true;
            }

            //Project the vector from the ray start to the circle center and check
            //if the point is inside the circle
            float projection    = Vector2.Dot(rayToCircle, rayDir);
            if(projection < 0.0f)
            {
                distance = 0.0f;
                return false;
            }

            Vector2 point       = rayStart + rayDir * projection;
            float distSquared   = Vector2.DistanceSquared(point, circleCenter);
            if (distSquared > radiusSquared)
            {
                distance = 0.0f;
                return false;
            }

            distance = projection - MathF.Sqrt(radiusSquared - distSquared);
            return true;
        }

        public static bool RayVsRectangle(Vector2 rayStart, Vector2 rayDir,
            Vector2[] rectVertices, Vector2[] rectNormals, out float distance)
        {
            distance = 0.0f;

            //First normal points to the "right", second points "down"
            Vector2 projDir = new Vector2(
                Vector2.Dot(rayDir, rectNormals[0]),
                Vector2.Dot(rayDir, rectNormals[1]));
            Vector2 projPos = new Vector2(
                Vector2.Dot(rayStart, rectNormals[0]),
                Vector2.Dot(rayStart, rectNormals[1]));

            Vector2 rectMax = new Vector2(
                Vector2.Dot(rectVertices[2], rectNormals[0]),
                Vector2.Dot(rectVertices[2], rectNormals[1]));
            Vector2 rectMin = new Vector2(
                Vector2.Dot(rectVertices[0], rectNormals[0]),
                Vector2.Dot(rectVertices[0], rectNormals[1]));

            //The ray can only collide in the X direction
            if (projDir.Y == 0.0f)
            {
                //projDir should be 1,-1 so the division can be avoided
                float min = (rectMin.X - projPos.X) * projDir.X;
                float max = (rectMax.X - projPos.X) * projDir.X;
                if (min > max) Util.Swap(ref min, ref max);

                if(projPos.Y >= rectMin.Y && 
                   projPos.Y <= rectMax.Y && 
                   max >= 0.0f)
                {
                    distance = MathF.Max(min, 0.0f);
                    return true;
                }

                return false;
            }
            //The ray can only collide in the Y direction
            else if(projDir.X == 0.0f)
            {
                //projDir should be 1,-1 so the division can be avoided
                float min = (rectMin.Y - projPos.Y) * projDir.Y;
                float max = (rectMax.Y - projPos.Y) * projDir.Y;
                if (min > max) Util.Swap(ref min, ref max);

                if (projPos.X >= rectMin.X &&
                    projPos.X <= rectMax.X &&
                    max >= 0.0f)
                {
                    distance = MathF.Max(min, 0.0f);
                    return true;
                }

                return false;
            }
            else
            {
                float tx0 = (rectMax.X - projPos.X) / projDir.X;
                float tx1 = (rectMin.X - projPos.X) / projDir.X;
                float ty0 = (rectMax.Y - projPos.Y) / projDir.Y;
                float ty1 = (rectMin.Y - projPos.Y) / projDir.Y;

                float tMin = MathF.Max(MathF.Min(tx0, tx1), MathF.Min(ty0, ty1));
                float tMax = MathF.Min(MathF.Max(tx0, tx1), MathF.Max(ty0, ty1));

                //Ray is pointing to the opossite direction
                if (tMax < 0.0f)
                    return false;
                //Ray is inside
                if (tMin < 0.0f)
                    return true;
                //Ray does not intersect
                if (tMin > tMax)
                    return false;

                distance = tMin;
                return true;
            }
        }

        public static void ClosestPointToLineSegment(Vector2 start, Vector2 end, 
            Vector2 point, out float distSquared, out Vector2 result)
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
