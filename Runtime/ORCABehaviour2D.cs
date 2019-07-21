using UnityEngine;
using Nebukam.Utils;

namespace Nebukam.ORCA
{ 

    public class ORCABehaviour2D : ORCABehaviour
    {
        
        protected override void UpdateVelocityAndPosition()
        {

            Vector2 pos = m_ORCAAgent.position;
            Vector2 vel = m_ORCAAgent.newVelocity;// m_RVOAgent.prefVelocity;

            currentForward = Vector2.Lerp(currentForward, vel, turnSpeed * Time.deltaTime);

            transform.position = pos;

            if (Mathf.Abs(currentForward.x) > 0.01f && Mathf.Abs(currentForward.y) > 0.01f)
            {
                transform.up = (Vector2)currentForward.normalized;
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
        
        protected override void DrawDebug()
        {
            Vector2 pos = m_ORCAAgent.position;
            Vector2 oPos;

            if (drawRadius)
            {
                Draw.Circle2D(pos, m_ORCAAgent.radius, drawColor);
            }

            if (drawNeighborsConnections)
            {
                ORCAAgent r = m_ORCAAgent as ORCAAgent;
                int count = r.m_agentNeighbors.Count;
                for(int i = 0; i < count; i++)
                {
                    oPos = r.m_agentNeighbors[i].Value.position;
                    Draw.Line(pos, Vector3.Lerp(pos, oPos, 0.5f), drawColor);
                }
            }
        }

#endif

    }

}