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

namespace Nebukam.ORCA
{

    public class ORCA : ProcessorChain, IPlanar
    {

        #region IPlanar

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_orcaPreparation.plane = m_orcaLines.plane = m_orcaApply.plane = m_raycasts.plane = value; }
        }

        #endregion

        /// 
        /// Fields
        /// 

        protected ORCAPreparation m_orcaPreparation;
        protected ORCALinesProcessor m_orcaLines;
        protected ORCAApplyProcessor m_orcaApply;
        protected RaycastsProcessor m_raycasts;

        /// 
        /// Properties
        /// 

        public IObstacleGroup staticObstacles { get { return m_orcaPreparation.staticObstacles; } set { m_orcaPreparation.staticObstacles = value; } }
        public IObstacleGroup dynamicObstacles { get { return m_orcaPreparation.dynamicObstacles; } set { m_orcaPreparation.dynamicObstacles = value; } }
        public IAgentGroup agents { get { return m_orcaPreparation.agents; } set { m_orcaPreparation.agents = value; } }
        public IRaycastGroup raycasts { get { return m_orcaPreparation.raycasts; } set { m_orcaPreparation.raycasts = value; } }

        public ORCA()
        {

            Add(ref m_orcaPreparation);
            Add(ref m_orcaLines);
            m_orcaLines.chunkSize = 5; //Linear programs are hefty >.<

            Add(ref m_orcaApply);
            m_orcaApply.chunkSize = 64;

            Add(ref m_raycasts);
            m_raycasts.chunkSize = 5;


        }

        protected override void Apply()
        {
            base.Apply();
            /*
            IAgentProvider agentProvider;
            if(!TryGetFirst(-1, out agentProvider, true))
            {
                throw new System.Exception("No IAgentProvider in chain !");
            }

            NativeArray<AgentData> data = agentProvider.outputAgents;

            Agent agent;
            List<Agent> agentList = agentProvider.lockedAgents;

            AgentDataResult result;
            NativeArray<AgentDataResult> results = m_orca.results;

            if(m_plane == AxisPair.XY)
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

    }

}
