using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Utils;
using Random = UnityEngine.Random;

namespace Nebukam.ORCA
{ 

    public class ORCAAgentComponent2D : ORCAAgentComponent
    {

        protected override float2 currentPos
        {
            get
            {
                Vector3 pos = transform.position;
                return float2(pos.x, pos.y);
            }
        }

        protected override void UpdateVelocityAndPosition()
        {

            Vector2 pos = m_ORCAAgent.position;

            if (navigationEnabled)
            {
                transform.position = pos;
            }
            else
            {
                pos = transform.position;
                m_ORCAAgent.position = pos;
            }
            
            if (controlLookAt)
            {
                Vector2 vel = lookAtTarget ? m_ORCAAgent.prefVelocity : m_ORCAAgent.newVelocity;
                currentForward = Vector2.Lerp(currentForward, vel, turnSpeed * Time.deltaTime);

                if (Mathf.Abs(currentForward.x) > 0.01f && Mathf.Abs(currentForward.y) > 0.01f)
                {
                    transform.up = (Vector2)currentForward.normalized;
                }
            }

            float2 goalVector = (Vector2)m_targetPosition - pos;

            if (lengthsq(goalVector) > 1.0f)
            {
                goalVector = normalize(goalVector) * speed;
            }

            if (addNoise)
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

#if UNITY_EDITOR
        
        protected override void DrawDebug()
        {

            if (m_ORCAAgent == null) { return; }

            Vector2 pos = m_ORCAAgent.position;
            Vector2 oPos;

            if (drawRadius)
            {
                Draw.Circle2D(float3(pos, 0f), m_ORCAAgent.radius, drawColor);
            }

            if (drawNeighborsConnections)
            {
                ORCAAgent r = m_ORCAAgent as ORCAAgent;
                int count = r.m_agentNeighbors.Count;
                for(int i = 0; i < count; i++)
                {
                    oPos = r.m_agentNeighbors[i].Value.position;
                    Draw.Line(float3(pos, 0f), Vector3.Lerp(pos, oPos, 0.5f), drawColor);
                }
            }
        }

#endif

    }

}