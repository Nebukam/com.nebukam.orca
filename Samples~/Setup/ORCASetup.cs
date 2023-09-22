
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
    public class ORCASetup : MonoBehaviour
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
        public float maxSpeed = 1f;
        public float minSpeed = 1f;

        [Header("Obstacles")]
        public int obstacleCount = 100;
        public int dynObstacleCount = 20;
        public float maxObstacleRadius = 2f;
        public int minObstacleEdgeCount = 2;
        public int maxObstacleEdgeCount = 2;
        public float2 min, max; //obstacle spawn boundaries

        [Header("Debug")]
        Color staticObstacleColor = Color.red;
        Color dynObstacleColor = Color.yellow;

        [Header("Raycasts")]
        public int raycastCount = 50;
        public float raycastDistance = 10f;

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

                float3 start = float3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0f),
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

            #region create encompasing square boundary

            float3[] squarePoints = new float3[] {
                float3(min.x, min.y, 0f) * 1.2f,
                float3(min.x, max.y, 0f)* 1.2f,
                float3(max.x, max.y, 0f)* 1.2f,
                float3(max.x, min.y, 0f)* 1.2f,
            };

            if (axis == AxisPair.XZ)
            {
                for (int i = 0; i < squarePoints.Length; i++)
                    squarePoints[i] = float3(squarePoints[i].x, 0f, squarePoints[i].y);
            }

            obstacles.Add(squarePoints, false, 10.0f);

            #endregion

            Random.InitState(seed + 10);

            #region create dyanmic obstacles

            for (int i = 0; i < dynObstacleCount; i++)
            {
                int vCount = Random.Range(minObstacleEdgeCount, maxObstacleEdgeCount);
                vList.Clear();
                vList.Capacity = vCount;

                //build branch-like obstacle

                float3 start = float3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0f),
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

            float inc = Maths.TAU / (float)agentCount;
            IAgent a;

            for (int i = 0; i < agentCount; i++)
            {
                if (axis == AxisPair.XY)
                {
                    a = agents.Add((float3)transform.position + float3(Random.value, Random.value, 0f)) as IAgent;
                }
                else
                {
                    a = agents.Add((float3)transform.position + float3(Random.value, 0f, Random.value)) as IAgent;
                }

                a.radius = 0.5f + Random.value * maxAgentRadius;
                a.radiusObst = a.radius + Random.value * maxAgentRadius;
                a.prefVelocity = float3(0f);
            }

            #endregion

            #region create raycasts

            Raycast r;

            for (int i = 0; i < raycastCount; i++)
            {
                if (axis == AxisPair.XY)
                {
                    r = raycasts.Add(float3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0f)) as Raycast;
                    r.dir = normalize(float3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f));
                }
                else
                {
                    r = raycasts.Add(float3(Random.Range(min.x, max.x), 0f, Random.Range(min.y, max.y))) as Raycast;
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

            //Store "target" position
            float3 tr = target.position;

            #region update & draw agents

            //Draw agents debug
            IAgent agent;
            float3 agentPos;
            for (int i = 0, count = agents.Count; i < count; i++)
            {
                agent = agents[i] as IAgent;
                agentPos = agent.pos;

#if UNITY_EDITOR
                //Agent body
                if (axis == AxisPair.XY)
                {
                    Draw.Circle2D(agentPos, agent.radius, Color.green, 12);
                    Draw.Circle2D(agentPos, agent.radiusObst, Color.cyan.A(0.15f), 12);
                }
                else
                {
                    Draw.Circle(agentPos, agent.radius, Color.green, 12);
                    Draw.Circle(agentPos, agent.radiusObst, Color.cyan.A(0.15f), 12);

                }
                //Agent simulated velocity (ORCA compliant)
                Draw.Line(agentPos, agentPos + (normalize(agent.velocity) * agent.radius), Color.green);
                //Agent goal vector
                Draw.Line(agentPos, agentPos + (normalize(agent.prefVelocity) * agent.radius), Color.grey);
#endif
                //Update agent preferred velocity so it always tries to reach the "target" object
                float mspd = max(minSpeed + (i + 1) * 0.5f, maxSpeed);
                float s = min(1f, distance(agent.pos, tr) / mspd);
                float agentSpeed = mspd * s;
                agent.maxSpeed = agentSpeed * s;
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
                    Draw.Circle(o[j - 1].pos, 0.2f, Color.magenta, 6);
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