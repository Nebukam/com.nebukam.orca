using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{
    public class ORCAProcessor : ParallelProcessor<ORCAJob>
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        protected IAgentProvider m_agentProvider;
        public IAgentProvider agentProvider { get { return m_agentProvider; } }

        protected IAgentKDTreeProvider m_agentKDTreeProvider;
        public IAgentKDTreeProvider distributionProvider { get { return m_agentKDTreeProvider; } }

        protected IObstacleProvider m_obstaclesProvider;
        public IObstacleProvider obstaclesProvider { get { return m_obstaclesProvider; } }

        protected IObstacleKDTreeProvider m_obstacleKDTreeProvider;
        public IObstacleKDTreeProvider obstacleKDTreeProvider { get { return m_obstacleKDTreeProvider; } }

        protected NativeArray<AgentDataResult> m_results = new NativeArray<AgentDataResult>(0, Allocator.Persistent);
        public NativeArray<AgentDataResult> results { get { return m_results; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override int Prepare(ref ORCAJob job, float delta)
        {

            if (!TryGetFirstInGroup(out m_agentProvider, true)
                || !TryGetFirstInGroup(out m_obstaclesProvider, true)
                || !TryGetFirstInGroup(out m_agentKDTreeProvider, true)
                || !TryGetFirstInGroup(out m_obstacleKDTreeProvider, true))
            {
                string msg = string.Format("Missing provider : IAgentProvider = {0}, IObstacleProvider = {1}, IAgentKDTreeProvider = {2}, IObstacleKDTreeProvider = {3}, group = {4}",
                    m_agentProvider,
                    m_obstaclesProvider, 
                    m_agentKDTreeProvider, 
                    m_obstacleKDTreeProvider, m_group);

                throw new System.Exception(msg);
            }

            int agentCount = m_agentProvider.outputAgents.Length;
            if (m_results.Length != agentCount)
            {
                m_results.Dispose();
                m_results = new NativeArray<AgentDataResult>(agentCount, Allocator.Persistent);
            }

            //Agent data
            job.m_inputAgents = m_agentProvider.outputAgents;
            job.m_inputAgentTree = m_agentKDTreeProvider.outputTree;

            //Obstacles data
            job.m_inputObstacleInfos = m_obstaclesProvider.outputObstacleInfos;
            job.m_referenceObstacles = m_obstaclesProvider.referenceObstacles;
            job.m_inputObstacles = m_obstaclesProvider.outputObstacles;
            job.m_inputObstacleTree = m_obstacleKDTreeProvider.outputTree;

            job.m_results = m_results;
            job.m_timestep = delta / 0.25f;

            return agentCount;

        }

        protected override void Apply(ref ORCAJob job)
        {
            
            NativeArray<AgentData> data = m_agentProvider.outputAgents;

            Agent agent;
            List<Agent> agentList = m_agentProvider.lockedAgents;

            AgentDataResult result;

            for (int i = 0, count = m_results.Length; i < count; i++)
            {
                result = m_results[i];
                agent = agentList[data[i].index];
                agent.pos = float3(result.position, agent.pos.z);
                agent.velocity = result.velocity;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { return; }
            
            m_results.Dispose();
        }

    }
}
