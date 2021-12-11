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
using static Nebukam.JobAssist.Extensions;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;

namespace Nebukam.ORCA
{

    public interface IAgentProvider : IProcessor
    {
        NativeArray<AgentData> outputAgents { get; }
        List<Agent> lockedAgents { get; }
        float maxRadius { get; }
    }

    public class AgentProvider : Processor<Unemployed>, IAgentProvider, IPlanar
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        protected IAgentGroup<IAgent> m_agents = null;
        public IAgentGroup<IAgent> agents 
        { 
            get { return m_agents; } 
            set { m_agents = value; } 
        }

        internal List<Agent> m_lockedAgents = new List<Agent>();
        public List<Agent> lockedAgents { get { return m_lockedAgents; } }

        protected NativeArray<AgentData> m_outputAgents = default;
        public NativeArray<AgentData> outputAgents { get { return m_outputAgents; } }

        protected float m_maxRadius = 0f;
        public float maxRadius { get { return m_maxRadius; } }
                

        protected override void InternalLock()
        {

            int count = m_agents == null ? 0 : m_agents.Count;

            m_lockedAgents.Clear();
            m_lockedAgents.Capacity = count;

            for (int i = 0; i < count; i++) { m_lockedAgents.Add(m_agents[i] as Agent); }

        }

        protected override void Prepare(ref Unemployed job, float delta)
        {

            int agentCount = m_lockedAgents.Count;

            MakeLength(ref m_outputAgents, agentCount);

            m_maxRadius = 0f;

            Agent a;
            float3 pos, prefVel, vel;

            if (plane == AxisPair.XY)
            {
                for (int i = 0; i < agentCount; i++)
                {
                    a = m_lockedAgents[i];
                    m_maxRadius = max(m_maxRadius , a.radius);
                    pos = a.pos;
                    prefVel = a.m_prefVelocity;
                    vel = a.m_velocity;
                    m_outputAgents[i] = new AgentData()
                    {
                        index = i,
                        kdIndex = i,
                        position = float2(pos.x, pos.y), //
                        worldPosition = pos,
                        baseline = pos.z,
                        prefVelocity = float2(prefVel.x, prefVel.y),
                        velocity = float2(vel.x, vel.y),
                        worldVelocity = vel,
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
                    m_maxRadius = max(m_maxRadius, a.radius);
                    pos = a.pos;
                    prefVel = a.m_prefVelocity;
                    vel = a.m_velocity;
                    m_outputAgents[i] = new AgentData()
                    {
                        index = i,
                        position = float2(pos.x, pos.z), //
                        worldPosition = pos,
                        baseline = pos.y,
                        prefVelocity = float2(prefVel.x, prefVel.z),
                        velocity = float2(vel.x, vel.z),
                        worldVelocity = vel,
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

        protected override void InternalDispose()
        {
            m_agents = null;

            m_lockedAgents.Clear();
            m_lockedAgents = null;

            m_outputAgents.Release();
        }

    }
}
