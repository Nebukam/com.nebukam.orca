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

using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    public interface IAgent : IVertex
    {
        
        /// <summary>
        /// Preferred velocity of the agent.
        /// This is the 'ideal', desired velocity.
        /// Note : The agent velocity is multiplied by the simulation's timestep.
        /// </summary>
        float3 prefVelocity { get; set; }
        /// <summary>
        /// Simulated, collision-free velocity.
        /// </summary>
        float3 velocity { get; set; }

        float height { get; set; }
        /// <summary>
        /// Radius of the agent when resolving agent-agent collisions.
        /// </summary>
        float radius { get; set; }
        /// <summary>
        /// Radius of the agent when resolving agent-obstacle collisions.
        /// </summary>
        float radiusObst { get; set; }
        /// <summary>
        /// Maximum allowed speed of the agent.
        /// This is used to avoid deadlock situation where a slight
        /// boost in velocity could help solve more complex scenarios.
        /// </summary>
        float maxSpeed { get; set; }

        /// <summary>
        /// Maxmimum number of neighbors this agent accounts for in the simulation
        /// </summary>
        int maxNeighbors { get; set; }
        /// <summary>
        /// Maximum distance at which this agent consider avoiding other agents
        /// </summary>
        float neighborDist { get; set; }
        float neighborElevation { get; set; }

        float timeHorizon { get; set; }
        float timeHorizonObst { get; set; }

        /// <summary>
        /// Layers on which this agent is physically present, and thus will affect
        /// other agents navigation.
        /// </summary>
        ORCALayer layerOccupation { get; set; }
        /// <summary>
        /// Ignored layers while resolving the simulation.
        /// </summary>
        ORCALayer layerIgnore { get; set; }
        /// <summary>
        /// Whether this agent's navigation is controlled by the simulation.
        /// This property has precedence over layers.
        /// </summary>
        bool navigationEnabled { get; set; }
        /// <summary>
        /// Whether this agent's collision is enabled.
        /// This property has precedence over layers.
        /// </summary>
        bool collisionEnabled { get; set; }

    }
    
    public class Agent : Vertex, IAgent
    {

        /// 
        /// Fields
        /// 

        protected internal float3 m_prefVelocity = float3(0f);
        protected internal float3 m_velocity { get; set; } = float3(0f);

        protected internal float m_height = 0.5f;
        protected internal float m_radius = 0.5f;
        protected internal float m_radiusObst = 0.5f;
        protected internal float m_maxSpeed = 20.0f;

        protected internal int m_maxNeighbors = 15;
        protected internal float m_neighborDist = 20.0f;
        protected internal float m_neighborElev = 0.5f;

        protected internal float m_timeHorizon = 15.0f;
        protected internal float m_timeHorizonObst = 1.2f;

        protected internal ORCALayer m_layerOccupation = ORCALayer.ANY;
        protected internal ORCALayer m_layerIgnore = ORCALayer.NONE;
        protected internal bool m_navigationEnabled = true;
        protected internal bool m_collisionEnabled = true;

        /// 
        /// Properties
        /// 

        public float3 prefVelocity {
            get { return m_prefVelocity; }
            set { m_prefVelocity = value; }
        }
        public float3 velocity {
            get { return m_velocity; }
            set { m_velocity = value; }
        }

        public float height
        {
            get { return m_height; }
            set { m_height = value; }
        }
        public float radius {
            get { return m_radius; }
            set { m_radius = value; }
        }
        public float radiusObst
        {
            get { return m_radiusObst; }
            set { m_radiusObst = value; }
        }
        public float maxSpeed {
            get { return m_maxSpeed; }
            set { m_maxSpeed = value; }
        }

        public int maxNeighbors {
            get { return m_maxNeighbors; }
            set { m_maxNeighbors = value; }
        }
        public float neighborDist {
            get { return m_neighborDist; }
            set { m_neighborDist = value; }
        }
        public float neighborElevation
        {
            get { return m_neighborElev; }
            set { m_neighborElev = value; }
        }

        public float timeHorizon {
            get { return m_timeHorizon; }
            set { m_timeHorizon = value; }
        }
        public float timeHorizonObst {
            get { return m_timeHorizonObst; }
            set { m_timeHorizonObst = value; }
        }

        public ORCALayer layerOccupation {
            get { return m_layerOccupation; }
            set { m_layerOccupation = value; }
        }
        public ORCALayer layerIgnore {
            get { return m_layerIgnore; }
            set { m_layerIgnore = value; }
        }
        public bool navigationEnabled {
            get { return m_navigationEnabled; }
            set { m_navigationEnabled = value; }
        }
        public bool collisionEnabled {
            get { return m_collisionEnabled; }
            set { m_collisionEnabled = value; }
        }
    }

}
