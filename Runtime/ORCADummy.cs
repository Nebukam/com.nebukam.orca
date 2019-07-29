using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    /// <summary>
    /// Defines an agent in the simulation.
    /// </summary>
    public class ORCADummy : ORCAAgent
    {
        
        public override float2 prefVelocity
        {
            get { return m_prefVelocity; }
            set { m_prefVelocity = float2(false); }
        }
        public override float2 velocity
        {
            get { return m_velocity; }
            set { m_velocity = float2(false); }
        }

        public override int maxNeighbors
        {
            get { return m_maxNeighbors; }
            set { m_maxNeighbors = 0; }
        }
        public override float maxSpeed
        {
            get { return m_maxSpeed; }
            set { m_maxSpeed = 0f; }
        }
        public override float neighborDist
        {
            get { return m_neighborDist; }
            set { m_neighborDist = 0f; }
        }
        public override float timeHorizon
        {
            get { return m_timeHorizon; }
            set { m_timeHorizon = 0f; }
        }
        public override float timeHorizonObst
        {
            get { return m_timeHorizonObst; }
            set { m_timeHorizonObst = 0f; }
        }
        
        /// <summary>
        /// Computes the neighbors of this agent.
        /// </summary>
        internal override void ComputeNeighbors()
        {
            m_obstacleNeighbors.Clear();
            //Dummy
        }

        /// <summary>
        /// Computes the new velocity of this agent.
        /// </summary>
        internal override void ComputeNewVelocity()
        {
            m_orcaLines.Clear();
            //Dummy
        }

        /// <summary>
        /// Inserts an agent neighbor into the set of neighbors of this agent.
        /// </summary>
        /// <param name="agent">A pointer to the agent to be inserted.</param>
        /// <param name="rangeSq">The squared range around this agent.</param>
        internal override void InsertAgentNeighbor(ORCAAgent agent, ref float rangeSq)
        {
            //Dummy
        }

        /// <summary>
        /// Inserts a static obstacle neighbor into the set of neighbors of this agent.
        /// </summary>
        /// <param name="obstacle">The number of the static obstacle to be inserted.</param>
        /// <param name="rangeSq">The squared range around this agent.</param>
        internal override void InsertObstacleNeighbor(Obstacle obstacle, float rangeSq)
        {
            //Dummy
        }

        /// <summary>
        /// Updates the two-dimensional position and two-dimensional velocity of this agent.
        /// </summary>
        internal override void Commit()
        {
            m_velocity = float2(false);
        }
        
    }
}
