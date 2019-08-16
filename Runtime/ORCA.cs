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
            set { m_plane = m_preparation.plane = m_orca.plane = value; }
        }

        #endregion

        /// 
        /// Fields
        /// 

        protected ORCAPreparation m_preparation;
        protected ORCAProcessor m_orca;

        /// 
        /// Properties
        /// 

        public IObstacleGroup staticObstacles { get { return m_preparation.staticObstacles; } set { m_preparation.staticObstacles = value; } }
        public IObstacleGroup dynamicObstacles { get { return m_preparation.dynamicObstacles; } set { m_preparation.dynamicObstacles = value; } }
        public IAgentGroup agents { get { return m_preparation.agents; } set { m_preparation.agents = value; } }

        public ORCA()
        {

            Add(ref m_preparation);
            Add(ref m_orca);
            m_orca.chunkSize = 5; //Linear programs are hefty >.<

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
