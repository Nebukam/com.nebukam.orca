
using Nebukam.Common;
#if UNITY_EDITOR
using Nebukam.Common.Editor;
#endif
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using Random = UnityEngine.Random;

namespace Nebukam.ORCA
{

    [System.Serializable]
    public struct AgentDef
    {
        public float3 position;
        public float3 destination;
        public float radius;
        public float radiusObst;
        public float speed;
        public float timeHorizon;
        public Color color;
    }

    [System.Serializable]
    public struct ObstacleDef
    {
        public bool edge;
        public bool inverse;
        public float thickness;
        public List<float3> vertices;
        public Color color;
    }

    [System.Serializable]
    public struct RaycastDef
    {
        public float3 position;
        public float3 direction;
        public float distance;
        public Color color;
        public Color colorHit;
    }

    public class ORCASimpleDemo : MonoBehaviour
    {

        private ORCABundle<Agent> m_simulation;

        public AxisPair axis = AxisPair.XY;

        [Header("Agents")]
        public List<AgentDef> Agents;

        [Header("Collisions")]
        public List<ObstacleDef> Obstacles;

        [Header("Raycasts")]
        public List<RaycastDef> Raycasts;

        protected Dictionary<Agent, float3> m_targets = new Dictionary<Agent, float3>();

        private void Awake()
        {
            m_simulation = new ORCABundle<Agent>();
            m_simulation.plane = axis;
        }

        private void Start()
        {

            #region Create agents
            if (Agents != null)
            {
                for (int i = 0; i < Agents.Count; i++)
                {
                    AgentDef def = Agents[i];
                    Agent a = m_simulation.NewAgent(def.position);
                    a.radius = def.radius;
                    a.radiusObst = def.radius + def.radiusObst;
                    a.maxSpeed = def.speed;
                    a.prefVelocity = normalize(def.destination - def.position) * def.speed;
                    a.velocity = a.prefVelocity;
                    a.timeHorizon = def.timeHorizon;
                }
            }
            #endregion

            #region Create obstacles
            if (Obstacles != null)
            {
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    ObstacleDef def = Obstacles[i];
                    Obstacle o = m_simulation.staticObstacles.Add(def.vertices, def.inverse);
                    o.edge = def.edge;
                    o.thickness = def.thickness;
                }
            }
            #endregion

            #region Create raycasts
            if (Raycasts != null)
            {
                for (int i = 0; i < Raycasts.Count; i++)
                {
                    RaycastDef def = Raycasts[i];
                    Raycast r = m_simulation.raycasts.Add(def.position, def.direction, def.distance);
                }
            }
            #endregion

        }

        private void Update()
        {

            m_simulation.orca.TryComplete();
            m_simulation.orca.Schedule(Time.deltaTime);

            #region Draw agents
            if (Agents != null)
            {
                for (int i = 0; i < Agents.Count; i++)
                {

                    Agent a = m_simulation.agents[i];
                    AgentDef d = Agents[i];
                    DrawDebug(a, d);
                    float speed = d.speed * min(1f, (distance(a.pos, d.destination) / d.speed ));
                    a.prefVelocity = normalize(d.destination - a.pos) * speed;

                }
            }
            #endregion

            #region Draw obstacles
            if (Obstacles != null)
            {
                for (int i = 0; i < Obstacles.Count; i++)
                    DrawDebug(Obstacles[i]);
            }
            #endregion

            #region Draw raycasts
            if (Raycasts != null)
            {
                for (int i = 0; i < Raycasts.Count; i++)
                    DrawDebug(m_simulation.raycasts[i], Raycasts[i]);
            }
            #endregion

        }

        private void DrawDebug(IAgent agent, AgentDef def)
        {
#if UNITY_EDITOR
            //Agent body
            Color bodyColor = def.color;
            float3 offset = transform.position;
            float3 agentPos = agent.pos + offset;
            float3 agentDest = def.destination + offset;

            if (axis == AxisPair.XY)
            {
                Draw.Circle2D(agentPos, agent.radius, bodyColor, 12);
                Draw.Circle2D(agentPos, agent.radiusObst, Color.cyan.A(0.15f), 12);
            }
            else
            {
                Draw.Circle(agentPos, agent.radius, bodyColor, 12);
                Draw.Circle(agentPos, agent.radiusObst, Color.cyan.A(0.15f), 12);

            }

            //Agent goal vector
            Draw.Line(agentPos, agentPos + (normalize(agent.prefVelocity) * agent.radius), Color.grey);
            //Agent simulated velocity (ORCA compliant)
            Draw.Line(agentPos, agentPos + (normalize(agent.velocity) * agent.radius), bodyColor);
#endif
        }

