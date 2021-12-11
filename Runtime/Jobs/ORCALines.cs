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

using Nebukam.JobAssist;
using static Nebukam.JobAssist.Extensions;
using Unity.Collections;
using Nebukam.Common;

namespace Nebukam.ORCA
{

    public interface IORCALinesProvider : IProcessor
    {
        IAgentProvider agentProvider { get; }
        NativeArray<AgentDataResult> results { get; }
    }

    public class ORCALines : ParallelProcessor<ORCALinesJob>, IORCALinesProvider
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        protected NativeArray<AgentDataResult> m_results = default;
        public NativeArray<AgentDataResult> results { get { return m_results; } }
        

        #region Inputs

        protected bool m_inputsDirty = true;

        protected IAgentProvider m_agentProvider;
        public IAgentProvider agentProvider { get { return m_agentProvider; } }

        protected IAgentKDTreeProvider m_agentKDTreeProvider;
        public IAgentKDTreeProvider agentKDTreeProvider { get { return m_agentKDTreeProvider; } }

        protected IStaticObstacleProvider m_staticObstaclesProvider;
        public IStaticObstacleProvider staticObstaclesProvider { get { return m_staticObstaclesProvider; } }

        protected IStaticObstacleKDTreeProvider m_staticObstacleKDTreeProvider;
        public IStaticObstacleKDTreeProvider staticObstacleKDTreeProvider { get { return m_staticObstacleKDTreeProvider; } }

        protected IDynObstacleProvider m_dynObstaclesProvider;
        public IDynObstacleProvider dynObstaclesProvider { get { return m_dynObstaclesProvider; } }

        protected IDynObstacleKDTreeProvider m_dynObstacleKDTreeProvider;
        public IDynObstacleKDTreeProvider dynObstacleKDTreeProvider { get { return m_dynObstacleKDTreeProvider; } }

        #endregion

        protected override int Prepare(ref ORCALinesJob job, float delta)
        {

            if (m_inputsDirty)
            {

                if (!TryGetFirstInCompound(out m_agentProvider, true)
                    || !TryGetFirstInCompound(out m_agentKDTreeProvider, true)
                    || !TryGetFirstInCompound(out m_staticObstaclesProvider, true)
                    || !TryGetFirstInCompound(out m_staticObstacleKDTreeProvider, true)
                    || !TryGetFirstInCompound(out m_dynObstaclesProvider, true)
                    || !TryGetFirstInCompound(out m_dynObstacleKDTreeProvider, true))
                {
                    string msg = string.Format("Missing provider : Agents = {0}, Static obs = {1}, Agent KD = {2}, Static obs KD= {3}, " +
                        "Dyn obs = {5}, Dyn obs KD= {6}, group = {4}",
                        m_agentProvider,
                        m_staticObstaclesProvider,
                        m_agentKDTreeProvider,
                        m_staticObstacleKDTreeProvider,
                        m_dynObstaclesProvider,
                        m_dynObstacleKDTreeProvider,
                        m_compound);

                    throw new System.Exception(msg);
                }

                m_inputsDirty = false;

            }

            int agentCount = m_agentProvider.outputAgents.Length;

            MakeLength(ref m_results, agentCount);

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
            job.m_timestep = delta;

            return agentCount;

        }

        protected override void Apply(ref ORCALinesJob job)
        {
            /*
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
            */
        }

        protected override void InternalDispose()
        {
            m_results.Release();
        }

    }
}
