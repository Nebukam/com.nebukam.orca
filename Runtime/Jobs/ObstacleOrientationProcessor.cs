using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{

    public class ObstacleOrientationProcessor : ParallelProcessor<ObstacleOrientationJob>
    {

        protected IObstacleProvider m_obstaclesProvider;
        public IObstacleProvider obstaclesProvider { get { return m_obstaclesProvider; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override int Prepare(ref ObstacleOrientationJob job, float delta)
        {
            
            if (!TryGetFirstInGroup(out m_obstaclesProvider, true))
            {
                throw new System.Exception("No IObstacleProvider or IObstacleSplitProvider in chain !");
            }

            if (!m_obstaclesProvider.recompute) { return 0; }

            job.m_inputObstacleInfos = m_obstaclesProvider.outputObstacleInfos;
            job.m_referenceObstacles = m_obstaclesProvider.referenceObstacles;
            job.m_inputObstacles = m_obstaclesProvider.outputObstacles;
            
            return m_obstaclesProvider.outputObstacles.Length;

        }

        protected override void Apply(ref ObstacleOrientationJob job)
        {

        }

    }
}