        private void DrawDebug(ObstacleDef def)
        {
#if UNITY_EDITOR
            if (def.vertices == null || def.vertices.Count <= 1) { return; }
            int vCount = def.vertices.Count;
            float3 offset = transform.position;

            //Draw each segment
            for (int j = 1, count = vCount; j < count; j++)
            {
                Draw.Line(def.vertices[j - 1] + offset, def.vertices[j] + offset, def.color);
            }
            if (!def.edge)
                Draw.Line(def.vertices[vCount - 1] + offset, def.vertices[0] + offset, def.color);
#endif
        }

        private void DrawDebug(Raycast raycast, RaycastDef def)
        {
#if UNITY_EDITOR
            float3 offset = transform.position;
            float rad = 0.2f;
            float3 origin = raycast.pos + offset;
            Draw.Circle2D(origin, rad, Color.white, 3);

            if (raycast.anyHit)
            {

                Draw.Line(origin, origin + raycast.dir * raycast.distance, Color.white.A(0.5f));

                if (axis == AxisPair.XY)
                {
                    if (raycast.obstacleHit != null) { Draw.Circle2D(raycast.obstacleHitLocation, rad, def.colorHit, 3); }
                    if (raycast.agentHit != null) { Draw.Circle2D(raycast.agentHitLocation, rad, def.colorHit, 3); }
                }
                else
                {
                    if (raycast.obstacleHit != null) { Draw.Circle(raycast.obstacleHitLocation, rad, def.colorHit, 3); }
                    if (raycast.agentHit != null) { Draw.Circle(raycast.agentHitLocation, rad, def.colorHit, 3); }
                }

            }
            else
            {
                Draw.Line(origin, origin + raycast.dir * raycast.distance, def.color.A(0.5f));
            }
#endif
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying) { return; }

            float3 offset = transform.position;

            #region Draw agents
            if (Agents != null)
            {
                for (int i = 0; i < Agents.Count; i++)
                {
                    AgentDef def = Agents[i];

                    Color bodyColor = def.color;
                    float3 agentPos = def.position + offset;
                    float3 agentDest = def.destination + offset;
                    float3 dir = normalize(agentDest - agentPos);

                    if (axis == AxisPair.XY)
                    {
                        Draw.Circle2D(agentPos, def.radius, bodyColor, 12);
                        Draw.Circle2D(agentPos, def.radius + def.radiusObst, Color.cyan.A(0.15f), 12);
                    }
                    else
                    {
                        Draw.Circle(agentPos, def.radius, bodyColor, 12);
                        Draw.Circle(agentPos, def.radius + def.radiusObst, Color.cyan.A(0.15f), 12);
                    }

                    //Agent goal vector
                    Draw.Line(agentPos, agentPos + (dir * def.radius), bodyColor);
                    Draw.Line(agentPos, agentDest, Colors.A(bodyColor, 0.1f));
                }
            }
            #endregion

            #region Draw obstacles
            if (Obstacles != null)
            {
                for (int i = 0; i < Obstacles.Count; i++)
                {
                    ObstacleDef def = Obstacles[i];

                    if (def.vertices == null || def.vertices.Count <= 1) { continue; }

                    int vCount = def.vertices.Count;

                    //Draw each segment
                    for (int j = 1, count = vCount; j < count; j++)
                    {
                        Draw.Line(def.vertices[j - 1] + offset, def.vertices[j] + offset, def.color);
                    }

                    if (!def.edge)
                        Draw.Line(def.vertices[vCount - 1] + offset, def.vertices[0] + offset, def.color);

                }
            }
            #endregion

            #region Draw raycasts
            if (Raycasts != null)
            {
                for (int i = 0; i < Raycasts.Count; i++)
                {
                    RaycastDef def = Raycasts[i];
                    float3 origin = offset + def.position;
                    Draw.Line(origin, origin + normalize(def.direction) * def.distance, def.color.A(0.5f));
                }
            }
            #endregion


        }

        private void OnApplicationQuit()
        {
            //Make sure to clean-up the jobs
            m_simulation.Dispose();
        }

    }
}