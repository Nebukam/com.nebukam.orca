// Copyright (c) 2021 Timothé Lapetite - nebukam@gmail.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Nebukam.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Nebukam.JobAssist.Extensions;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    [BurstCompile]
    public struct Segment2D
    {

        public static Segment2D zero = new Segment2D();

        public float2 A, B;

        public Segment2D(float2 a, float2 b) { A = a; B = b; }

        public float2 normal
        {
            get
            {
                float3 n = cross(float3(B.x - A.x, B.y - A.y, 0f), float3(A.x - A.x, A.y - A.y, 0f));
                return float2(n.x, n.y);
            }
        }

        /// <summary>
        /// Check if this segment is intersecting with another given segment, on the XY (2D) plane,
        /// Faster alternative, since the intersection point is not required.
        /// </summary>
        /// <param name="segment"></param>
        /// <returns></returns>
        public bool IsIntersecting(Segment2D segment)
        {

            float2 A2 = segment.A, B2 = segment.B;

            var d = (B.x - A.x) * (B2.y - A2.y) - (B.y - A.y) * (B2.x - A2.x);

            if (d == 0.0f)
                return false;

            float u = ((A2.x - A.x) * (B2.y - A2.y) - (A2.y - A.y) * (B2.x - A2.x)) / d;
            float v = ((A2.x - A.x) * (B.y - A.y) - (A2.y - A.y) * (B.x - A.x)) / d;

            if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
                return false;

            return true;
        }

        public bool IsIntersecting(Segment2D segment, out float2 intersection)
        {

            intersection = float2(0f);
            float2 A2 = segment.A, B2 = segment.B;

            var d = (B.x - A.x) * (B2.y - A2.y) - (B.y - A.y) * (B2.x - A2.x);

            if (d == 0.0f)
                return false;

            float u = ((A2.x - A.x) * (B2.y - A2.y) - (A2.y - A.y) * (B2.x - A2.x)) / d;
            float v = ((A2.x - A.x) * (B.y - A.y) - (A2.y - A.y) * (B.x - A.x)) / d;

            if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
                return false;

            intersection.x = A.x + u * (B.x - A.x);
            intersection.y = A.y + u * (B.y - A.y);

            return true;
        }

    }

    [BurstCompile]
    public struct RaycastsJob : IJobParallelFor
    {

        const float EPSILON = 0.00001f;

        [ReadOnly]
        public AxisPair m_plane;

        [ReadOnly]
        public float m_maxAgentRadius;

        [ReadOnly]
        public NativeArray<RaycastData> m_inputRaycasts;

        [ReadOnly]
        public NativeArray<AgentData> m_inputAgents;
        [ReadOnly]
        public NativeArray<AgentTreeNode> m_inputAgentTree;

        [ReadOnly]
        public NativeArray<ObstacleInfos> m_staticObstacleInfos;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_staticRefObstacles;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_staticObstacles;
        [ReadOnly]
        public NativeArray<ObstacleTreeNode> m_staticObstacleTree;

        [ReadOnly]
        public NativeArray<ObstacleInfos> m_dynObstacleInfos;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_dynRefObstacles;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_dynObstacles;
        [ReadOnly]
        public NativeArray<ObstacleTreeNode> m_dynObstacleTree;

        public NativeArray<RaycastResult> m_results;

        public void Execute(int index)
        {

            RaycastData raycast = m_inputRaycasts[index];
            RaycastResult result = new RaycastResult()
            {
                hitAgent = -1,
                hitObstacle = -1,
                dynamicObstacle = false
            };

            RaycastFilter filter = raycast.filter;

            if (filter == 0)
            {
                m_results[index] = result;
                return;
            }

            float2
                r_position = raycast.position,
                r_dir = raycast.direction,
                hitLocation;

            float
                a_bottom = raycast.baseline,
                a_top = a_bottom,
                a_sqRange = lengthsq(raycast.distance);

            Segment2D raySegment = new Segment2D(raycast.position, raycast.position + raycast.direction * raycast.distance),
                segment;

            UIntPair pair = new UIntPair(0, 0);
            ObstacleVertexData otherVertex;
            bool
                twoSidedCast = raycast.twoSided,
                alreadyCovered = false,
                hit;

            #region static obstacles

            if ((filter & RaycastFilter.OBSTACLE_STATIC) != 0)
            {

                NativeList<int> staticObstacleNeighbors = new NativeList<int>(10, Allocator.Temp);

                if (m_staticObstacleTree.Length > 0)
                    QueryObstacleTreeRecursive(ref raycast, ref a_sqRange, 0, ref staticObstacleNeighbors,
                    ref m_staticObstacles, ref m_staticRefObstacles, ref m_staticObstacleInfos, ref m_staticObstacleTree);

                NativeParallelHashMap<UIntPair, bool> coveredStaticEdges = new NativeParallelHashMap<UIntPair, bool>(m_staticObstacleTree.Length * 2, Allocator.Temp);

                for (int i = 0; i < staticObstacleNeighbors.Length; ++i)
                {

                    ObstacleVertexData vertex = m_staticObstacles[staticObstacleNeighbors[i]];
                    ObstacleInfos infos = m_staticObstacleInfos[vertex.infos];

                    pair = new UIntPair(vertex.index, vertex.next);
                    alreadyCovered = coveredStaticEdges.TryGetValue(pair, out hit);

                    if (!alreadyCovered)
                    {

                        otherVertex = m_staticRefObstacles[vertex.next];
                        segment = new Segment2D(vertex.pos, otherVertex.pos);
                        alreadyCovered = (!twoSidedCast && dot(r_dir, segment.normal) < 0f);

                        if (!alreadyCovered)
                        {
                            if (raySegment.IsIntersecting(segment, out hitLocation))
                            {
                                //Rays intersecting !
                                if (result.hitObstacle == -1
                                    || distancesq(r_position, hitLocation) < distancesq(r_position, result.hitObstacleLocation2D))
                                {
                                    result.hitObstacleLocation2D = hitLocation;
                                    result.hitObstacle = infos.index;
                                }
                            }
                        }

                        coveredStaticEdges.TryAdd(pair, true);

                    }

                    pair = new UIntPair(vertex.index, vertex.prev);
                    alreadyCovered = coveredStaticEdges.ContainsKey(pair);

                    if (!alreadyCovered)
                    {
                        otherVertex = m_staticRefObstacles[vertex.prev];
                        segment = new Segment2D(vertex.pos, otherVertex.pos);
                        alreadyCovered = (!twoSidedCast && dot(r_dir, segment.normal) < 0f);

                        if (!alreadyCovered)
                        {
                            if (raySegment.IsIntersecting(segment, out hitLocation))
                            {
                                //Rays intersecting !
                                if (result.hitObstacle == -1
                                    || distancesq(r_position, hitLocation) < distancesq(r_position, result.hitObstacleLocation2D))
                                {
                                    result.hitObstacleLocation2D = hitLocation;
                                    result.hitObstacle = infos.index;
                                }
                            }
                        }

                        coveredStaticEdges.TryAdd(pair, true);

                    }

                }

                staticObstacleNeighbors.Release();
                coveredStaticEdges.Release();

            }

            #endregion

            #region dynamic obstacles

            if ((filter & RaycastFilter.OBSTACLE_DYNAMIC) != 0)
            {

                NativeList<int> dynObstacleNeighbors = new NativeList<int>(10, Allocator.Temp);

                if (m_dynObstacleTree.Length > 0)
                    QueryObstacleTreeRecursive(ref raycast, ref a_sqRange, 0, ref dynObstacleNeighbors,
                        ref m_dynObstacles, ref m_dynRefObstacles, ref m_dynObstacleInfos, ref m_dynObstacleTree);

                NativeParallelHashMap<UIntPair, bool> coveredDynEdges = new NativeParallelHashMap<UIntPair, bool>(m_dynObstacleTree.Length * 2, Allocator.Temp);

                for (int i = 0; i < dynObstacleNeighbors.Length; ++i)
                {

                    ObstacleVertexData vertex = m_dynObstacles[dynObstacleNeighbors[i]];
                    ObstacleInfos infos = m_dynObstacleInfos[vertex.infos];

                    pair = new UIntPair(vertex.index, vertex.next);
                    alreadyCovered = coveredDynEdges.TryGetValue(pair, out hit);

                    if (!alreadyCovered)
                    {

                        otherVertex = m_dynRefObstacles[vertex.next];
                        segment = new Segment2D(vertex.pos, otherVertex.pos);
                        alreadyCovered = (!twoSidedCast && dot(r_dir, segment.normal) < 0f);

                        if (!alreadyCovered)
                        {
                            if (raySegment.IsIntersecting(segment, out hitLocation))
                            {
                                //Rays intersecting !
                                if (result.hitObstacle == -1
                                    || distancesq(r_position, hitLocation) < distancesq(r_position, result.hitObstacleLocation2D))
                                {
                                    result.hitObstacleLocation2D = hitLocation;
                                    result.hitObstacle = infos.index;
                                    result.dynamicObstacle = true;
                                }
                            }
                        }

                        coveredDynEdges.TryAdd(pair, true);

                    }

                    pair = new UIntPair(vertex.index, vertex.prev);
                    alreadyCovered = coveredDynEdges.ContainsKey(pair);

                    if (!alreadyCovered)
                    {
                        otherVertex = m_dynRefObstacles[vertex.prev];
                        segment = new Segment2D(vertex.pos, otherVertex.pos);
                        alreadyCovered = (!twoSidedCast && dot(r_dir, segment.normal) < 0f);

                        if (!alreadyCovered)
                        {
                            if (raySegment.IsIntersecting(segment, out hitLocation))
                            {
                                //Rays intersecting !
                                if (result.hitObstacle == -1
                                    || distancesq(r_position, hitLocation) < distancesq(r_position, result.hitObstacleLocation2D))
                                {
                                    result.hitObstacleLocation2D = hitLocation;
                                    result.hitObstacle = infos.index;
                                    result.dynamicObstacle = true;
                                }
                            }
                        }

                        coveredDynEdges.TryAdd(pair, true);

                    }

                }

                dynObstacleNeighbors.Release();
                coveredDynEdges.Release();

            }

            #endregion

            #region Agents

            if ((filter & RaycastFilter.AGENTS) != 0)
            {

                NativeList<int> agentNeighbors = new NativeList<int>(10, Allocator.Temp);

                float radSq = a_sqRange + lengthsq(m_maxAgentRadius);

                if (m_inputAgents.Length > 0)
                    QueryAgentTreeRecursive(ref raycast, ref radSq, 0, ref agentNeighbors);

                AgentData agent;
                int iResult, agentIndex;
                float2 center, i1, i2, rayEnd = r_position + normalize(r_dir) * raycast.distance;
                float
                    a_radius, a_sqRadius, cx, cy,
                    Ax = r_position.x,
                    Ay = r_position.y,
                    Bx = rayEnd.x,
                    By = rayEnd.y,
                    dx = Bx - Ax,
                    dy = By - Ay,
                    magA = dx * dx + dy * dy,
                    distSq;

                int TryGetIntersection(out float2 intersection1, out float2 intersection2)
                {

                    float
                        magB = 2f * (dx * (Ax - cx) + dy * (Ay - cy)),
                        magC = (Ax - cx) * (Ax - cx) + (Ay - cy) * (Ay - cy) - a_sqRadius,
                        det = magB * magB - 4f * magA * magC,
                        sqDet, t;

                    if ((magA <= float.Epsilon) || (det < 0f))
                    {
                        // No real solutions.
                        intersection1 = float2(float.NaN, float.NaN);
                        intersection2 = float2(float.NaN, float.NaN);

                        return 0;
                    }

                    if (det == 0)
                    {
                        // One solution.
                        t = -magB / (2f * magA);

                        intersection1 = float2(Ax + t * dx, Ay + t * dy);
                        intersection2 = float2(float.NaN, float.NaN);

                        return 1;
                    }
                    else
                    {
                        // Two solutions.
                        sqDet = sqrt(det);

                        t = ((-magB + sqDet) / (2f * magA));
                        intersection1 = float2(Ax + t * dx, Ay + t * dy);

                        t = ((-magB - sqDet) / (2f * magA));
                        intersection2 = float2(Ax + t * dx, Ay + t * dy);

                        return 2;
                    }
                }

                for (int i = 0; i < agentNeighbors.Length; ++i)
                {
                    agentIndex = agentNeighbors[i];
                    agent = m_inputAgents[agentIndex];

                    center = agent.position;
                    cx = center.x;
                    cy = center.y;

                    a_radius = agent.radius;
                    a_sqRadius = a_radius * a_radius;

                    if (dot(center - r_position, r_dir) < 0f)
                        continue;

                    iResult = TryGetIntersection(out i1, out i2);

                    if (iResult == 0)
                        continue;

                    distSq = distancesq(r_position, i1);

                    if (distSq < a_sqRange
                        && (result.hitAgent == -1 || distSq < distancesq(r_position, result.hitAgentLocation2D))
                        && IsBetween(r_position, center, i1))
                    {
                        result.hitAgentLocation2D = i1;
                        result.hitAgent = agentIndex;
                    }

                    if (iResult == 2)
                    {

                        distSq = distancesq(r_position, i2);

                        if (distSq < a_sqRange
                            && (result.hitAgent == -1 || distSq < distancesq(r_position, result.hitAgentLocation2D))
                            && IsBetween(r_position, center, i2))
                        {
                            result.hitAgentLocation2D = i2;
                            result.hitAgent = agentIndex;
                        }
                    }

                }

                agentNeighbors.Release();

            }

            #endregion

            if (m_plane == AxisPair.XY)
            {
                float baseline = raycast.worldPosition.z;

                if (result.hitAgent != -1)
                    result.hitAgentLocation = float3(result.hitAgentLocation2D, baseline);

                if (result.hitObstacle != -1)
                    result.hitObstacleLocation = float3(result.hitObstacleLocation2D, baseline);
            }
            else
            {
                float baseline = raycast.worldPosition.y;

                if (result.hitAgent != -1)
                    result.hitAgentLocation = float3(result.hitAgentLocation2D.x, baseline, result.hitAgentLocation2D.y);

                if (result.hitObstacle != -1)
                    result.hitObstacleLocation = float3(result.hitObstacleLocation2D.x, baseline, result.hitObstacleLocation2D.y);
            }

            m_results[index] = result;

        }

        #region Agent KDTree Query

        /// <summary>
        /// Recursive method for computing the agent neighbors of the specified raycast.
        /// </summary>
        /// <param name="raycast">The agent for which agent neighbors are to be computed.</param>
        /// <param name="raycast">The agent making the initial query</param>
        /// <param name="rangeSq">The squared range around the agent.</param>
        /// <param name="node">The current agent k-D tree node index.</param>
        /// <param name="agentNeighbors">The list of neighbors to be filled up.</param>
        private void QueryAgentTreeRecursive(ref RaycastData raycast, ref float rangeSq, int node, ref NativeList<int> agentNeighbors)
        {

            float2 center = raycast.position;

            AgentTreeNode treeNode = m_inputAgentTree[node];

            if (treeNode.end - treeNode.begin <= AgentTreeNode.MAX_LEAF_SIZE)
            {
                AgentData a;
                float bottom = raycast.baseline, top = bottom;
                for (int i = treeNode.begin; i < treeNode.end; ++i)
                {
                    a = m_inputAgents[i];

                    if (!a.collisionEnabled
                        || (a.layerOccupation & ~raycast.layerIgnore) == 0
                        || (top < a.baseline || bottom > a.baseline + a.height))
                    {
                        continue;
                    }

                    //if ((distancesq(center, a.position) - lengthsq(a.radius)) < rangeSq)
                    agentNeighbors.Add(i);

                }
            }
            else
            {

                AgentTreeNode leftNode = m_inputAgentTree[treeNode.left], rightNode = m_inputAgentTree[treeNode.right];
                float distSqLeft = lengthsq(max(0.0f, leftNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - leftNode.maxX))
                    + lengthsq(max(0.0f, leftNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - leftNode.maxY));
                float distSqRight = lengthsq(max(0.0f, rightNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - rightNode.maxX))
                    + lengthsq(max(0.0f, rightNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - rightNode.maxY));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryAgentTreeRecursive(ref raycast, ref rangeSq, treeNode.left, ref agentNeighbors);

                        if (distSqRight < rangeSq)
                        {
                            QueryAgentTreeRecursive(ref raycast, ref rangeSq, treeNode.right, ref agentNeighbors);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryAgentTreeRecursive(ref raycast, ref rangeSq, treeNode.right, ref agentNeighbors);

                        if (distSqLeft < rangeSq)
                        {
                            QueryAgentTreeRecursive(ref raycast, ref rangeSq, treeNode.left, ref agentNeighbors);
                        }
                    }
                }

            }

        }

        #endregion

        #region Obstacle KDTree Query

        private void QueryObstacleTreeRecursive(
            ref RaycastData raycast,
            ref float rangeSq,
            int node,
            ref NativeList<int> obstacleNeighbors,
            ref NativeArray<ObstacleVertexData> obstacles,
            ref NativeArray<ObstacleVertexData> refObstacles,
            ref NativeArray<ObstacleInfos> obstaclesInfos,
            ref NativeArray<ObstacleTreeNode> kdTree)
        {

            float2 center = raycast.position;
            ObstacleTreeNode treeNode = kdTree[node];

            if (treeNode.end - treeNode.begin <= ObstacleTreeNode.MAX_LEAF_SIZE)
            {
                ObstacleVertexData o;
                ObstacleInfos infos;
                float top = raycast.baseline, bottom = raycast.baseline;
                float2 oPos, nPos;
                for (int i = treeNode.begin; i < treeNode.end; ++i)
                {
                    o = obstacles[i];
                    infos = obstaclesInfos[o.infos];

                    if (!infos.collisionEnabled || (infos.layerOccupation & ~raycast.layerIgnore) == 0)
                        continue;

                    if (top < infos.baseline || bottom > infos.baseline + infos.height)
                        continue;

                    oPos = o.pos; nPos = refObstacles[o.next].pos;
                    float distSq = DistSqPointLineSegment(oPos, nPos, center);
                    if (distSq < rangeSq)
                    {
                        float raycastLeftOfLine = LeftOf(oPos, nPos, center);
                        if ((lengthsq(raycastLeftOfLine) / lengthsq(nPos - oPos)) < rangeSq && raycastLeftOfLine < 0.0f)
                            obstacleNeighbors.Add(i);
                    }

                }

            }
            else
            {

                ObstacleTreeNode leftNode = kdTree[treeNode.left],
                    rightNode = kdTree[treeNode.right];

                float distSqLeft = lengthsq(max(0.0f, leftNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - leftNode.maxX))
                    + lengthsq(max(0.0f, leftNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - leftNode.maxY));
                float distSqRight = lengthsq(max(0.0f, rightNode.minX - center.x))
                    + lengthsq(max(0.0f, center.x - rightNode.maxX))
                    + lengthsq(max(0.0f, rightNode.minY - center.y))
                    + lengthsq(max(0.0f, center.y - rightNode.maxY));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryObstacleTreeRecursive(ref raycast, ref rangeSq, treeNode.left, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);

                        if (distSqRight < rangeSq)
                        {
                            QueryObstacleTreeRecursive(ref raycast, ref rangeSq, treeNode.right, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryObstacleTreeRecursive(ref raycast, ref rangeSq, treeNode.right, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);

                        if (distSqLeft < rangeSq)
                        {
                            QueryObstacleTreeRecursive(ref raycast, ref rangeSq, treeNode.left, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);
                        }
                    }
                }

            }

        }

        #endregion

        #region maths

        /// <summary>
        /// Computes the determinant of a two-dimensional square matrix 
        /// with rows consisting of the specified two-dimensional vectors.
        /// </summary>
        /// <param name="a">The top row of the two-dimensional square matrix</param>
        /// <param name="b">The bottom row of the two-dimensional square matrix</param>
        /// <returns>The determinant of the two-dimensional square matrix.</returns>
        private float Det(float2 a, float2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private float LeftOf(float2 a, float2 b, float2 c)
        {
            float x1 = a.x - c.x, y1 = a.y - c.y, x2 = b.x - a.x, y2 = b.y - a.y;
            return x1 * y2 - y1 * x2;
        }

        /// <summary>
        /// Is a point c between a and b?
        /// </summary>
        /// <param name="c"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool IsBetween(float2 a, float2 b, float2 c)
        {

            float2 ab = float2(b.x - a.x, b.y - a.y);//Entire line segment
            float2 ac = float2(c.x - a.x, c.y - a.y);//The intersection and the first point

            float dot = ab.x * ac.x + ab.y * ac.y;

            //If the vectors are pointing in the same direction = dot product is positive
            if (dot <= 0f) { return false; }

            float abm = ab.x * ab.x + ab.y * ab.y;
            float acm = ac.x * ac.x + ac.y * ac.y;

            //If the length of the vector between the intersection and the first point is smaller than the entire line
            return (abm >= acm);
        }

        /// <summary>
        /// Computes the squared distance from a line segment with the specified endpoints to a specified point.
        /// </summary>
        /// <param name="a">The first endpoint of the line segment.</param>
        /// <param name="b">The second endpoint of the line segment.</param>
        /// <param name="c">The point to which the squared distance is to be calculated.</param>
        /// <returns>The squared distance from the line segment to the point.</returns>
        private float DistSqPointLineSegment(float2 a, float2 b, float2 c)
        {

            float2 ca = float2(c.x - a.x, c.y - a.y);
            float2 ba = float2(b.x - a.x, b.y - a.y);
            float dot = ca.x * ba.x + ca.y * ba.y;

            float r = dot / (ba.x * ba.x + ba.y * ba.y);

            if (r < 0.0f)
            {
                return ca.x * ca.x + ca.y * ca.y;
            }

            if (r > 1.0f)
            {
                float2 cb = float2(c.x - b.x, c.y - b.y);
                return cb.x * cb.x + cb.y * cb.y;
            }

            float2 d = float2(c.x - (a.x + r * ba.x), c.y - (a.y + r * ba.y));
            return d.x * d.x + d.y * d.y;

        }

        #endregion

    }
}
