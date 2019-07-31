using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Random = UnityEngine.Random;
using Nebukam.Utils;

namespace Nebukam.ORCA
{ 

    /// <summary>
    /// This component allows an ORCAAgent to control a gameobject's position and orientation.
    /// Its ORCAAgent property must be provided with a valid agent from an ORCASolver in order for it to function properly.
    /// </summary>
    public class ORCAAgentComponent : MonoBehaviour
    {

        const string sdef = "useSolverSettings";

        protected IORCAAgent m_ORCAAgent = null;
        public IORCAAgent ORCAAgent
        {
            get { return m_ORCAAgent; }
            set
            {
                if (m_ORCAAgent == value) { return; }
                m_ORCAAgent = value;
            }
        }

        protected Vector3 m_targetPosition = Vector3.zero;

        /// <summary>
        /// The current goal of this agent.
        /// </summary>
        public Vector3 targetPosition
        {
            get { return m_targetPosition; }
            set { m_targetPosition = value; }
        }

        [ConditionalField("fixedStep")]

        [Header("ORCA Solver")]

        [Tooltip("Should this agent use the default available ORCA solver ?")]
        public bool useDefaultSolver = false;
        [ConditionalField("useDefaultSolver", null, true)]
        [Tooltip("Solver Component controlling this agent")]
        public ORCASolverComponent solverComponent = null;
        

        [Header("ORCA Settings")]
        public bool useSolverSettings = true;
        
        [ConditionalField(sdef, null, true)]
        [Tooltip("How far this agent look to avoid collision. Lower is better.")]
        public float neighborsDist = 20.0f;

        [ConditionalField(sdef, null, true)]
        [Tooltip("How many agents to consider while looking for a path. Lower is better.")]
        public int neighborsMaxCount = 15;

        [ConditionalField(sdef, null, true)]
        [Tooltip("How soon this agent accounts for other agents. Lower is better.")]
        public float timeHorizon = 15.0f;

        [ConditionalField(sdef, null, true)]
        [Tooltip("How soon this agent accounts for obstacles. Lower is better.")]
        public float timeHorizonObstacle = 5.0f;
        
        [ConditionalField(sdef, null, true)]
        [Tooltip("Collision radius of the agent")]
        public float radius = 0.5f;

        [ConditionalField(sdef, null, true)]
        [Tooltip("Moving speed of the agent")]
        public float speed = 1.0f;

        [ConditionalField(sdef, null, true)]
        [Tooltip("Maximum speed of the agent. High value may cause odd accelerations.")]
        public float maxSpeed = 20.0f;
        
        [Tooltip("How fast does this agent turn ?")]
        public float turnSpeed = 5.0f;
        
        [EnumFlags]
        [Tooltip("Layer on which this agent can be seen by other agents")]
        public ORCALayer layerPresence = ORCALayer.ALL;
        
        [EnumFlags]
        [Tooltip("Layer this agent will not look at while resolving RVOs")]
        public ORCALayer layerIgnore = ORCALayer.NONE;

        [Header("Controls")]
        [Tooltip("Add a slight amount of noise to the velocity vector each update to avoid deadlocks.")]
        public bool addNoise = true;
        [Tooltip("Is the Agent navigation simulated ?")]
        public bool navigationEnabled = true;
        [Tooltip("Is the Agent collision with other agents simulated ?")]
        public bool collisionEnabled = true;
        [Tooltip("Should the Agent control the gameobject orientation ?")]
        public bool controlLookAt = true;
        [Tooltip("Should the Agent look at his target (instead of velocity) ?")]
        public bool lookAtTarget = true;
        
        protected Vector3 currentForward = Vector3.zero;

#if UNITY_EDITOR

        [Header("ORCA Debug")]
        public bool drawSelectedOnly = true;
        public Color drawColor = Color.black;
        public bool drawRadius = true;
        public bool drawNeighborsConnections = true;

#endif
        // Use this for initialization
        void Start()
        {
            if (enabled)
            {
                TryAssignAgent();
            }
        }

        /// <summary>
        /// Try to auto-assign a solver and an agent.
        /// </summary>
        void TryAssignAgent()
        {

            if (m_ORCAAgent != null) { return; }

            ORCASolverComponent comp = null;

            if (useDefaultSolver)
            {
                comp = ORCA.instance.defaultSolver;
            }
            else
            {
                comp = solverComponent;
            }

            if(comp == null) { return; }

            solverComponent = comp;

            if (useSolverSettings)
            {
                neighborsDist = comp.neighborsDist;
                neighborsMaxCount = comp.neighborsMaxCount;
                timeHorizon = comp.timeHorizon;
                timeHorizonObstacle = comp.timeHorizonObstacle;
                radius = comp.radius;
                maxSpeed = comp.maxSpeed;
            }
            
            m_ORCAAgent = comp.solver.AddAgent(currentPos);

        }

