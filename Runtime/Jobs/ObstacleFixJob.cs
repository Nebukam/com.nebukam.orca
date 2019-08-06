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
    public struct ObstacleFixJob : IJob
    {

        
        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_referenceObstacles;
        public NativeArray<ObstacleVertexData> m_inputObstacles;
        
        public void Execute()
        {
            m_referenceObstacles.CopyTo(m_inputObstacles);
        }

    }
}
