using UnityEngine;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.JobAssist;

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
