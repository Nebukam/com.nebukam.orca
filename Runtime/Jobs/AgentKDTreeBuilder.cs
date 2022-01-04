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
using Nebukam.Common;
using Unity.Burst;

namespace Nebukam.ORCA
{

    [BurstCompile]
    public class AgentKDTreeBuilder : ProcessorChain, IPlanar
    {

        #region IPlanar

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_agentProvider.plane = value; }
        }

        #endregion

        protected AgentProvider m_agentProvider;
        protected AgentKDTree m_agentKDTreeProvider;

        public IAgentGroup<IAgent> agents { 
            get { return m_agentProvider.agents; } 
            set { m_agentProvider.agents = value; } 
        }

        public AgentKDTreeBuilder()
        {
            Add(ref m_agentProvider);
            Add(ref m_agentKDTreeProvider);
        }

    }

}
