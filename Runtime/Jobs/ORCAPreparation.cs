using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{
    public class ORCAPreparation : ProcessorGroup, IPlanar
    {

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_obstacles.plane = m_agents.plane = value; }
        }

        protected ObstacleKDTreeBuilder m_obstacles;
        public IObstacleGroup obstacles { get { return m_obstacles.obstacles; } set { m_obstacles.obstacles = value; } }
        
        protected AgentKDTreeBuilder m_agents;
        public IAgentGroup agents { get { return m_agents.agents; } set { m_agents.agents = value; } }

        public ORCAPreparation()
        {
            m_obstacles = Add( new ObstacleKDTreeBuilder() );
            m_agents = Add( new AgentKDTreeBuilder() );
        }

    }
}
