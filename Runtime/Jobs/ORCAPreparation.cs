using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{
    public class ORCAPreparation : ProcessorGroup, IPlanar
    {

        #region IPlanar

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_staticObstacles.plane = m_dynamicObstacles.plane = m_agents.plane = value; }
        }

        #endregion

        /// 
        /// Fields
        ///

        protected ObstacleKDTreeBuilder<IDynObstacleProvider, DynObstacleProvider, DynObstacleKDTreeProcessor> m_dynamicObstacles;
        protected ObstacleKDTreeBuilder<IStaticObstacleProvider, StaticObstacleProvider, StaticObstacleKDTreeProcessor> m_staticObstacles;
        protected AgentKDTreeBuilder m_agents;


        /// 
        /// Properties
        ///

        public IObstacleGroup dynamicObstacles
        {
            get { return m_dynamicObstacles.obstacles; }
            set { m_dynamicObstacles.obstacles = value; }
        }

        public IObstacleGroup staticObstacles {
            get { return m_staticObstacles.obstacles; }
            set { m_staticObstacles.obstacles = value; }
        }
        
        public IAgentGroup agents {
            get { return m_agents.agents; }
            set { m_agents.agents = value; }
        }

        public ORCAPreparation()
        {
            Add( ref m_dynamicObstacles );
            Add( ref m_staticObstacles );
            Add( ref m_agents );
        }

    }
}
