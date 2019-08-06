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

    public class AgentKDTreeBuilder : ProcessorChain, IPlanar
    {

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_agentSource.plane = value; }
        }

        protected AgentProvider m_agentSource;
        public IAgentGroup agents { get { return m_agentSource.agents; } set { m_agentSource.agents = value; } }

        protected AgentKDTreeProcessor m_agentKDTreeProvider;
                
        public AgentKDTreeBuilder()
        {

            m_agentSource = Add(new AgentProvider());
            m_agentKDTreeProvider = Add(new AgentKDTreeProcessor());

        }

        protected override void Apply()
        {
            base.Apply();
        }

    }

}
