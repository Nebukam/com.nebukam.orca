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

//[NativeDisableParallelForRestriction]

namespace Nebukam.ORCA
{
    [BurstCompile]
    public struct ObstacleOrientationJob : IJobParallelFor
    {

        public bool m_recompute;

        [ReadOnly]
        public NativeArray<ObstacleInfos> m_inputObstacleInfos;

        public NativeArray<ObstacleVertexData> m_referenceObstacles;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_inputObstacles;

        public void Execute(int index)
        {

            if (!m_recompute) { return; }

            //Compute whether a vertex is convex or concave
            //as well as its direction
            ObstacleVertexData v = m_inputObstacles[index];
            float2 pos = v.pos,
                nextPos = m_inputObstacles[v.next].pos,
                prevPos = m_inputObstacles[v.prev].pos;

            ObstacleInfos infos = m_inputObstacleInfos[v.infos];

            if (infos.length == 2)//infos.edge || 
            {
                v.convex = true;
            }
            else
            {
                v.convex = LeftOf(prevPos, pos, nextPos) >= 0.0f;
            }

            //TODO : Fix obstacle direction... ?

            v.dir = normalize(nextPos - pos);
            m_referenceObstacles[index] = v;

        }

        private float LeftOf(float2 a, float2 b, float2 c)
        {
            float x1 = a.x - c.x, y1 = a.y - c.y, x2 = b.x - a.x, y2 = b.y - a.y;
            return x1 * y2 - y1 * x2;
        }

    }
}
