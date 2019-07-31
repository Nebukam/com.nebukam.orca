using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Utils;
using Random = UnityEngine.Random;

namespace Nebukam.ORCA
{

    public class ORCASolverComponent : MonoBehaviour
    {

        [Header("N:ORCA")]
        public bool registerAsDefaultSolver = true;

        [Header("Simulation")]
        public bool fixedStep = false;
        [ConditionalField("fixedStep")]
        public float fixedStepValue = 0.25f;

        [Header("Agent Defaults")]
        [Tooltip("How far this agent look to avoid collision. Lower is better.")]
        public float neighborsDist = 20.0f;
        [Tooltip("How many agents to consider while looking for a path. Lower is better.")]
        public int neighborsMaxCount = 15;
        [Tooltip("How soon this agent accounts for other agents. Lower is better.")]
        public float timeHorizon = 15.0f;
        [Tooltip("How soon this agent accounts for obstacles. Lower is better.")]
        public float timeHorizonObstacle = 5.0f;
        [Tooltip("Collision radius of the agent")]
        public float radius = 0.5f;
        [Tooltip("Moving speed of the agent")]
        public float speed = 1.0f;
        [Tooltip("Maximum speed of the agent. High value may cause odd accelerations.")]
        public float maxSpeed = 20.0f;
        [EnumFlags]
        [Tooltip("Layer on which this agent can be seen by other agents")]
        public ORCALayer layerPresence = ORCALayer.ALL;
        [EnumFlags]
        [Tooltip("Layer this agent will not look at while resolving RVOs")]
        public ORCALayer layerIgnore = ORCALayer.NONE;

        protected ORCASolver m_solver = null;
        public ORCASolver solver { get { return m_solver; } }

        protected Dictionary<IORCAAgent, ORCAAgentComponent> m_agentMap = new Dictionary<IORCAAgent, ORCAAgentComponent>();

        private void Awake()
        {
            m_solver = ORCASolver.CreateSolver(
                float2(false),
                fixedStepValue,
                neighborsDist,
                neighborsMaxCount,
                timeHorizon,
                timeHorizonObstacle,
                radius,
                maxSpeed);

            ORCA.instance.Register(this, registerAsDefaultSolver);
        }

        private void Start()
        {
            m_solver.ProcessObstacles();
        }

        private void Update()
        {
            if (!enabled) { return; }

            m_solver.timestep = fixedStep ? fixedStepValue : Time.deltaTime * 10f;
            m_solver.DoStep();
        }

        private void OnDestroy()
        {
            ORCA.instance.Unregister(this);
        }

        public bool TryGetNearAgent(out IORCAAgent agent, float2 position, float radius)
        {
            agent = m_solver.QueryNearAgent(position, radius);
            return agent != null;
        }

    }

}