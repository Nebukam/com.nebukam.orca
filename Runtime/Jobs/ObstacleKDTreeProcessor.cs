using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{

    public interface IObstacleKDTreeProvider : IProcessor
    {
        NativeArray<ObstacleTreeNode> outputTree { get; }
    }

    public class ObstacleKDTreeProcessor : Processor<ObstacleKDTreeJob>, IObstacleKDTreeProvider
    {

        
        protected NativeArray<ObstacleTreeNode> m_outputTree = new NativeArray<ObstacleTreeNode>(0, Allocator.Persistent);
        public NativeArray<ObstacleTreeNode> outputTree { get { return m_outputTree; } }

        protected IObstacleProvider m_obstaclesProvider;
        public IObstacleProvider obstaclesProvider { get { return m_obstaclesProvider; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override void Prepare(ref ObstacleKDTreeJob job, float delta)
        {

            if (!TryGetFirstInGroup(out m_obstaclesProvider))
            {
                throw new System.Exception("No IObstacleSplitProvider or IObstacleProvider in chain !");
            }
            
            if (m_obstaclesProvider.recompute)
            {
                job.m_recompute = true;

                int obsCount = 2 * m_obstaclesProvider.referenceObstacles.Length;
                if (m_outputTree.Length != obsCount)
                {
                    m_outputTree.Dispose();
                    m_outputTree = new NativeArray<ObstacleTreeNode>(obsCount, Allocator.Persistent);
                }

            }
            else
            {
                job.m_recompute = false;
            }
            
            job.m_inputObstacleInfos = m_obstaclesProvider.outputObstacleInfos;
            job.m_referenceObstacles = m_obstaclesProvider.referenceObstacles;
            job.m_inputObstacles = m_obstaclesProvider.outputObstacles;
            job.m_outputTree = m_outputTree;


        }

        protected override void Apply(ref ObstacleKDTreeJob job)
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { return; }

            m_outputTree.Dispose();
        }

    }
}
