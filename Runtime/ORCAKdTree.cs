using System.Collections.Generic;
using UnityEngine;
using Nebukam.Utils;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    /// <summary>
    /// Defines k-D trees for agents and static obstacles in the simulation.
    /// </summary>
    internal class ORCAKdTree : SolverChild
    {
        /// <summary>
        /// Defines a node of an agent k-D tree.
        /// </summary>
        private struct AgentTreeNode
        {
            internal int begin;
            internal int end;
            internal int left;
            internal int right;
            internal float maxX;
            internal float maxY;
            internal float minX;
            internal float minY;
        }

        /// <summary>
        /// Defines a pair of scalar values.
        /// </summary>
        private struct FloatPair
        {
            private float m_a;
            private float m_b;

            /// <summary>
            /// Constructs and initializes a pair of scalar values.
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            internal FloatPair(float a, float b)
            {
                m_a = a;
                m_b = b;
            }
            
            /// <summary>
            /// Returns true if the first pair of scalar values is less 
            /// than the second pair of scalar values.
            /// </summary>
            /// <param name="ab1">The first pair of scalar values.</param>
            /// <param name="ab2">The second pair of scalar values.</param>
            /// <returns>True if the first pair of scalar values is less than the
            /// second pair of scalar values.</returns>
            public static bool operator <(FloatPair ab1, FloatPair ab2)
            {
                return ab1.m_a < ab2.m_a || !(ab2.m_a < ab1.m_a) && ab1.m_b < ab2.m_b;
            }
            
            /// <summary>
            /// Returns true if the first pair of scalar values is less
            /// than or equal to the second pair of scalar values.
            /// </summary>
            /// <param name="ab1">The first pair of scalar values.</param>
            /// <param name="ab2">The second pair of scalar values.</param>
            /// <returns>True if the first pair of scalar values is less than or
            /// equal to the second pair of scalar values.</returns>
            public static bool operator <=(FloatPair ab1, FloatPair ab2)
            {
                return (ab1.m_a == ab2.m_a && ab1.m_b == ab2.m_b) || ab1 < ab2;
            }

            /// <summary>
            /// Returns true if the first pair of scalar values is
            /// greater than the second pair of scalar values.
            /// </summary>
            /// <param name="ab1">The first pair of scalar values.</param>
            /// <param name="ab2">The second pair of scalar values.</param>
            /// <returns>True if the first pair of scalar values is greater than
            /// the second pair of scalar values.</returns>
            public static bool operator >(FloatPair ab1, FloatPair ab2)
            {
                return !(ab1 <= ab2);
            }
            
            /// <summary>
            /// Returns true if the first pair of scalar values is
            /// greater than or equal to the second pair of scalar values.
            /// </summary>
            /// <param name="ab1">The first pair of scalar values.</param>
            /// <param name="ab2">The second pair of scalar values.</param>
            /// <returns>True if the first pair of scalar values is greater than
            /// or equal to the second pair of scalar values.</returns>
            public static bool operator >=(FloatPair ab1, FloatPair ab2)
            {
                return !(ab1 < ab2);
            }
        }

        /// <summary>
        /// Defines a node of an obstacle k-D tree.
        /// </summary>
        private class ObstacleTreeNode
        {
            internal Obstacle obstacle;
            internal ObstacleTreeNode left;
            internal ObstacleTreeNode right;

            /// <summary>
            /// Clear the Tree Node recursively
            /// </summary>
            internal void ClearRecursive()
            {
                obstacle?.ClearRecursive();
                obstacle = null;

                left?.ClearRecursive();
                left = null;

                right?.ClearRecursive();
                right = null;

                //TODO : Return to pool.
            }
        };

        /// <summary>
        /// The maximum size of an agent k-D tree leaf.
        /// </summary>
        private const int MAX_LEAF_SIZE = 10;

        private ORCAAgent[] m_agents;
        private AgentTreeNode[] m_agentTree;
        private ObstacleTreeNode m_obstacleTree;
        private ObstacleTreeNode m_dynamicObstacleTree;

        /// <summary>
        /// Builds an agent k-D tree.
        /// </summary>
        internal void BuildAgentTree()
        {
            if (m_agents == null || m_agents.Length != m_solver.m_agents.Count)
            {
                m_agents = new ORCAAgent[m_solver.m_agents.Count];

                for (int i = 0; i < m_agents.Length; ++i)
                {
                    m_agents[i] = m_solver.m_agents[i];
                }

                m_agentTree = new AgentTreeNode[2 * m_agents.Length];

                for (int i = 0; i < m_agentTree.Length; ++i)
                {
                    m_agentTree[i] = new AgentTreeNode();
                }
            }

            if (m_agents.Length != 0)
            {
                BuildAgentTreeRecursive(0, m_agents.Length, 0);
            }
        }

        /// <summary>
        /// Builds an obstacle k-D tree.
        /// </summary>
        internal void BuildObstacleTree()
        {
            m_obstacleTree = new ObstacleTreeNode();

            IList<Obstacle> o = m_solver.m_obstacles,
             obstacles = new List<Obstacle>(o.Count);

            for (int i = 0; i < o.Count; ++i)
            {
                obstacles.Add(o[i]);
            }

            m_obstacleTree = BuildObstacleTreeRecursive(obstacles);
        }

        /// <summary>
        /// Rebuilds the dynamic obstacle k-D tree.
        /// </summary>
        internal void BuildDynamicObstacleTree()
        {
            m_dynamicObstacleTree = new ObstacleTreeNode();

            IList<Obstacle> o = m_solver.m_dynamicObstacles,
             obstacles = new List<Obstacle>(o.Count);

            for (int i = 0; i < o.Count; ++i)
            {
                obstacles.Add(o[i]);
            }

            m_dynamicObstacleTree = BuildObstacleTreeRecursive(obstacles);
        }
        
        /// <summary>
        /// Computes the agent neighbors of the specified agent.
        /// </summary>
        /// <param name="agent">The agent for which agent neighbors are to be computed.</param>
        /// <param name="rangeSq">The squared range around the agent.</param>
        internal void ComputeAgentNeighbors(ORCAAgent agent, ref float rangeSq)
        {
            QueryAgentTreeRecursive(agent, ref rangeSq, 0);
        }
        
        /// <summary>
        /// Computes the obstacle neighbors of the specified agent.
        /// </summary>
        /// <param name="agent">The agent for which obstacle neighbors are to be computed.</param>
        /// <param name="rangeSq">The squared range around the agent.</param>
        internal void ComputeObstacleNeighbors(ORCAAgent agent, float rangeSq)
        {
            QueryObstacleTreeRecursive(agent, rangeSq, m_obstacleTree);
            QueryObstacleTreeRecursive(agent, rangeSq, m_dynamicObstacleTree);
        }

        /// <summary>
        /// Queries the visibility between two points within a specified radius.
        /// </summary>
        /// <param name="q1">The first point between which visibility is to be tested</param>
        /// <param name="q2">The second point between which visibility is to be tested</param>
        /// <param name="radius">The radius within which visibility is to be tested</param>
        /// <returns>True if q1 and q2 are mutually visible within the radius; false otherwise.</returns>
        internal bool QueryVisibility(float2 q1, float2 q2, float radius)
        {
            return QueryVisibilityRecursive(q1, q2, radius, m_obstacleTree);
        }

        internal int QueryNearAgent(float2 point, float radius)
        {
            float rangeSq = float.MaxValue;
            int agentNo = -1;
            QueryAgentTreeRecursive(point, ref rangeSq, ref agentNo, 0);
            if (rangeSq < radius*radius)
                return agentNo;
            return -1;
        }

        /// <summary>
        /// Recursive method for building an agent k-D tree.
        /// </summary>
        /// <param name="begin">The beginning agent k-D tree node node index.</param>
        /// <param name="end">The ending agent k-D tree node index.</param>
        /// <param name="node">The current agent k-D tree node index.</param>
        private void BuildAgentTreeRecursive(int begin, int end, int node)
        {
            m_agentTree[node].begin = begin;
            m_agentTree[node].end = end;
            m_agentTree[node].minX = m_agentTree[node].maxX = m_agents[begin].m_position.x;
            m_agentTree[node].minY = m_agentTree[node].maxY = m_agents[begin].m_position.y;

            for (int i = begin + 1; i < end; ++i)
            {
                m_agentTree[node].maxX = Mathf.Max(m_agentTree[node].maxX, m_agents[i].m_position.x);
                m_agentTree[node].minX = Mathf.Min(m_agentTree[node].minX, m_agents[i].m_position.x);
                m_agentTree[node].maxY = Mathf.Max(m_agentTree[node].maxY, m_agents[i].m_position.y);
                m_agentTree[node].minY = Mathf.Min(m_agentTree[node].minY, m_agents[i].m_position.y);
            }

            if (end - begin > MAX_LEAF_SIZE)
            {
                // No leaf node.
                bool isVertical = m_agentTree[node].maxX - m_agentTree[node].minX > m_agentTree[node].maxY - m_agentTree[node].minY;
                float splitValue = 0.5f * (isVertical ? m_agentTree[node].maxX + m_agentTree[node].minX : m_agentTree[node].maxY + m_agentTree[node].minY);

                int left = begin;
                int right = end;

                while (left < right)
                {
                    while (left < right && (isVertical ? m_agents[left].m_position.x : m_agents[left].m_position.y) < splitValue)
                    {
                        ++left;
                    }

                    while (right > left && (isVertical ? m_agents[right - 1].m_position.x : m_agents[right - 1].m_position.y) >= splitValue)
                    {
                        --right;
                    }

                    if (left < right)
                    {
                        ORCAAgent tempAgent = m_agents[left];
                        m_agents[left] = m_agents[right - 1];
                        m_agents[right - 1] = tempAgent;
                        ++left;
                        --right;
                    }
                }

                int leftSize = left - begin;

                if (leftSize == 0)
                {
                    ++leftSize;
                    ++left;
                    ++right;
                }

                m_agentTree[node].left = node + 1;
                m_agentTree[node].right = node + 2 * leftSize;

                BuildAgentTreeRecursive(begin, left, m_agentTree[node].left);
                BuildAgentTreeRecursive(left, end, m_agentTree[node].right);
            }
        }
        
        /// <summary>
        /// Recursive method for building an obstacle k-D tree.
        /// </summary>
        /// <param name="obstacles">A list of obstacles.</param>
        /// <returns>An obstacle k-D tree node.</returns>
        private ObstacleTreeNode BuildObstacleTreeRecursive(IList<Obstacle> obstacles)
        {
            if (obstacles.Count == 0)
            {
                return null;
            }

            ObstacleTreeNode node = new ObstacleTreeNode();

            int optimalSplit = 0;
            int minLeft = obstacles.Count;
            int minRight = obstacles.Count;

            for (int i = 0; i < obstacles.Count; ++i)
            {
                int leftSize = 0;
                int rightSize = 0;

                Obstacle obstacleI1 = obstacles[i];
                Obstacle obstacleI2 = obstacleI1.next;

                // Compute optimal split node.
                for (int j = 0; j < obstacles.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Obstacle obstacleJ1 = obstacles[j];
                    Obstacle obstacleJ2 = obstacleJ1.next;

                    float j1LeftOfI = Maths.LeftOf(obstacleI1.point, obstacleI2.point, obstacleJ1.point);
                    float j2LeftOfI = Maths.LeftOf(obstacleI1.point, obstacleI2.point, obstacleJ2.point);

                    if (j1LeftOfI >= -Maths.EPSILON && j2LeftOfI >= -Maths.EPSILON)
                    {
                        ++leftSize;
                    }
                    else if (j1LeftOfI <= Maths.EPSILON && j2LeftOfI <= Maths.EPSILON)
                    {
                        ++rightSize;
                    }
                    else
                    {
                        ++leftSize;
                        ++rightSize;
                    }

                    if (new FloatPair(Mathf.Max(leftSize, rightSize), Mathf.Min(leftSize, rightSize)) >= new FloatPair(Mathf.Max(minLeft, minRight), Mathf.Min(minLeft, minRight)))
                    {
                        break;
                    }
                }

                if (new FloatPair(Mathf.Max(leftSize, rightSize), Mathf.Min(leftSize, rightSize)) < new FloatPair(Mathf.Max(minLeft, minRight), Mathf.Min(minLeft, minRight)))
                {
                    minLeft = leftSize;
                    minRight = rightSize;
                    optimalSplit = i;
                }
            }

            {
                // Build split node.
                IList<Obstacle> leftObstacles = new List<Obstacle>(minLeft);

                for (int n = 0; n < minLeft; ++n)
                {
                    leftObstacles.Add(null);
                }

                IList<Obstacle> rightObstacles = new List<Obstacle>(minRight);

                for (int n = 0; n < minRight; ++n)
                {
                    rightObstacles.Add(null);
                }

                int leftCounter = 0;
                int rightCounter = 0;
                int i = optimalSplit;

                Obstacle obstacleI1 = obstacles[i];
                Obstacle obstacleI2 = obstacleI1.next;

                for (int j = 0; j < obstacles.Count; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    Obstacle obstacleJ1 = obstacles[j];
                    Obstacle obstacleJ2 = obstacleJ1.next;

                    float j1LeftOfI = Maths.LeftOf(obstacleI1.point, obstacleI2.point, obstacleJ1.point);
                    float j2LeftOfI = Maths.LeftOf(obstacleI1.point, obstacleI2.point, obstacleJ2.point);

                    if (j1LeftOfI >= -Maths.EPSILON && j2LeftOfI >= -Maths.EPSILON)
                    {
                        leftObstacles[leftCounter++] = obstacles[j];
                    }
                    else if (j1LeftOfI <= Maths.EPSILON && j2LeftOfI <= Maths.EPSILON)
                    {
                        rightObstacles[rightCounter++] = obstacles[j];
                    }
                    else
                    {
                        // Split obstacle j.
                        float t = Maths.Det(obstacleI2.point - obstacleI1.point, obstacleJ1.point - obstacleI1.point) / Maths.Det(obstacleI2.point - obstacleI1.point, obstacleJ1.point - obstacleJ2.point);

                        float2 splitPoint = obstacleJ1.point + t * (obstacleJ2.point - obstacleJ1.point);

                        Obstacle newObstacle = new Obstacle();
                        newObstacle.point = splitPoint;
                        newObstacle.prev = obstacleJ1;
                        newObstacle.next = obstacleJ2;
                        newObstacle.convex = true;
                        newObstacle.dir = obstacleJ1.dir;

                        newObstacle.id = m_solver.m_obstacles.Count;

                        m_solver.m_obstacles.Add(newObstacle);

                        obstacleJ1.next = newObstacle;
                        obstacleJ2.prev = newObstacle;

                        if (j1LeftOfI > 0.0f)
                        {
                            leftObstacles[leftCounter++] = obstacleJ1;
                            rightObstacles[rightCounter++] = newObstacle;
                        }
                        else
                        {
                            rightObstacles[rightCounter++] = obstacleJ1;
                            leftObstacles[leftCounter++] = newObstacle;
                        }
                    }
                }

                node.obstacle = obstacleI1;
                node.left = BuildObstacleTreeRecursive(leftObstacles);
                node.right = BuildObstacleTreeRecursive(rightObstacles);

                return node;
            }
        }

        private void QueryAgentTreeRecursive(float2 position, ref float rangeSq, ref int agentNo, int node)
        {
            if (m_agentTree[node].end - m_agentTree[node].begin <= MAX_LEAF_SIZE)
            {
                for (int i = m_agentTree[node].begin; i < m_agentTree[node].end; ++i)
                {
                    float distSq = lengthsq(position - m_agents[i].m_position);
                    if (distSq < rangeSq)
                    {
                        rangeSq = distSq;
                        agentNo = m_agents[i].m_id;
                    }
                }
            }
            else
            {
                float distSqLeft = lengthsq(max(0.0f, m_agentTree[m_agentTree[node].left].minX - position.x)) 
                    + lengthsq(max(0.0f, position.x - m_agentTree[m_agentTree[node].left].maxX)) 
                    + lengthsq(max(0.0f, m_agentTree[m_agentTree[node].left].minY - position.y))
                    + lengthsq(max(0.0f, position.y - m_agentTree[m_agentTree[node].left].maxY));
                float distSqRight = lengthsq(max(0.0f, m_agentTree[m_agentTree[node].right].minX - position.x))
                    + lengthsq(max(0.0f, position.x - m_agentTree[m_agentTree[node].right].maxX))
                    + lengthsq(max(0.0f, m_agentTree[m_agentTree[node].right].minY - position.y))
                    + lengthsq(max(0.0f, position.y - m_agentTree[m_agentTree[node].right].maxY));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryAgentTreeRecursive(position, ref rangeSq, ref agentNo, m_agentTree[node].left);

                        if (distSqRight < rangeSq)
                        {
                            QueryAgentTreeRecursive(position, ref rangeSq, ref agentNo, m_agentTree[node].right);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryAgentTreeRecursive(position, ref rangeSq, ref agentNo, m_agentTree[node].right);

                        if (distSqLeft < rangeSq)
                        {
                            QueryAgentTreeRecursive(position, ref rangeSq, ref agentNo, m_agentTree[node].left);
                        }
                    }
                }

            }
        }
        
        /// <summary>
        /// Recursive method for computing the agent neighbors of the specified agent.
        /// </summary>
        /// <param name="agent">The agent for which agent neighbors are to be computed.</param>
        /// <param name="rangeSq">The squared range around the agent.</param>
        /// <param name="node">The current agent k-D tree node index.</param>
        private void QueryAgentTreeRecursive(ORCAAgent agent, ref float rangeSq, int node)
        {
            if (m_agentTree[node].end - m_agentTree[node].begin <= MAX_LEAF_SIZE)
            {
                ORCAAgent a;
                for (int i = m_agentTree[node].begin; i < m_agentTree[node].end; ++i)
                {
                    a = m_agents[i];
                    if (!a.m_collisionEnabled) { continue; }
                    agent.InsertAgentNeighbor(a, ref rangeSq);
                }
            }
            else
            {
                float distSqLeft = lengthsq(max(0.0f, m_agentTree[m_agentTree[node].left].minX - agent.m_position.x))
                    + lengthsq(max(0.0f, agent.m_position.x - m_agentTree[m_agentTree[node].left].maxX))
                    + lengthsq(max(0.0f, m_agentTree[m_agentTree[node].left].minY - agent.m_position.y))
                    + lengthsq(max(0.0f, agent.m_position.y - m_agentTree[m_agentTree[node].left].maxY));
                float distSqRight = lengthsq(max(0.0f, m_agentTree[m_agentTree[node].right].minX - agent.m_position.x))
                    + lengthsq(max(0.0f, agent.m_position.x - m_agentTree[m_agentTree[node].right].maxX))
                    + lengthsq(max(0.0f, m_agentTree[m_agentTree[node].right].minY - agent.m_position.y))
                    + lengthsq(max(0.0f, agent.m_position.y - m_agentTree[m_agentTree[node].right].maxY));

                if (distSqLeft < distSqRight)
                {
                    if (distSqLeft < rangeSq)
                    {
                        QueryAgentTreeRecursive(agent, ref rangeSq, m_agentTree[node].left);

                        if (distSqRight < rangeSq)
                        {
                            QueryAgentTreeRecursive(agent, ref rangeSq, m_agentTree[node].right);
                        }
                    }
                }
                else
                {
                    if (distSqRight < rangeSq)
                    {
                        QueryAgentTreeRecursive(agent, ref rangeSq, m_agentTree[node].right);

                        if (distSqLeft < rangeSq)
                        {
                            QueryAgentTreeRecursive(agent, ref rangeSq, m_agentTree[node].left);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Recursive method for computing the obstacle neighbors of the specified agent.
        /// </summary>
        /// <param name="agent">The agent for which obstacle neighbors are to be computed</param>
        /// <param name="rangeSq">The squared range around the agent.</param>
        /// <param name="node">The current obstacle k-D node.</param>
        private void QueryObstacleTreeRecursive(ORCAAgent agent, float rangeSq, ObstacleTreeNode node)
        {
            if (node != null)
            {
                Obstacle obstacle1 = node.obstacle;
                Obstacle obstacle2 = obstacle1.next;

                float agentLeftOfLine = Maths.LeftOf(obstacle1.point, obstacle2.point, agent.m_position);

                QueryObstacleTreeRecursive(agent, rangeSq, agentLeftOfLine >= 0.0f ? node.left : node.right);

                float distSqLine = lengthsq(agentLeftOfLine) / lengthsq(obstacle2.point - obstacle1.point);

                if (distSqLine < rangeSq)
                {
                    if (agentLeftOfLine < 0.0f)
                    {
                        // Try obstacle at this node only if agent is on right side of
                        // obstacle (and can see obstacle).
                        agent.InsertObstacleNeighbor(node.obstacle, rangeSq);
                    }

                    // Try other side of line.
                    QueryObstacleTreeRecursive(agent, rangeSq, agentLeftOfLine >= 0.0f ? node.right : node.left);
                }
            }
        }

        /// <summary>
        /// Recursive method for querying the visibility between two points within a specified radius.
        /// </summary>
        /// <param name="q1">The first point between which visibility is to be tested</param>
        /// <param name="q2">The second point between which visibility is to be tested</param>
        /// <param name="radius">The radius within which visibility is to be tested</param>
        /// <param name="node">The current obstacle k-D node.</param>
        /// <returns>True if q1 and q2 are mutually visible within the radius; false otherwise.</returns>
        private bool QueryVisibilityRecursive(float2 q1, float2 q2, float radius, ObstacleTreeNode node)
        {
            if (node == null)
            {
                return true;
            }

            Obstacle obstacle1 = node.obstacle;
            Obstacle obstacle2 = obstacle1.next;

            float q1LeftOfI = Maths.LeftOf(obstacle1.point, obstacle2.point, q1);
            float q2LeftOfI = Maths.LeftOf(obstacle1.point, obstacle2.point, q2);
            float invLengthI = 1.0f / lengthsq(obstacle2.point - obstacle1.point);

            float rSqr = lengthsq(radius);

            if (q1LeftOfI >= 0.0f && q2LeftOfI >= 0.0f)
            {
                return QueryVisibilityRecursive(q1, q2, radius, node.left) 
                    && ((lengthsq(q1LeftOfI) * invLengthI >= rSqr && lengthsq(q2LeftOfI) * invLengthI >= rSqr) || QueryVisibilityRecursive(q1, q2, radius, node.right));
            }

            if (q1LeftOfI <= 0.0f && q2LeftOfI <= 0.0f)
            {
                return QueryVisibilityRecursive(q1, q2, radius, node.right) 
                    && ((lengthsq(q1LeftOfI) * invLengthI >= rSqr && lengthsq(q2LeftOfI) * invLengthI >= rSqr) || QueryVisibilityRecursive(q1, q2, radius, node.left));
            }

            if (q1LeftOfI >= 0.0f && q2LeftOfI <= 0.0f)
            {
                // One can see through obstacle from left to right.
                return QueryVisibilityRecursive(q1, q2, radius, node.left) 
                    && QueryVisibilityRecursive(q1, q2, radius, node.right);
            }

            float point1LeftOfQ = Maths.LeftOf(q1, q2, obstacle1.point);
            float point2LeftOfQ = Maths.LeftOf(q1, q2, obstacle2.point);
            float invLengthQ = 1.0f / lengthsq(q2 - q1);

            return point1LeftOfQ * point2LeftOfQ >= 0.0f 
                && lengthsq(point1LeftOfQ) * invLengthQ > rSqr && lengthsq(point2LeftOfQ) * invLengthQ > rSqr 
                && QueryVisibilityRecursive(q1, q2, radius, node.left) 
                && QueryVisibilityRecursive(q1, q2, radius, node.right);
        }
    }
}