        protected virtual float2 currentPos {
            get{
                Vector3 pos = transform.position;
                return float2(pos.x, pos.z);
            }
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (drawColor.a == 0f)
            {
                drawColor = new Color(Random.value, Random.value, Random.value, 1.0f);
            }
#endif
        }
        
        // Update is called once per frame
        void Update()
        {
            if (!enabled) { return; }

            if(m_ORCAAgent != null)
            {
                UpdateAgent();
                UpdateVelocityAndPosition();

#if UNITY_EDITOR
                if (!drawSelectedOnly)
                {
                    DrawDebug();
                }
#endif

            }
            else
            {
                TryAssignAgent();
            }

        }

        protected virtual void UpdateAgent()
        {

            m_ORCAAgent.neighborDist = neighborsDist;
            m_ORCAAgent.maxNeighbors = layerIgnore == ORCALayer.ALL ? 0 : neighborsMaxCount;
            m_ORCAAgent.timeHorizon = timeHorizon;
            m_ORCAAgent.timeHorizonObst = timeHorizonObstacle;

            m_ORCAAgent.radius = radius;
            m_ORCAAgent.maxSpeed = maxSpeed;

            m_ORCAAgent.layerPresence = layerPresence;
            m_ORCAAgent.layerIgnore = layerIgnore;

            m_ORCAAgent.navigationEnabled= navigationEnabled;
            m_ORCAAgent.collisionEnabled = collisionEnabled;

        }

        protected virtual void UpdateVelocityAndPosition()
        {

            Vector2 pos = m_ORCAAgent.position;

            if (navigationEnabled)
            {
                transform.position = new Vector3(pos.x, transform.position.y, pos.y);
            }
            else
            {
                pos = transform.position;
                m_ORCAAgent.position = pos;
            }

            if (controlLookAt)
            {
                Vector2 vel = lookAtTarget ? m_ORCAAgent.prefVelocity : m_ORCAAgent.newVelocity;
                currentForward = Vector3.Slerp(currentForward, vel, turnSpeed * Time.deltaTime);

                if (Mathf.Abs(currentForward.x) > 0.01f && Mathf.Abs(currentForward.y) > 0.01f)
                {
                    transform.forward = new Vector3(currentForward.x, 0, currentForward.y).normalized;
                }
            }

            float2 goalVector = (Vector2)m_targetPosition - pos;
            if (lengthsq(goalVector) > 1.0f)
            {
                goalVector = normalize(goalVector) * speed;
            }

            if(addNoise)
            {
                /* Perturb a little to avoid deadlocks due to perfect symmetry. */
                float angle = Random.value * 2.0f * (float)Mathf.PI;
                float dist = Random.value * 0.0001f;

                m_ORCAAgent.prefVelocity = goalVector + dist * float2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            else
            {
                m_ORCAAgent.prefVelocity = goalVector;
            }
            
        }

        private void OnDestroy()
        {
            if(m_ORCAAgent != null)
            {
                m_ORCAAgent.solver.RemoveAgent(m_ORCAAgent);
                m_ORCAAgent = null;
            }
        }

#if UNITY_EDITOR

        private void OnDrawGizmosSelected()
        {
            if (drawSelectedOnly)
            {
                DrawDebug();
            }
        }

        protected virtual void DrawDebug()
        {

            if(m_ORCAAgent == null) { return; }

            Vector3 pos = (Vector2)m_ORCAAgent.position;
            Vector3 oPos;
            pos.z = pos.y;
            pos.y = 0f;

            if (drawRadius)
            {
                Draw.Circle(pos, m_ORCAAgent.radius, drawColor);
            }

            if (drawNeighborsConnections)
            {
                ORCAAgent r = m_ORCAAgent as ORCAAgent;
                int count = r.m_agentNeighbors.Count;
                for(int i = 0; i < count; i++)
                {
                    oPos = (Vector2)r.m_agentNeighbors[i].Value.position;
                    oPos.z = oPos.y;
                    oPos.y = 0f;
                    Draw.Line(pos, Vector3.Lerp(pos, oPos, 0.5f), drawColor);
                }
            }
        }

#endif

    }

}