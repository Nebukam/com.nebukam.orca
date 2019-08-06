using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = UnityEngine.Random;
using Nebukam.Common;
using Nebukam.Utils;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nebukam.ORCA
{
    public class ORCASetup : MonoBehaviour
    {

        private AgentGroup<Agent> agents;
        private ObstacleGroup obstacles;
        private ObstacleGroup dynObstacles;
        private ORCA simulation;

        [Header("Settings")]
        public int seed = 12345;
        public Transform target;

        [Header("Agents")]
        public int agentCount = 50;
        public float maxAgentRadius = 2f;

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

        private void Awake()
        {

            agents = new AgentGroup<Agent>();

            obstacles = new ObstacleGroup();
            dynObstacles = new ObstacleGroup();

            simulation = new ORCA();
            simulation.agents = agents;
            simulation.staticObstacles = obstacles;
            simulation.dynamicObstacles = dynObstacles;

        }

        private void Start()
        {

            Random.InitState(seed);

            #region create obstacles

            float dirRange = 2f;
            List<float3> vList = new List<float3>();
            for (int i = 0; i < obstacleCount; i++)
            {
                int vCount = Random.Range(minObstacleEdgeCount, maxObstacleEdgeCount);
                vList.Clear();
                vList.Capacity = vCount;

                //build branch-like obstacle

                float3 start = float3(Random.Range(min.x, max.x), Random.Range(min.y, max.y), 0f), 
                    pt = start, 
                    dir = float3(Random.Range(-dirRange, dirRange), Random.Range(-dirRange, dirRange), 0f);
                vList.Add(start);
                vCount--;

                for (int j = 0; j < vCount; j++)
                {
                    dir = normalize(Maths.RotateAroundPivot(dir, float3(false), float3(0f, 0f, (math.PI) / vCount)));
                    pt = pt + dir * Random.Range(1f, maxObstacleRadius);
                    vList.Add(pt);
                }

                //if (vCount != 2) { vList.Add(start); }

                obstacles.Add(vList);
            }

            #endregion

            Random.InitState(seed+10);

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
                vList.Add(start);
                vCount--;

                for (int j = 0; j < vCount; j++)
                {
                    dir = normalize(Maths.RotateAroundPivot(dir, float3(false), float3(0f, 0f, (math.PI) / vCount)));
                    pt = pt + dir * Random.Range(1f, maxObstacleRadius);
                    vList.Add(pt);
                }

                //if (vCount != 2) { vList.Add(start); }

                dynObstacles.Add(vList);
            }

            #endregion


            #region create agents

            float inc = Maths.TAU / (float)agentCount;
            IAgent a;

            for(int i = 0; i < agentCount; i++)
            {
                a = agents.Add((float3)transform.position + float3(Random.value, Random.value, Random.value)) as IAgent;
                a.radius = 0.5f + Random.value * maxAgentRadius;
                a.prefVelocity = float2(false);
            }

            #endregion

        }

        private void Update()
        {

            //Schedule the simulation job. 
            simulation.Schedule(Time.deltaTime);
            
            //Store "target" position
            float2 tr = float2(target.position.x, target.position.y);

            //Draw agents debug
            IAgent agent;
            for(int i = 0, count = agents.Count; i < count; i++)
            {
                agent = agents[i] as IAgent;
                
                //Agent body
                Draw.Circle2D(agent.pos, agent.radius, Color.green, 12);
                //Agent simulated velocity (ORCA compliant)
                Draw.Line(agent.pos, agent.pos + (normalize(float3(agent.velocity, 0f)) * agent.radius), Color.green);
                //Agent goal vector
                Draw.Line(agent.pos, agent.pos + (normalize(float3(agent.prefVelocity, 0f)) * agent.radius), Color.grey);

                //Update agent preferred velocity so it always tries to reach the "target" object
                agent.prefVelocity = normalize(tr - agent.XY) * 10f;

            }

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
                Draw.Line(o[subCount - 1].pos, o[0].pos, dynObstacleColor);

            }

            #endregion

        }

        private void LateUpdate()
        {
            //Attempt to complete and apply the simulation results, only if the job is done.
            //TryComplete will not force job completion.
            if (simulation.TryComplete())
            {

                //Move dynamic obstacles randomly
                Obstacle o;
                int oCount = dynObstacles.Count, subCount;
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
