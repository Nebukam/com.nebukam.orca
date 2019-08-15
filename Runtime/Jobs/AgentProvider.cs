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

    public interface IAgentProvider : IProcessor
    {
        NativeArray<AgentData> outputAgents { get; }
        List<Agent> lockedAgents { get; }
    }

    public class AgentProvider : Processor<Unemployed>, IAgentProvider, IPlanar
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        /// 
        /// Fields
        ///

        protected IAgentGroup m_agents = null;
        protected List<Agent> m_lockedAgents = new List<Agent>();
        protected NativeArray<AgentData> m_outputAgents = new NativeArray<AgentData>(0, Allocator.Persistent);

        /// 
        /// Properties
        ///

        public IAgentGroup agents { get { return m_agents; } set { m_agents = value; } }
        public List<Agent> lockedAgents { get { return m_lockedAgents; } }
        public NativeArray<AgentData> outputAgents { get { return m_outputAgents; } }

        protected override void InternalLock()
        {

            int count = m_agents.Count;

            m_lockedAgents.Clear();
            m_lockedAgents.Capacity = count;

            for (int i = 0; i < count; i++) { m_lockedAgents.Add(m_agents[i] as Agent); }

        }

        protected override void Prepare(ref Unemployed job, float delta)
        {
            
            int agentCount = m_lockedAgents.Count;

            if (m_outputAgents.Length != agentCount)
            {
                m_outputAgents.Dispose();
                m_outputAgents = new NativeArray<AgentData>(agentCount, Allocator.Persistent);
            }

            Agent a;
            float3 pos, prefVel, vel;

            if(plane == AxisPair.XY)
            {
                for (int i = 0; i < agentCount; i++)
                {
                    a = m_lockedAgents[i];
                    pos = a.pos;
                    prefVel = a.m_prefVelocity;
                    vel = a.m_velocity;
                    m_outputAgents[i] = new AgentData()
                    {
                        index = i,
                        kdIndex = i,
                        position = float2(pos.x, pos.y), //
                        baseline = pos.z,
                        prefVelocity = float2(prefVel.x, prefVel.y),
                        velocity = float2(vel.x, vel.y),
                        height = a.m_height,
                        radius = a.m_radius,
                        radiusObst = a.m_radiusObst,
                        maxSpeed = a.m_maxSpeed,
                        maxNeighbors = a.m_maxNeighbors,
                        neighborDist = a.m_neighborDist,
                        neighborElev = a.m_neighborElev,
                        timeHorizon = a.m_timeHorizon,
                        timeHorizonObst = a.m_timeHorizonObst,
                        navigationEnabled = a.m_navigationEnabled,
                        collisionEnabled = a.m_collisionEnabled,
                        layerOccupation = a.m_layerOccupation,
                        layerIgnore = a.m_layerIgnore
                    };
                }
            }
            else
            {
                for (int i = 0; i < agentCount; i++)
                {
                    a = m_lockedAgents[i];
                    pos = a.pos;
                    prefVel = a.m_prefVelocity;
                    vel = a.m_velocity;
                    m_outputAgents[i] = new AgentData()
                    {
                        index = i,
                        position = float2(pos.x, pos.z), //
                        baseline = pos.y,
                        prefVelocity = float2(prefVel.x, prefVel.z),
                        velocity = float2(vel.x, vel.z),
                        height = a.m_height,
                        radius = a.m_radius,
                        radiusObst = a.m_radiusObst,
                        maxSpeed = a.m_maxSpeed,
                        maxNeighbors = a.m_maxNeighbors,
                        neighborDist = a.m_neighborDist,
                        neighborElev = a.m_neighborElev,
                        timeHorizon = a.m_timeHorizon,
                        timeHorizonObst = a.m_timeHorizonObst,
                        navigationEnabled = a.m_navigationEnabled,
                        collisionEnabled = a.m_collisionEnabled,
                        layerOccupation = a.m_layerOccupation,
                        layerIgnore = a.m_layerIgnore
                    };
                }
            }
            
        }

        protected override void Apply(ref Unemployed job)
        {

        }

        protected override void InternalUnlock()
        {

        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { return; }

            m_agents = null;
            m_outputAgents.Dispose();
        }

    }
}
