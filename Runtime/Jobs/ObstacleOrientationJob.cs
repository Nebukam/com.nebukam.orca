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

        [ReadOnly]
        public NativeArray<ObstacleInfos> m_inputObstacleInfos;

        public NativeArray<ObstacleVertexData> m_referenceObstacles;
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_inputObstacles;
        
        public void Execute(int index)
        {

            //Compute whether a vertex is convex or concave
            //as well as its direction
            ObstacleVertexData vertex = m_inputObstacles[index];
            float2 pos = vertex.pos,
                nextPos = m_inputObstacles[vertex.next].pos,
                prevPos = m_inputObstacles[vertex.prev].pos;

            ObstacleInfos infos = m_inputObstacleInfos[vertex.infos];
            
            if (infos.length == 2)
            {
                vertex.convex = true;
            }
            else
            {
                vertex.convex = LeftOf(prevPos, pos, nextPos) >= 0.0f;
            }

            vertex.dir = normalize(nextPos - pos);
            vertex.normal = normalize(float2(-(pos.y - nextPos.y), pos.x - nextPos.x));

            m_referenceObstacles[index] = vertex;

        }

        private float LeftOf(float2 a, float2 b, float2 c)
        {
            float x1 = a.x - c.x, y1 = a.y - c.y, x2 = b.x - a.x, y2 = b.y - a.y;
            return x1 * y2 - y1 * x2;
        }

    }
}
