using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{
    public class AgentKDTreeProcessor : Processor<AgentKDTreeJob>, IAgentKDTreeProvider
    {


        protected IAgentProvider m_agentProvider;
        protected NativeArray<AgentTreeNode> m_outputTree = new NativeArray<AgentTreeNode>(0, Allocator.Persistent);

        public IAgentProvider agentProvider { get { return m_agentProvider; } }
        public NativeArray<AgentTreeNode> outputTree { get { return m_outputTree; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override void Prepare(ref AgentKDTreeJob job, float delta)
        {
            if (!TryGetFirstInGroup(out m_agentProvider))
            {
                throw new System.Exception("No IAgentProvider in chain !");
            }

            int agentCount = 2 * m_agentProvider.outputAgents.Length;
            if(m_outputTree.Length != agentCount)
            {
                m_outputTree.Dispose();
                m_outputTree = new NativeArray<AgentTreeNode>(agentCount, Allocator.Persistent);
            }

            job.m_inputAgents = m_agentProvider.outputAgents;
            job.m_outputTree = m_outputTree;

        }

        protected override void Apply(ref AgentKDTreeJob job)
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
