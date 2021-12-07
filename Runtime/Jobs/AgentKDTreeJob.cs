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
    public struct AgentKDTreeJob : IJob
    {

        public NativeArray<AgentData> m_inputAgents; //Not Read-only : we may need to re-order it.
        public NativeArray<AgentTreeNode> m_outputTree;

        public void Execute()
        {
            int agentCount = m_inputAgents.Length;
            if (agentCount == 0) { return; }

            //for (int i = 0, count = m_outputTree.Length; i < count; i++)
            //    m_outputTree[i] = new AgentTreeNode();

            BuildAgentTreeRecursive(0, agentCount, 0);
        }

        /// <summary>
        /// Recursive method for building an agent k-D tree.
        /// </summary>
        /// <param name="begin">The beginning agent k-D tree node node index.</param>
        /// <param name="end">The ending agent k-D tree node index.</param>
        /// <param name="node">The current agent k-D tree node index.</param>
        private void BuildAgentTreeRecursive(int begin, int end, int node)
        {

            AgentTreeNode treeNode = m_outputTree[node];
            AgentData agent = m_inputAgents[begin];
            float2 pos;

            treeNode.begin = begin;
            treeNode.end = end;
            treeNode.minX = treeNode.maxX = agent.position.x;
            treeNode.minY = treeNode.maxY = agent.position.y;

            for (int i = begin + 1; i < end; ++i)
            {
                pos = m_inputAgents[i].position;
                treeNode.maxX = max(treeNode.maxX, pos.x);
                treeNode.minX = min(treeNode.minX, pos.x);
                treeNode.maxY = max(treeNode.maxY, pos.y);
                treeNode.minY = min(treeNode.minY, pos.y);
            }

            m_outputTree[node] = treeNode;

            if (end - begin > AgentTreeNode.MAX_LEAF_SIZE)
            {
                // No leaf node.
                bool isVertical = treeNode.maxX - treeNode.minX > treeNode.maxY - treeNode.minY;
                float splitValue = 0.5f * (isVertical ? treeNode.maxX + treeNode.minX : treeNode.maxY + treeNode.minY);

                int left = begin;
                int right = end;

                while (left < right)
                {
                    while (left < right && (isVertical ? m_inputAgents[left].position.x : m_inputAgents[left].position.y) < splitValue)
                    {
                        ++left;
                    }

                    while (right > left && (isVertical ? m_inputAgents[right - 1].position.x : m_inputAgents[right - 1].position.y) >= splitValue)
                    {
                        --right;
                    }

                    if (left < right)
                    {
                        AgentData tempAgent = m_inputAgents[left], rep = m_inputAgents[right - 1];
                        tempAgent.kdIndex = right - 1;
                        rep.kdIndex = left;
                        m_inputAgents[left] = rep;
                        m_inputAgents[right - 1] = tempAgent;
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

    }
}
