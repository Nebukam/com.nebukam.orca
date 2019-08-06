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

        #region IPlanar

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_agentProvider.plane = value; }
        }

        #endregion

        /// 
        /// Fields
        ///

        protected AgentProvider m_agentProvider;
        protected AgentKDTreeProcessor m_agentKDTreeProvider;


        /// 
        /// Properties
        ///

        public IAgentGroup agents { get { return m_agentProvider.agents; } set { m_agentProvider.agents = value; } }

        public AgentKDTreeBuilder()
        {
            Add(ref m_agentProvider);
            Add(ref m_agentKDTreeProvider);
        }

        protected override void Apply()
        {
            base.Apply();
        }

    }

}
