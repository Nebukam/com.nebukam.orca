// Copyright (c) 2019 Timothé Lapetite - nebukam@gmail.com
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

using Nebukam.JobAssist;
using System.Collections.Generic;
using Unity.Collections;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public class ORCAProcessor : ParallelProcessor<ORCAJob>
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        /// 
        /// Fields
        /// 

        protected IAgentProvider m_agentProvider;
        protected IAgentKDTreeProvider m_agentKDTreeProvider;
        protected IStaticObstacleProvider m_staticObstaclesProvider;
        protected IStaticObstacleKDTreeProvider m_staticObstacleKDTreeProvider;
        protected IDynObstacleProvider m_dynObstaclesProvider;
        protected IDynObstacleKDTreeProvider m_dynObstacleKDTreeProvider;

        protected NativeArray<AgentDataResult> m_results = new NativeArray<AgentDataResult>(0, Allocator.Persistent);


        /// 
        /// Properties
        /// 

        public IAgentProvider agentProvider { get { return m_agentProvider; } }
        public IAgentKDTreeProvider distributionProvider { get { return m_agentKDTreeProvider; } }
        public IStaticObstacleProvider staticObstaclesProvider { get { return m_staticObstaclesProvider; } }
        public IStaticObstacleKDTreeProvider staticObstacleKDTreeProvider { get { return m_staticObstacleKDTreeProvider; } }
        public IDynObstacleProvider dynObstaclesProvider { get { return m_dynObstaclesProvider; } }
        public IDynObstacleKDTreeProvider dynObstacleKDTreeProvider { get { return m_dynObstacleKDTreeProvider; } }

        public NativeArray<AgentDataResult> results { get { return m_results; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override int Prepare(ref ORCAJob job, float delta)
        {

            if (!TryGetFirstInGroup(out m_agentProvider, true)
                || !TryGetFirstInGroup(out m_agentKDTreeProvider, true)
                || !TryGetFirstInGroup(out m_staticObstaclesProvider, true)
                || !TryGetFirstInGroup(out m_staticObstacleKDTreeProvider, true)
                || !TryGetFirstInGroup(out m_dynObstaclesProvider, true)
                || !TryGetFirstInGroup(out m_dynObstacleKDTreeProvider, true))
            {
                string msg = string.Format("Missing provider : Agents = {0}, Static obs = {1}, Agent KD = {2}, Static obs KD= {3}, " +
                    "Dyn obs = {5}, Dyn obs KD= {6}, group = {4}",
                    m_agentProvider,
                    m_staticObstaclesProvider, 
                    m_agentKDTreeProvider, 
                    m_staticObstacleKDTreeProvider, 
                    m_dynObstaclesProvider,
                    m_dynObstacleKDTreeProvider, m_group);

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

            //Static obstacles data
            job.m_staticObstacleInfos = m_staticObstaclesProvider.outputObstacleInfos;
            job.m_staticRefObstacles = m_staticObstaclesProvider.referenceObstacles;
            job.m_staticObstacles = m_staticObstaclesProvider.outputObstacles;
            job.m_staticObstacleTree = m_staticObstacleKDTreeProvider.outputTree;

            //Dynamic obstacles data
            job.m_dynObstacleInfos = m_dynObstaclesProvider.outputObstacleInfos;
            job.m_dynRefObstacles = m_dynObstaclesProvider.referenceObstacles;
            job.m_dynObstacles = m_dynObstaclesProvider.outputObstacles;
            job.m_dynObstacleTree = m_dynObstacleKDTreeProvider.outputTree;

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

            if (plane == AxisPair.XY)
            {
                for (int i = 0, count = results.Length; i < count; i++)
                {
                    result = results[i];
                    agent = agentList[data[i].index];
                    agent.pos = float3(result.position, agent.pos.z);
                    agent.velocity = float3(result.velocity, agent.velocity.z);
                }
            }
            else
            {
                for (int i = 0, count = results.Length; i < count; i++)
                {
                    result = results[i];
                    agent = agentList[data[i].index];
                    agent.pos = float3(result.position.x, agent.pos.y, result.position.y);
                    agent.velocity = float3(result.velocity.x, agent.velocity.y, result.velocity.y);
                }
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
