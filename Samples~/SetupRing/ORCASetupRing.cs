
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
    public class ORCASetupRing : MonoBehaviour
    {

        private AgentGroup<Agent> agents;
        private ObstacleGroup obstacles;
        private ObstacleGroup dynObstacles;
        private RaycastGroup raycasts;
        private ORCA simulation;

        [Header("Settings")]
        public int seed = 12345;
        public Transform target;
        public AxisPair axis = AxisPair.XY;

        [Header("Agents")]
        public int agentCount = 50;
        public float maxAgentRadius = 2f;
        public bool uniqueRadius = false;
        public float maxSpeed = 1f;
        public float minSpeed = 1f;

        [Header("Obstacles")]
        public int obstacleCount = 100;
        public int dynObstacleCount = 20;
        public float maxObstacleRadius = 2f;
        public int minObstacleEdgeCount = 2;
        public int maxObstacleEdgeCount = 2;

        [Header("Debug")]
        Color staticObstacleColor = Color.red;
        Color dynObstacleColor = Color.yellow;

        [Header("Raycasts")]
        public int raycastCount = 50;
        public float raycastDistance = 10f;

        protected Dictionary<Agent, float3> m_targets = new Dictionary<Agent, float3>();

        private void Awake()
        {

            agents = new AgentGroup<Agent>();

            obstacles = new ObstacleGroup();
            dynObstacles = new ObstacleGroup();
            raycasts = new RaycastGroup();

            simulation = new ORCA();
            simulation.plane = axis;
            simulation.agents = agents;
            simulation.staticObstacles = obstacles;
            simulation.dynamicObstacles = dynObstacles;
            simulation.raycasts = raycasts;

        }

        private void Start()
        {

            float radius = ((agentCount * (maxAgentRadius * 2f)) / PI) * 0.5f;

            Random.InitState(seed);

            #region create obstacles

            float dirRange = 2f;
            List<float3> vList = new List<float3>();
            Obstacle o;
            for (int i = 0; i < obstacleCount; i++)
            {
                int vCount = Random.Range(minObstacleEdgeCount, maxObstacleEdgeCount);
                vList.Clear();
                vList.Capacity = vCount;

                //build branch-like obstacle

                float3 start = float3(Random.Range(-radius, radius), Random.Range(-radius, radius), 0f),
                    pt = start,
                    dir = float3(Random.Range(-dirRange, dirRange), Random.Range(-dirRange, dirRange), 0f);

                if (axis == AxisPair.XZ)
                {
                    pt = start = float3(start.x, 0f, start.y);
                    dir = float3(dir.x, 0f, dir.y);
                }

                vList.Add(start);
                vCount--;

                for (int j = 0; j < vCount; j++)
                {
                    dir = normalize(Maths.RotateAroundPivot(dir, float3(0f),
                        axis == AxisPair.XY ? float3(0f, 0f, (math.PI) / vCount) : float3(0f, (math.PI) / vCount, 0f)));

                    pt = pt + dir * Random.Range(1f, maxObstacleRadius);
                    vList.Add(pt);
                }

                //if (vCount != 2) { vList.Add(start); }

                o = obstacles.Add(vList, axis == AxisPair.XZ);
            }

            #endregion

            Random.InitState(seed + 10);

            #region create dyanmic obstacles

            for (int i = 0; i < dynObstacleCount; i++)
            {
                int vCount = Random.Range(minObstacleEdgeCount, maxObstacleEdgeCount);
                vList.Clear();
                vList.Capacity = vCount;

                //build branch-like obstacle

                float3 start = float3(Random.Range(-radius, radius), Random.Range(-radius, radius), 0f),
                    pt = start,
                    dir = float3(Random.Range(-dirRange, dirRange), Random.Range(-dirRange, dirRange), 0f);

                if (axis == AxisPair.XZ)
                {
                    pt = start = float3(start.x, 0f, start.y);
                    dir = float3(dir.x, 0f, dir.y);
                }

                vList.Add(start);
                vCount--;

                for (int j = 0; j < vCount; j++)
                {
                    dir = normalize(Maths.RotateAroundPivot(dir, float3(0f),
                        axis == AxisPair.XY ? float3(0f, 0f, (math.PI) / vCount) : float3(0f, (math.PI) / vCount, 0f)));
                    pt = pt + dir * Random.Range(1f, maxObstacleRadius);
                    vList.Add(pt);
                }

                //if (vCount != 2) { vList.Add(start); }

                dynObstacles.Add(vList, axis == AxisPair.XZ);
            }

            #endregion

            #region create agents

            IAgent a;

            
            float angleInc = (PI * 2) / agentCount;

            for (int i = 0; i < agentCount; i++)
            {
                float2 pos = float2(sin(angleInc * i), cos(angleInc * i)) * (radius * 1.5f);

                if (axis == AxisPair.XY)
                {
                    a = agents.Add((float3)transform.position + float3(pos.x, pos.y, 0f)) as IAgent;
                }
                else
                {
                    a = agents.Add((float3)transform.position + float3(pos.x, 0f, pos.y)) as IAgent;
                }

                a.radius = uniqueRadius ? maxAgentRadius : 0.5f + Random.value * maxAgentRadius;
                a.radiusObst = a.radius + Random.value * maxAgentRadius;
                a.prefVelocity = float3(0f);
                m_targets[a as Agent] = a.pos * -1;
            }

            #endregion

            #region create raycasts

            Raycast r;

            for (int i = 0; i < raycastCount; i++)
            {
                if (axis == AxisPair.XY)
                {
                    r = raycasts.Add(float3(Random.Range(-radius, radius), Random.Range(-radius, radius), 0f)) as Raycast;
                    r.dir = normalize(float3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f));
                }
                else
                {
                    r = raycasts.Add(float3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius))) as Raycast;
                    r.dir = normalize(float3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)));
                }

                r.distance = raycastDistance;
            }

            #endregion

        }

        private void Update()
        {

            //Schedule the simulation job. 
            simulation.Schedule(Time.deltaTime);

            #region update & draw agents

            //Draw agents debug
            IAgent agent;
            float3 agentPos;
            bool aggro = false;
            for (int i = 0, count = agents.Count; i < count; i++)
            {
                agent = agents[i] as IAgent;
                agentPos = agent.pos;
                aggro = i % 3 == 0;

#if UNITY_EDITOR
                //Agent body
                Color bodyColor = aggro ? Color.red : Color.green;

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
                //Agent simulated velocity (ORCA compliant)
                Draw.Line(agentPos, agentPos + (normalize(agent.velocity) * agent.radius), Color.green);
                //Agent goal vector
                Draw.Line(agentPos, agentPos + (normalize(agent.prefVelocity) * agent.radius), Color.grey);
#endif
                //Update agent preferred velocity so it always tries to reach its "target" location
                //Slow it down as it reaches its target
                float3 tr = m_targets[agent as Agent];
                float mspd = maxSpeed; // max(minSpeed + (i + 1) * 0.5f, maxSpeed);
                float s = min(1f, distance(agent.pos, tr) / mspd);
                float agentSpeed = mspd * s;
                agent.maxSpeed = agentSpeed * s;

                if (aggro)
                {
                    agent.timeHorizon = 0.0001f;
                }
                else
                {
                    //agent.timeHorizon = 100.0f;
                }

                agent.prefVelocity = normalize(tr - agent.pos) * agentSpeed;

            }

            #endregion

#if UNITY_EDITOR

            #region draw obstacles

            //Draw static obstacles
            Obstacle o;
            int oCount = obstacles.Count, subCount;
            for (int i = 0; i < oCount; i++)
            {
                o = obstacles[i];
                subCount = o.Count;

                //Draw each segment
                for (int j = 1, count = o.Count; j < count; j++)
                {
                    Draw.Line(o[j - 1].pos, o[j].pos, staticObstacleColor);
                }
                //Draw closing segment (simulation consider 2+ segments to be closed.)
                if (!o.edge)
                    Draw.Line(o[subCount - 1].pos, o[0].pos, staticObstacleColor);
            }

            float delta = Time.deltaTime * 50f;

            //Draw dynamic obstacles
            oCount = dynObstacles.Count;
            for (int i = 0; i < oCount; i++)
            {
                o = dynObstacles[i];
                subCount = o.Count;

                //Draw each segment
                for (int j = 1, count = o.Count; j < count; j++)
                {
                    Draw.Line(o[j - 1].pos, o[j].pos, dynObstacleColor);
                }
                //Draw closing segment (simulation consider 2+ segments to be closed.)
                if (!o.edge)
                    Draw.Line(o[subCount - 1].pos, o[0].pos, dynObstacleColor);

            }

            #endregion

            #region update & draw raycasts

            Raycast r;
            float rad = 0.2f;
            for (int i = 0, count = raycasts.Count; i < count; i++)
            {
                r = raycasts[i] as Raycast;
                Draw.Circle2D(r.pos, rad, Color.white, 3);
                if (r.anyHit)
                {
                    Draw.Line(r.pos, r.pos + r.dir * r.distance, Color.white.A(0.5f));

                    if (axis == AxisPair.XY)
                    {
                        if (r.obstacleHit != null) { Draw.Circle2D(r.obstacleHitLocation, rad, Color.cyan, 3); }
                        if (r.agentHit != null) { Draw.Circle2D(r.agentHitLocation, rad, Color.magenta, 3); }
                    }
                    else
                    {
                        if (r.obstacleHit != null) { Draw.Circle(r.obstacleHitLocation, rad, Color.cyan, 3); }
                        if (r.agentHit != null) { Draw.Circle(r.agentHitLocation, rad, Color.magenta, 3); }
                    }

                }
                else
                {
                    Draw.Line(r.pos, r.pos + r.dir * r.distance, Color.blue.A(0.5f));
                }
            }

            #endregion

#endif

        }

        private void LateUpdate()
        {
            //Attempt to complete and apply the simulation results, only if the job is done.
            //TryComplete will not force job completion.
            if (simulation.TryComplete())
            {

                //Move dynamic obstacles randomly
                int oCount = dynObstacles.Count;
                float delta = Time.deltaTime * 50f;

                for (int i = 0; i < oCount; i++)
                    dynObstacles[i].Offset(float3(Random.Range(-delta, delta), Random.Range(-delta, delta), 0f));

            }
        }

        private void OnApplicationQuit()
        {
            //Make sure to clean-up the jobs
            simulation.DisposeAll();
        }

    }
}