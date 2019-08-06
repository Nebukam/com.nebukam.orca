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


        public bool m_recompute;

        [ReadOnly]
        public NativeArray<ObstacleVertexData> m_referenceObstacles;
        public NativeArray<ObstacleVertexData> m_inputObstacles;
        
        public void Execute()
        {
            if (!m_recompute) { return; }
            m_referenceObstacles.CopyTo(m_inputObstacles);
        }

    }
}
