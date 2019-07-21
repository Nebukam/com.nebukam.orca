using UnityEngine;
using Nebukam.Utils;

namespace Nebukam.ORCA
{ 

    public class ORCABehaviour : MonoBehaviour
    {

        protected IORCAAgent m_ORCAAgent = null;
        public IORCAAgent ORCAAgent
        {
            get { return m_ORCAAgent; }
            set
            {
                if (m_ORCAAgent == value) { return; }
                m_ORCAAgent = value;
                if (m_ORCAAgent != null) { OnRVOAgentAssigned(); }
            }
        }

        protected Vector3 m_targetPosition = Vector3.zero;
        public Vector3 targetPosition
        {
            get { return m_targetPosition; }
            set { m_targetPosition = value; }
        }

        [Header("RVO Awareness")]
        public float neighborsDist = 20.0f;
        public int neighborsMaxCount = 15;
        public float timeHorizon = 15.0f;
        public float timeHorizonObstacle = 5.0f;
        public bool addNoise = true;

        [Header("RVO Navigation")]
        public float radius = 0.5f;
        public float speed = 2.0f;
        public float maxSpeed = 20.0f;
        public float turnSpeed = 5.0f;

        protected Vector3 currentForward = Vector3.zero;

#if UNITY_EDITOR

        [Header("RVO Debug")]
        public bool drawSelectedOnly = true;
        public Color drawColor = Color.black;
        public bool drawRadius = true;
        public bool drawNeighborsConnections = true;

#endif
        // Use this for initialization
        void Start()
        {
            
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

        private void OnRVOAgentAssigned()
        {
            UpdateRVO();
        }

        // Update is called once per frame
        void Update()
        {
            if (!enabled)
            {
                return;
            }

            if(m_ORCAAgent != null)
            {
                UpdateRVO();
                UpdateVelocityAndPosition();

#if UNITY_EDITOR
                if (!drawSelectedOnly)
                {
                    DrawDebug();
                }
#endif

            }

        }

        protected virtual void UpdateRVO()
        {

            m_ORCAAgent.neighborDist = neighborsDist;
            m_ORCAAgent.maxNeighbors = neighborsMaxCount;
            m_ORCAAgent.timeHorizon = timeHorizon;
            m_ORCAAgent.timeHorizonObst = timeHorizonObstacle;

            m_ORCAAgent.radius = radius;
            m_ORCAAgent.maxSpeed = maxSpeed;

        }

        protected virtual void UpdateVelocityAndPosition()
        {

            Vector2 pos = m_ORCAAgent.position;
            Vector2 vel = m_ORCAAgent.newVelocity;// m_RVOAgent.prefVelocity;

            currentForward = Vector3.Slerp(currentForward, vel, turnSpeed * Time.deltaTime);

            transform.position = new Vector3(pos.x, transform.position.y, pos.y);

            if (Mathf.Abs(currentForward.x) > 0.01f && Mathf.Abs(currentForward.y) > 0.01f)
            {
                transform.forward = new Vector3(currentForward.x, 0, currentForward.y).normalized;
            }
        
            Vector2 goalVector = (Vector2)m_targetPosition - pos;

            if (goalVector.AbsSq() > 1.0f)
            {
                goalVector = goalVector.normalized * speed;
            }

            /* Perturb a little to avoid deadlocks due to perfect symmetry. */

            float angle = Random.value * 2.0f * (float)Mathf.PI;
            float dist = Random.value * 0.0001f;

            m_ORCAAgent.prefVelocity = goalVector + dist * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

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
            Vector3 pos = m_ORCAAgent.position;
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
                    oPos = r.m_agentNeighbors[i].Value.position;
                    oPos.z = oPos.y;
                    oPos.y = 0f;
                    Draw.Line(pos, Vector3.Lerp(pos, oPos, 0.5f), drawColor);
                }
            }
        }

#endif

    }

}