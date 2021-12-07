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
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    [BurstCompile]
    public struct ObstacleKDTreeJob : IJob
    {

        private const float EPSILON = 0.00001f;

        public bool m_recompute;

        [ReadOnly]
        public NativeArray<ObstacleInfos> m_inputObstacleInfos;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_referenceObstacles;
        public NativeArray<ObstacleVertexData> m_inputObstacles;
        public NativeArray<ObstacleTreeNode> m_outputTree;

        public void Execute()
        {

            if (!m_recompute) { return; }

            //BuildObstacleTreeRecursive((NativeArray<ObstacleVertexData>)m_inputSplitObstacles);

            int obsCount = m_inputObstacles.Length;
            if (obsCount == 0) { return; }

            //for (int i = 0, count = obsCount; i < count; i++)
            //    m_outputTree[i] = new ObstacleTreeNode());

            BuildAgentTreeRecursive(0, obsCount, 0);

        }

        /// <summary>
        /// Recursive method for building an agent k-D tree.
        /// </summary>
        /// <param name="begin">The beginning agent k-D tree node node index.</param>
        /// <param name="end">The ending agent k-D tree node index.</param>
        /// <param name="node">The current agent k-D tree node index.</param>
        private void BuildAgentTreeRecursive(int begin, int end, int node)
        {

            ObstacleTreeNode treeNode = m_outputTree[node];
            ObstacleVertexData obstacle = m_inputObstacles[begin];
            float2 pos;
            float minX, minY, maxX, maxY;

            treeNode.begin = begin;
            treeNode.end = end;
            minX = maxX = obstacle.pos.x;
            minY = maxY = obstacle.pos.y;

            for (int i = begin + 1; i < end; ++i)
            {
                pos = m_inputObstacles[i].pos;
                maxX = max(maxX, pos.x);
                minX = min(minX, pos.x);
                maxY = max(maxY, pos.y);
                minY = min(minY, pos.y);
            }

            treeNode.minX = minX;
            treeNode.maxX = maxX;
            treeNode.minY = minY;
            treeNode.maxY = maxY;

            m_outputTree[node] = treeNode;

            if (end - begin > ObstacleTreeNode.MAX_LEAF_SIZE)
            {
                // No leaf node.
                bool isVertical = treeNode.maxX - treeNode.minX > treeNode.maxY - treeNode.minY;
                float splitValue = 0.5f * (isVertical ? treeNode.maxX + treeNode.minX : treeNode.maxY + treeNode.minY);

                int left = begin;
                int right = end;

                while (left < right)
                {
                    while (left < right && (isVertical ? m_inputObstacles[left].pos.x : m_inputObstacles[left].pos.y) < splitValue)
                    {
                        ++left;
                    }

                    while (right > left && (isVertical ? m_inputObstacles[right - 1].pos.x : m_inputObstacles[right - 1].pos.y) >= splitValue)
                    {
                        --right;
                    }

                    if (left < right)
                    {
                        ObstacleVertexData tempAgent = m_inputObstacles[left];
                        m_inputObstacles[left] = m_inputObstacles[right - 1];
                        m_inputObstacles[right - 1] = tempAgent;
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

                treeNode.left = node + 1;
                treeNode.right = node + 2 * leftSize;
                m_outputTree[node] = treeNode;

                BuildAgentTreeRecursive(begin, left, treeNode.left);
                BuildAgentTreeRecursive(left, end, treeNode.right);
            }
        }

        private float LeftOf(float2 a, float2 b, float2 c)
        {
            float x1 = a.x = c.x, y1 = a.y - c.y, x2 = b.x - a.x, y2 = b.y - a.y;
            return x1 * y2 - y1 * x2;
        }

        private float Det(float2 a, float2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

    }
}
