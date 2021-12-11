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

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Nebukam.JobAssist.Extensions;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    public struct DVP
    {
        public float distSq;
        public int index;
        public DVP(float dist, int i)
        {
            distSq = dist;
            index = i;
        }
    }

    public struct ORCALine
    {
        public float2 dir;
        public float2 point;
    }

    [BurstCompile]
    public struct ORCALinesJob : IJobParallelFor
    {

        const float EPSILON = 0.00001f;

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

        public NativeArray<AgentDataResult> m_results;
        public float m_timestep;

        public void Execute(int index)
        {

            AgentData agent = m_inputAgents[index];
            AgentDataResult result = new AgentDataResult();

            if (agent.maxNeighbors == 0 || !agent.navigationEnabled)
            {
                result.position = agent.position;
                result.velocity = agent.velocity;
                m_results[index] = result;
                return;
            }

            AgentData otherAgent;
                        
            float2
                a_position = agent.position,
                a_prefVelocity = agent.prefVelocity,
                a_velocity = agent.velocity,
                a_newVelocity = a_prefVelocity;

            float
                a_bottom = agent.baseline,
                a_top = a_bottom + agent.height,
                a_maxSpeed = agent.maxSpeed,
                a_radius = agent.radius,
                a_radiusObst = agent.radiusObst,
                a_timeHorizon = agent.timeHorizon,
                a_timeHorizonObst = agent.timeHorizonObst,
                obsRangeSq = lengthsq(a_timeHorizonObst * a_maxSpeed + a_radius),
                rangeSq = lengthsq(agent.radius + agent.neighborDist);

            NativeList<ORCALine> m_orcaLines = new NativeList<ORCALine>(16, Allocator.Temp);
            int numObstLines = 0;
            
            #region obstacles

            float invTimeHorizonObst = 1.0f / agent.timeHorizonObst;

            #region static obstacles

            if (m_staticObstacleTree.Length > 0)
            {

                NativeList<DVP> staticObstacleNeighbors = new NativeList<DVP>(10, Allocator.Temp);

                QueryObstacleTreeRecursive(
                    ref a_position,
                    ref agent,
                    ref obsRangeSq, 0,
                    ref staticObstacleNeighbors,
                    ref m_staticObstacles,
                    ref m_staticRefObstacles,
                    ref m_staticObstacleInfos,
                    ref m_staticObstacleTree);

                for (int i = 0; i < staticObstacleNeighbors.Length; ++i)
                {

                    ObstacleVertexData vertex = m_staticObstacles[staticObstacleNeighbors[i].index];
                    ObstacleVertexData nextVertex = m_staticRefObstacles[vertex.next];
                    ObstacleInfos infos = m_staticObstacleInfos[vertex.infos];

                    //if(a_top < infos.baseline || a_bottom > infos.baseline + infos.height) { continue; }

                    float2 relPos1 = vertex.pos - a_position;
                    float2 relPos2 = nextVertex.pos - a_position;

                    float oRadius = a_radiusObst + infos.thickness;

                    // Check if velocity obstacle of obstacle is already taken care
                    // of by previously constructed obstacle ORCA lines.
                    bool alreadyCovered = false;

                    for (int j = 0; j < m_orcaLines.Length; ++j)
                    {
                        if (Det(invTimeHorizonObst * relPos1 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius
                            >= -EPSILON && Det(invTimeHorizonObst * relPos2 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius >= -EPSILON)
                        {
                            alreadyCovered = true;
                            break;
                        }
                    }


                    if (alreadyCovered)
                    {
                        continue;
                    }

                    // Not yet covered. Check for collisions.
                    float distSq1 = lengthsq(relPos1);
                    float distSq2 = lengthsq(relPos2);

                    float radiusSq = lengthsq(oRadius);

                    float2 obstacleVector = nextVertex.pos - vertex.pos;
                    float s = lengthsq(obstacleVector / dot(-relPos1, obstacleVector));
                    float distSqLine = lengthsq(-relPos1 - (s * obstacleVector));

                    ORCALine line;

                    if (s < 0.0f && distSq1 <= radiusSq)
                    {
                        // Collision with left vertex. Ignore if non-convex.
                        if (vertex.convex)
                        {
                            line.point = float2(0f);
                            line.dir = normalize(float2(-relPos1.y, relPos1.x));
                            m_orcaLines.Add(line);
                        }

                        continue;
                    }
                    else if (s > 1.0f && distSq2 <= radiusSq)
                    {
                        // Collision with right vertex. Ignore if non-convex or if
                        // it will be taken care of by neighboring obstacle.
                        if (nextVertex.convex && Det(relPos2, nextVertex.dir) >= 0.0f)
                        {
                            line.point = float2(0f);
                            line.dir = normalize(float2(-relPos2.y, relPos2.x));
                            m_orcaLines.Add(line);
                        }

                        continue;
                    }
                    else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                    {
                        // Collision with obstacle segment.
                        line.point = float2(0f);
                        line.dir = -vertex.dir;
                        m_orcaLines.Add(line);

                        continue;
                    }

                    // No collision. Compute legs. When obliquely viewed, both legs
                    // can come from a single vertex. Legs extend cut-off line when
                    // non-convex vertex.

                    float2 lLegDir, rLegDir;

                    if (s < 0.0f && distSqLine <= radiusSq)
                    {

                        // Obstacle viewed obliquely so that left vertex
                        // defines velocity obstacle.
                        if (!vertex.convex)
                        {
                            // Ignore obstacle.
                            continue;
                        }

                        nextVertex = vertex;

                        float leg1 = sqrt(distSq1 - radiusSq);
                        lLegDir = float2(relPos1.x * leg1 - relPos1.y * oRadius, relPos1.x * oRadius + relPos1.y * leg1) / distSq1;
                        rLegDir = float2(relPos1.x * leg1 + relPos1.y * oRadius, -relPos1.x * oRadius + relPos1.y * leg1) / distSq1;
                    }
                    else if (s > 1.0f && distSqLine <= radiusSq)
                    {

                        // Obstacle viewed obliquely so that
                        // right vertex defines velocity obstacle.
                        if (!nextVertex.convex)
                        {
                            // Ignore obstacle.
                            continue;
                        }

                        vertex = nextVertex;

                        float leg2 = sqrt(distSq2 - radiusSq);
                        lLegDir = float2(relPos2.x * leg2 - relPos2.y * oRadius, relPos2.x * oRadius + relPos2.y * leg2) / distSq2;
                        rLegDir = float2(relPos2.x * leg2 + relPos2.y * oRadius, -relPos2.x * oRadius + relPos2.y * leg2) / distSq2;
                    }
                    else
                    {
                        // Usual situation.
                        if (vertex.convex)
                        {
                            float leg1 = sqrt(distSq1 - radiusSq);
                            lLegDir = float2(relPos1.x * leg1 - relPos1.y * oRadius, relPos1.x * oRadius + relPos1.y * leg1) / distSq1;
                        }
                        else
                        {
                            // Left vertex non-convex; left leg extends cut-off line.
                            lLegDir = -vertex.dir;
                        }

                        if (nextVertex.convex)
                        {
                            float leg2 = sqrt(distSq2 - radiusSq);
                            rLegDir = float2(relPos2.x * leg2 + relPos2.y * oRadius, -relPos2.x * oRadius + relPos2.y * leg2) / distSq2;
                        }
                        else
                        {
                            // Right vertex non-convex; right leg extends cut-off line.
                            rLegDir = vertex.dir;
                        }
                    }

                    // Legs can never point into neighboring edge when convex
                    // vertex, take cutoff-line of neighboring edge instead. If
                    // velocity projected on "foreign" leg, no constraint is added.

                    ObstacleVertexData leftNeighbor = m_staticRefObstacles[vertex.prev];

                    bool isLeftLegForeign = false;
                    bool isRightLegForeign = false;

                    if (vertex.convex && Det(lLegDir, -leftNeighbor.dir) >= 0.0f)
                    {
                        // Left leg points into obstacle.
                        lLegDir = -leftNeighbor.dir;
                        isLeftLegForeign = true;
                    }

                    if (nextVertex.convex && Det(rLegDir, nextVertex.dir) <= 0.0f)
                    {
                        // Right leg points into obstacle.
                        rLegDir = nextVertex.dir;
                        isRightLegForeign = true;
                    }

                    // Compute cut-off centers.
                    float2 leftCutOff = invTimeHorizonObst * (vertex.pos - a_position);
                    float2 rightCutOff = invTimeHorizonObst * (nextVertex.pos - a_position);
                    float2 cutOffVector = rightCutOff - leftCutOff;

                    // Project current velocity on velocity obstacle.

                    // Check if current velocity is projected on cutoff circles.
                    float t = vertex.index == nextVertex.index ? 0.5f : dot((a_velocity - leftCutOff), cutOffVector) / lengthsq(cutOffVector);
                    float tLeft = dot((a_velocity - leftCutOff), lLegDir);
                    float tRight = dot((a_velocity - rightCutOff), rLegDir);

                    if ((t < 0.0f && tLeft < 0.0f) || (vertex.index == nextVertex.index && tLeft < 0.0f && tRight < 0.0f))
                    {
                        // Project on left cut-off circle.
                        float2 unitW = normalize(a_velocity - leftCutOff);

                        line.dir = float2(unitW.y, -unitW.x);
                        line.point = leftCutOff + oRadius * invTimeHorizonObst * unitW;
                        m_orcaLines.Add(line);

                        continue;
                    }
                    else if (t > 1.0f && tRight < 0.0f)
                    {
                        // Project on right cut-off circle.
                        float2 unitW = normalize(a_velocity - rightCutOff);

                        line.dir = float2(unitW.y, -unitW.x);
                        line.point = rightCutOff + oRadius * invTimeHorizonObst * unitW;
                        m_orcaLines.Add(line);

                        continue;
                    }

                    // Project on left leg, right leg, or cut-off line, whichever is
                    // closest to velocity.
                    float distSqCutoff = (t < 0.0f || t > 1.0f || vertex.index == nextVertex.index) ? float.PositiveInfinity : lengthsq(a_velocity - (leftCutOff + t * cutOffVector));
                    float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : lengthsq(a_velocity - (leftCutOff + tLeft * lLegDir));
                    float distSqRight = tRight < 0.0f ? float.PositiveInfinity : lengthsq(a_velocity - (rightCutOff + tRight * rLegDir));

                    if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                    {
                        // Project on cut-off line.
                        line.dir = -vertex.dir;
                        line.point = leftCutOff + oRadius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                        m_orcaLines.Add(line);

                        continue;
                    }

                    if (distSqLeft <= distSqRight)
                    {
                        // Project on left leg.
                        if (isLeftLegForeign)
                        {
                            continue;
                        }

                        line.dir = lLegDir;
                        line.point = leftCutOff + oRadius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                        m_orcaLines.Add(line);

                        continue;
                    }

                    // Project on right leg.
                    if (isRightLegForeign)
                    {
                        continue;
                    }

                    line.dir = -rLegDir;
                    line.point = rightCutOff + oRadius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                    m_orcaLines.Add(line);
                }

                staticObstacleNeighbors.Release();

            }

            #endregion

            #region dynamic obstacles

            if (m_dynObstacleTree.Length > 0)
            {

                NativeList<DVP> dynObstacleNeighbors = new NativeList<DVP>(10, Allocator.Temp);

                QueryObstacleTreeRecursive(
                    ref a_position, 
                    ref agent, 
                    ref obsRangeSq, 0, 
                    ref dynObstacleNeighbors,
                    ref m_dynObstacles, 
                    ref m_dynRefObstacles, 
                    ref m_dynObstacleInfos, 
                    ref m_dynObstacleTree);

                for (int i = 0; i < dynObstacleNeighbors.Length; ++i)
                {

                    ObstacleVertexData vertex = m_dynObstacles[dynObstacleNeighbors[i].index];
                    ObstacleVertexData nextVertex = m_dynRefObstacles[vertex.next];
                    ObstacleInfos infos = m_dynObstacleInfos[vertex.infos];

                    //if (a_top < infos.baseline || a_bottom > infos.baseline + infos.height) { continue; }

                    float2 relPos1 = vertex.pos - a_position;
                    float2 relPos2 = nextVertex.pos - a_position;

                    float oRadius = a_radiusObst + infos.thickness;

                    // Check if velocity obstacle of obstacle is already taken care
                    // of by previously constructed obstacle ORCA lines.
                    bool alreadyCovered = false;

                    for (int j = 0; j < m_orcaLines.Length; ++j)
                    {
                        if (Det(invTimeHorizonObst * relPos1 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius
                            >= -EPSILON && Det(invTimeHorizonObst * relPos2 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * oRadius >= -EPSILON)
                        {
                            alreadyCovered = true;
                            break;
                        }
                    }

                    if (alreadyCovered)
                    {
                        continue;
                    }

                    // Not yet covered. Check for collisions.
                    float distSq1 = lengthsq(relPos1);
                    float distSq2 = lengthsq(relPos2);

                    float radiusSq = lengthsq(oRadius);

                    float2 obstacleVector = nextVertex.pos - vertex.pos;
                    float s = lengthsq(obstacleVector / dot(-relPos1, obstacleVector));
                    float distSqLine = lengthsq(-relPos1 - (s * obstacleVector));

                    ORCALine line;

                    if (s < 0.0f && distSq1 <= radiusSq)
                    {
                        // Collision with left vertex. Ignore if non-convex.
                        if (vertex.convex)
                        {
                            line.point = float2(0f);
                            line.dir = normalize(float2(-relPos1.y, relPos1.x));
                            m_orcaLines.Add(line);
                        }

                        continue;
                    }
                    else if (s > 1.0f && distSq2 <= radiusSq)
                    {
                        // Collision with right vertex. Ignore if non-convex or if
                        // it will be taken care of by neighboring obstacle.
                        if (nextVertex.convex && Det(relPos2, nextVertex.dir) >= 0.0f)
                        {
                            line.point = float2(0f);
                            line.dir = normalize(float2(-relPos2.y, relPos2.x));
                            m_orcaLines.Add(line);
                        }

                        continue;
                    }
                    else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                    {
                        // Collision with obstacle segment.
                        line.point = float2(0f);
                        line.dir = -vertex.dir;
                        m_orcaLines.Add(line);

                        continue;
                    }

                    // No collision. Compute legs. When obliquely viewed, both legs
                    // can come from a single vertex. Legs extend cut-off line when
                    // non-convex vertex.

                    float2 lLegDir, rLegDir;

                    if (s < 0.0f && distSqLine <= radiusSq)
                    {

                        // Obstacle viewed obliquely so that left vertex
                        // defines velocity obstacle.
                        if (!vertex.convex)
                        {
                            // Ignore obstacle.
                            continue;
                        }

                        nextVertex = vertex;

                        float leg1 = sqrt(distSq1 - radiusSq);
                        lLegDir = float2(relPos1.x * leg1 - relPos1.y * oRadius, relPos1.x * oRadius + relPos1.y * leg1) / distSq1;
                        rLegDir = float2(relPos1.x * leg1 + relPos1.y * oRadius, -relPos1.x * oRadius + relPos1.y * leg1) / distSq1;
                    }
                    else if (s > 1.0f && distSqLine <= radiusSq)
                    {

                        // Obstacle viewed obliquely so that
                        // right vertex defines velocity obstacle.
                        if (!nextVertex.convex)
                        {
                            // Ignore obstacle.
                            continue;
                        }

                        vertex = nextVertex;

                        float leg2 = sqrt(distSq2 - radiusSq);
                        lLegDir = float2(relPos2.x * leg2 - relPos2.y * oRadius, relPos2.x * oRadius + relPos2.y * leg2) / distSq2;
                        rLegDir = float2(relPos2.x * leg2 + relPos2.y * oRadius, -relPos2.x * oRadius + relPos2.y * leg2) / distSq2;
                    }
                    else
                    {
                        // Usual situation.
                        if (vertex.convex)
                        {
                            float leg1 = sqrt(distSq1 - radiusSq);
                            lLegDir = float2(relPos1.x * leg1 - relPos1.y * oRadius, relPos1.x * oRadius + relPos1.y * leg1) / distSq1;
                        }
                        else
                        {
                            // Left vertex non-convex; left leg extends cut-off line.
                            lLegDir = -vertex.dir;
                        }

                        if (nextVertex.convex)
                        {
                            float leg2 = sqrt(distSq2 - radiusSq);
                            rLegDir = float2(relPos2.x * leg2 + relPos2.y * oRadius, -relPos2.x * oRadius + relPos2.y * leg2) / distSq2;
                        }
                        else
                        {
                            // Right vertex non-convex; right leg extends cut-off line.
                            rLegDir = vertex.dir;
                        }
                    }

                    // Legs can never point into neighboring edge when convex
                    // vertex, take cutoff-line of neighboring edge instead. If
                    // velocity projected on "foreign" leg, no constraint is added.

                    ObstacleVertexData leftNeighbor = m_dynRefObstacles[vertex.prev];

                    bool isLeftLegForeign = false;
                    bool isRightLegForeign = false;

                    if (vertex.convex && Det(lLegDir, -leftNeighbor.dir) >= 0.0f)
                    {
                        // Left leg points into obstacle.
                        lLegDir = -leftNeighbor.dir;
                        isLeftLegForeign = true;
                    }

                    if (nextVertex.convex && Det(rLegDir, nextVertex.dir) <= 0.0f)
                    {
                        // Right leg points into obstacle.
                        rLegDir = nextVertex.dir;
                        isRightLegForeign = true;
                    }

                    // Compute cut-off centers.
                    float2 leftCutOff = invTimeHorizonObst * (vertex.pos - a_position);
                    float2 rightCutOff = invTimeHorizonObst * (nextVertex.pos - a_position);
                    float2 cutOffVector = rightCutOff - leftCutOff;

                    // Project current velocity on velocity obstacle.

                    // Check if current velocity is projected on cutoff circles.
                    float t = vertex.index == nextVertex.index ? 0.5f : dot((a_velocity - leftCutOff), cutOffVector) / lengthsq(cutOffVector);
                    float tLeft = dot((a_velocity - leftCutOff), lLegDir);
                    float tRight = dot((a_velocity - rightCutOff), rLegDir);

                    if ((t < 0.0f && tLeft < 0.0f) || (vertex.index == nextVertex.index && tLeft < 0.0f && tRight < 0.0f))
                    {
                        // Project on left cut-off circle.
                        float2 unitW = normalize(a_velocity - leftCutOff);

                        line.dir = float2(unitW.y, -unitW.x);
                        line.point = leftCutOff + oRadius * invTimeHorizonObst * unitW;
                        m_orcaLines.Add(line);

                        continue;
                    }
                    else if (t > 1.0f && tRight < 0.0f)
                    {
                        // Project on right cut-off circle.
                        float2 unitW = normalize(a_velocity - rightCutOff);

                        line.dir = float2(unitW.y, -unitW.x);
                        line.point = rightCutOff + oRadius * invTimeHorizonObst * unitW;
                        m_orcaLines.Add(line);

                        continue;
                    }

                    // Project on left leg, right leg, or cut-off line, whichever is
                    // closest to velocity.
                    float distSqCutoff = (t < 0.0f || t > 1.0f || vertex.index == nextVertex.index) ? float.PositiveInfinity : lengthsq(a_velocity - (leftCutOff + t * cutOffVector));
                    float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : lengthsq(a_velocity - (leftCutOff + tLeft * lLegDir));
                    float distSqRight = tRight < 0.0f ? float.PositiveInfinity : lengthsq(a_velocity - (rightCutOff + tRight * rLegDir));

                    if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                    {
                        // Project on cut-off line.
                        line.dir = -vertex.dir;
                        line.point = leftCutOff + oRadius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                        m_orcaLines.Add(line);

                        continue;
                    }

                    if (distSqLeft <= distSqRight)
                    {
                        // Project on left leg.
                        if (isLeftLegForeign)
                        {
                            continue;
                        }

                        line.dir = lLegDir;
                        line.point = leftCutOff + oRadius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                        m_orcaLines.Add(line);

                        continue;
                    }

                    // Project on right leg.
                    if (isRightLegForeign)
                    {
                        continue;
                    }

                    line.dir = -rLegDir;
                    line.point = rightCutOff + oRadius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                    m_orcaLines.Add(line);
                }

                dynObstacleNeighbors.Release();

            }

            #endregion

            numObstLines = m_orcaLines.Length;

            #endregion

            #region agents

            NativeList<DVP> agentNeighbors = new NativeList<DVP>(agent.maxNeighbors, Allocator.Temp);

            QueryAgentTreeRecursive(
                ref a_position, 
                ref agent, 
                ref rangeSq, 0, 
                ref agentNeighbors);

            float invTimeHorizon = 1.0f / a_timeHorizon;

            for (int i = 0; i < agentNeighbors.Length; ++i)
            {

                otherAgent = m_inputAgents[agentNeighbors[i].index];

                float2 relPos = otherAgent.position - a_position;
                float2 relVel = a_velocity - otherAgent.velocity;
                float distSq = lengthsq(relPos);
                float cRad = a_radius + otherAgent.radius;
                float cRadSq = lengthsq(cRad);

                ORCALine line = new ORCALine();
                float2 u;

                if (distSq > cRadSq)
                {
                    // No collision.
                    float2 w = relVel - invTimeHorizon * relPos;

                    // Vector from cutoff center to relative velocity.
                    float wLengthSq = lengthsq(w);
                    float dotProduct1 = dot(w, relPos);

                    if (dotProduct1 < 0.0f && lengthsq(dotProduct1) > cRadSq * wLengthSq)
                    {
                        // Project on cut-off circle.
                        float wLength = sqrt(wLengthSq);
                        float2 unitW = w / wLength;

                        line.dir = float2(unitW.y, -unitW.x);
                        u = (cRad * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        // Project on legs.
                        float leg = sqrt(distSq - cRadSq);

                        if (Det(relPos, w) > 0.0f)
                        {
                            // Project on left leg.
                            line.dir = float2(relPos.x * leg - relPos.y * cRad, relPos.x * cRad + relPos.y * leg) / distSq;
                        }
                        else
                        {
                            // Project on right leg.
                            line.dir = -float2(relPos.x * leg + relPos.y * cRad, -relPos.x * cRad + relPos.y * leg) / distSq;
                        }

                        float dotProduct2 = dot(relVel, line.dir);
                        u = dotProduct2 * line.dir - relVel;
                    }
                }
                else
                {
                    // Collision. Project on cut-off circle of time timeStep.
                    float invTimeStep = 1.0f / m_timestep;

                    // Vector from cutoff center to relative velocity.
                    float2 w = relVel - invTimeStep * relPos;

                    float wLength = length(w);
                    float2 unitW = w / wLength;

                    line.dir = float2(unitW.y, -unitW.x);
                    u = (cRad * invTimeStep - wLength) * unitW;
                }

                line.point = a_velocity + 0.5f * u;
                m_orcaLines.Add(line);
            }

            agentNeighbors.Release();

            #endregion

            #region Compute new velocity

            int lineFail = LP2(m_orcaLines, a_maxSpeed, a_prefVelocity, false, ref a_newVelocity);

            if (lineFail < m_orcaLines.Length)
                LP3(m_orcaLines, numObstLines, lineFail, a_maxSpeed, ref a_newVelocity);

            #endregion

            result.velocity = a_newVelocity;
            result.position = a_position + a_newVelocity * m_timestep;

            m_results[index] = result;

            m_orcaLines.Release();

        }

        #region Agent KDTree Query

        /// <summary>
        /// Recursive method for computing the agent neighbors of the specified agent.
        /// </summary>
        /// <param name="agent">The agent for which agent neighbors are to be computed.</param>
        /// <param name="agent">The agent making the initial query</param>
        /// <param name="rangeSq">The squared range around the agent.</param>
        /// <param name="node">The current agent k-D tree node index.</param>
        /// <param name="agentNeighbors">The list of neighbors to be filled up.</param>
        private void QueryAgentTreeRecursive(ref float2 center, ref AgentData agent, ref float rangeSq, int node, ref NativeList<DVP> agentNeighbors)
        {

            AgentTreeNode treeNode = m_inputAgentTree[node];

            if (treeNode.end - treeNode.begin <= AgentTreeNode.MAX_LEAF_SIZE)
            {
                AgentData a;
                float bottom = agent.baseline, top = bottom + agent.height;
                for (int i = treeNode.begin; i < treeNode.end; ++i)
                {
                    a = m_inputAgents[i];

                    if (a.index == agent.index
                        || !a.collisionEnabled
                        || (a.layerOccupation & ~agent.layerIgnore) == 0
                        || (top < a.baseline || bottom > a.baseline + a.height))
                    {
                        continue;
                    }

                    float distSq = lengthsq(center - a.position);

                    if (distSq < rangeSq)
                    {
                        if (agentNeighbors.Length < agent.maxNeighbors)
                        {
                            agentNeighbors.Add(new DVP(distSq, i));
                        }

                        int j = agentNeighbors.Length - 1;

                        while (j != 0 && distSq < agentNeighbors[j - 1].distSq)
                        {
                            agentNeighbors[j] = agentNeighbors[j - 1];
                            --j;
                        }

                        agentNeighbors[j] = new DVP(distSq, i);

                        if (agentNeighbors.Length == agent.maxNeighbors)
                        {
                            rangeSq = agentNeighbors[agentNeighbors.Length - 1].distSq;
                        }
                    }

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
                        QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref agentNeighbors);

                        if (distSqRight < rangeSq)
                        {
                            QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref agentNeighbors);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref agentNeighbors);

                        if (distSqLeft < rangeSq)
                        {
                            QueryAgentTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref agentNeighbors);
                        }
                    }
                }

            }

        }

        #endregion

        #region Obstacle KDTree Query

        private void QueryObstacleTreeRecursive(
            ref float2 center,
            ref AgentData agent,
            ref float rangeSq,
            int node,
            ref NativeList<DVP> obstacleNeighbors,
            ref NativeArray<ObstacleVertexData> obstacles,
            ref NativeArray<ObstacleVertexData> refObstacles,
            ref NativeArray<ObstacleInfos> obstaclesInfos,
            ref NativeArray<ObstacleTreeNode> kdTree)
        {
            ObstacleTreeNode treeNode = kdTree[node];

            if (treeNode.end - treeNode.begin <= ObstacleTreeNode.MAX_LEAF_SIZE)
            {
                ObstacleVertexData o, next;
                ObstacleInfos infos;
                float top = agent.baseline + agent.height, bottom = agent.baseline;
                for (int i = treeNode.begin; i < treeNode.end; ++i)
                {
                    o = obstacles[i];
                    infos = obstaclesInfos[o.infos];

                    if (!infos.collisionEnabled || (infos.layerOccupation & ~agent.layerIgnore) == 0)
                        continue;

                    if (top < infos.baseline || bottom > infos.baseline + infos.height)
                        continue;

                    next = refObstacles[o.next];
                    float distSq = DistSqPointLineSegment(o.pos, next.pos, center);

                    if (distSq < rangeSq)
                    {

                        float agentLeftOfLine = LeftOf(o.pos, next.pos, center);
                        float distSqLine = lengthsq(agentLeftOfLine) / lengthsq(next.pos - o.pos);

                        if (distSqLine < rangeSq)
                        {


                            if (agentLeftOfLine < 0.0f)
                            {
                                // Try obstacle at this node only if agent is on right side of
                                // obstacle (and can see obstacle).
                                obstacleNeighbors.Add(new DVP(distSq, i));


                                int index = obstacleNeighbors.Length - 1;

                                //re-order to keep the closest vertex first
                                while (index != 0 && distSq < obstacleNeighbors[index - 1].distSq)
                                {
                                    obstacleNeighbors[index] = obstacleNeighbors[index - 1];
                                    --index;
                                }

                                obstacleNeighbors[index] = new DVP(distSq, i);


                            }

                        }
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
                        QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);

                        if (distSqRight < rangeSq)
                        {
                            QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.right, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);

                        if (distSqLeft < rangeSq)
                        {
                            QueryObstacleTreeRecursive(ref center, ref agent, ref rangeSq, treeNode.left, ref obstacleNeighbors,
                            ref obstacles, ref refObstacles, ref obstaclesInfos, ref kdTree);
                        }
                    }
                }

            }

        }

        #endregion

        #region Linear programs

        /// <summary>
        /// Solves a one-dimensional linear program on a specified line subject to linear 
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="lineNo">The specified line constraint.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="optVel">The optimization velocity.</param>
        /// <param name="dirOpt">True if the direction should be optimized.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        /// <returns>True if successful.</returns>
        private bool LP1(NativeList<ORCALine> lines, int lineNo, float radius, float2 optVel, bool dirOpt, ref float2 result)
        {

            ORCALine line = lines[lineNo];
            float2 dir = line.dir, pt = line.point;

            float dotProduct = dot(pt, dir);
            float discriminant = lengthsq(dotProduct) + lengthsq(radius) - lengthsq(pt);

            if (discriminant < 0.0f)
            {
                // Max speed circle fully invalidates line lineNo.
                return false;
            }

            ORCALine lineA;
            float2 dirA, ptA;

            float sqrtDiscriminant = sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {

                lineA = lines[i]; dirA = lineA.dir; ptA = lineA.point;

                float denominator = Det(dir, dirA);
                float numerator = Det(dirA, pt - ptA);

                if (abs(denominator) <= EPSILON)
                {
                    // Lines lineNo and i are (almost) parallel.
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    // Line i bounds line lineNo on the right.
                    tRight = min(tRight, t);
                }
                else
                {
                    // Line i bounds line lineNo on the left.
                    tLeft = max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (dirOpt)
            {
                // Optimize direction.
                if (dot(optVel, dir) > 0.0f)
                {
                    // Take right extreme.
                    result = pt + tRight * dir;
                }
                else
                {
                    // Take left extreme.
                    result = pt + tLeft * dir;
                }
            }
            else
            {
                // Optimize closest point.
                float t = dot(dir, (optVel - pt));

                if (t < tLeft)
                {
                    result = pt + tLeft * dir;
                }
                else if (t > tRight)
                {
                    result = pt + tRight * dir;
                }
                else
                {
                    result = pt + t * dir;
                }
            }

            return true;
        }

        /// <summary>
        /// Solves a two-dimensional linear program subject to linear 
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="optVel">The optimization velocity.</param>
        /// <param name="dirOpt">True if the direction should be optimized.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        /// <returns>The number of the line it fails on, and the number of lines if successful.</returns>
        private int LP2(NativeList<ORCALine> lines, float radius, float2 optVel, bool dirOpt, ref float2 result)
        {
            if (dirOpt)
            {
                // Optimize direction. Note that the optimization velocity is of
                // unit length in this case.
                result = optVel * radius;
            }
            else if (lengthsq(optVel) > (radius * radius))
            {
                // Optimize closest point and outside circle.
                result = normalize(optVel) * radius;
            }
            else
            {
                // Optimize closest point and inside circle.
                result = optVel;
            }

            for (int i = 0, count = lines.Length; i < count; ++i)
            {
                if (Det(lines[i].dir, lines[i].point - result) > 0.0f)
                {
                    // Result does not satisfy constraint i. Compute new optimal result.
                    float2 tempResult = result;
                    if (!LP1(lines, i, radius, optVel, dirOpt, ref result))
                    {
                        result = tempResult;
                        return i;
                    }
                }
            }

            return lines.Length;
        }

        /// <summary>
        /// Solves a two-dimensional linear program subject to linear
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="numObstLines">Count of obstacle lines.</param>
        /// <param name="beginLine">The line on which the 2-d linear program failed.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        private void LP3(NativeList<ORCALine> lines, int numObstLines, int beginLine, float radius, ref float2 result)
        {
            float distance = 0.0f;

            ORCALine lineA, lineB;
            float2 dirA, ptA, dirB, ptB;

            for (int i = beginLine, iCount = lines.Length; i < iCount; ++i)
            {
                lineA = lines[i]; dirA = lineA.dir; ptA = lineA.point;

                if (Det(dirA, ptA - result) > distance)
                {
                    // Result does not satisfy constraint of line i.
                    NativeList<ORCALine> projLines = new NativeList<ORCALine>(numObstLines, Allocator.Temp);

                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {

                        lineB = lines[j]; dirB = lineB.dir; ptB = lineB.point;

                        ORCALine line = new ORCALine();
                        float determinant = Det(dirA, dirB);

                        if (abs(determinant) <= EPSILON)
                        {
                            // Line i and line j are parallel.
                            if (dot(dirA, dirB) > 0.0f)
                            {
                                // Line i and line j point in the same direction.
                                continue;
                            }
                            else
                            {
                                // Line i and line j point in opposite direction.
                                line.point = 0.5f * (ptA + ptB);
                            }
                        }
                        else
                        {
                            line.point = ptA + (Det(dirB, ptA - ptB) / determinant) * dirA;
                        }

                        line.dir = normalize(dirB - dirA);
                        projLines.Add(line);
                    }

                    float2 tempResult = result;
                    if (LP2(projLines, radius, float2(-dirA.y, dirA.x), true, ref result) < projLines.Length)
                    {
                        // This should in principle not happen. The result is by
                        // definition already in the feasible region of this
                        // linear program. If it fails, it is due to small
                        // floating point error, and the current result is kept.
                        result = tempResult;
                    }

                    distance = Det(dirA, ptA - result);

                    //projLines.Dispose(); //Burst doesn't like this.
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
        /// Computes the squared distance from a line segment with the specified endpoints to a specified point.
        /// </summary>
        /// <param name="a">The first endpoint of the line segment.</param>
        /// <param name="b">The second endpoint of the line segment.</param>
        /// <param name="c">The point to which the squared distance is to be calculated.</param>
        /// <returns>The squared distance from the line segment to the point.</returns>
        private float DistSqPointLineSegment(float2 a, float2 b, float2 c)
        {

            //TODO : inline operations instead of calling shorthands
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
