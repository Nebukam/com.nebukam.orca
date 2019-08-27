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
    public class ORCAPreparation : ProcessorGroup, IPlanar
    {

        #region IPlanar

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_staticObstacles.plane = m_dynamicObstacles.plane = m_agents.plane = m_raycasts.plane = value; }
        }

        #endregion

        /// 
        /// Fields
        ///

        protected ObstacleKDTreeBuilder<IDynObstacleProvider, DynObstacleProvider, DynObstacleKDTreeProcessor> m_dynamicObstacles;
        protected ObstacleKDTreeBuilder<IStaticObstacleProvider, StaticObstacleProvider, StaticObstacleKDTreeProcessor> m_staticObstacles;
        protected AgentKDTreeBuilder m_agents;
        protected RaycastProvider m_raycasts;


        /// 
        /// Properties
        ///

        public IObstacleGroup dynamicObstacles
        {
            get { return m_dynamicObstacles.obstacles; }
            set { m_dynamicObstacles.obstacles = value; }
        }

        public IObstacleGroup staticObstacles
        {
            get { return m_staticObstacles.obstacles; }
            set { m_staticObstacles.obstacles = value; }
        }

        public IAgentGroup agents
        {
            get { return m_agents.agents; }
            set { m_agents.agents = value; }
        }

        public IRaycastGroup raycasts
        {
            get { return m_raycasts.raycasts; }
            set { m_raycasts.raycasts = value; }
        }

        public ORCAPreparation()
        {
            Add(ref m_dynamicObstacles);
            Add(ref m_staticObstacles);
            Add(ref m_agents);
            Add(ref m_raycasts);
        }

    }
}
