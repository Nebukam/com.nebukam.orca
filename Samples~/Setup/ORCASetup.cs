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
        private ORCA simulation;
        private ObstacleKDTreeBuilder obstacleBuilder;

        public int seed = 12345;
        public float deltaMultiplier = 1f;
        public int agentCount = 50;
        public int obstacleCount = 100;
        public float maxAgentRadius = 2f;
        public float maxObstacleRadius = 2f;
        public int minObstacleEdgeCount = 2;
        public int maxObstacleEdgeCount = 2;
        public Transform target;

        public float2 min, max; //obstacle spawn boundaries

        private void Awake()
        {

            agents = new AgentGroup<Agent>();

            obstacles = new ObstacleGroup();

            simulation = new ORCA();
            simulation.agents = agents;
            simulation.obstacles = obstacles;
            simulation.deltaMultiplier = deltaMultiplier;

        }

        private void Start()
        {

            Random.InitState(seed);

            #region create obstacles

            float dirRange = 20f;
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
                    dir = normalize(Maths.RotateAroundPivot(dir, float3(false), float3(0f, 0f, Random.value)));
                    pt = pt + dir * Random.Range(1f, maxObstacleRadius);
                    vList.Add(pt);
                }

                //if (vCount != 2) { vList.Add(start); }

                obstacles.Add(vList);
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

            //Draw obstacles
            Obstacle o;
            int oCount = obstacles.Count, subCount;
            for (int i = 0; i < oCount; i++)
            {
                o = obstacles[i];
                subCount = o.Count;

                //Draw each segment
                for (int j = 1, count = o.Count; j < count; j++)
                {
                    Draw.Line(o[j - 1].pos, o[j].pos);
                }
                //Draw closing segment (simulation consider 2+ segments to be closed.)
                Draw.Line(o[subCount - 1].pos, o[0].pos);
            }
            
        }

        private void LateUpdate()
        {
            //This will force the simulation to complete on the main thread and apply its results
            //simulation.Complete();

            //This will apply the simulation job results, as soon as it is finished
            simulation.TryComplete();

        }

        private void OnApplicationQuit()
        {
            //Make sure to clean-up the jobs
            simulation.DisposeAll();
        }

    }
}
