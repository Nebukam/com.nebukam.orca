using System.Collections.Generic;
using UnityEngine;
using Nebukam.Utils;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    public interface IORCAAgent
    {
        
        ORCASolver solver { get; }

        int id { get; }

        bool requirePreparation { get; set; }
        int preparationOrder { get; set; }

        /// <summary>
        /// A list of specific agents to ignore.
        /// Only checked, and not maintained in any way.
        /// </summary>
        HashSet<IORCAAgent> ignoreHash { get; }

        /// <summary>
        /// ORCA layers on which this Agent exists.
        /// </summary>
        ORCALayer layerPresence { get; set; }

        /// <summary>
        /// ORCA layers this agent ignores. 
        /// An agent may exist on ignored layers, as well as colliding with layers it does not exist on.
        /// </summary>
        ORCALayer layerIgnore { get; set; }

        /// <summary>
        /// Return whether a given agent will be ignored based solely
        /// on ignoreHash & layer setup.
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        bool WillIgnore(IORCAAgent agent);

        float2 position { get; set; }
        float2 prefVelocity { get; set; }

        /// <summary>
        /// The default initial two-dimensional linear velocity of this agent.
        /// </summary>
        float2 velocity { get; set; }

        /// <summary>
        /// The maximum number of other agents this agent takes into account in the navigation.
        /// The larger this number, the longer the running time of the simulation.
        /// </summary>
        int maxNeighbors { get; set; }

        /// <summary>
        /// The maximum speed of this agent.
        /// </summary>
        float maxSpeed { get; set; }

        /// <summary>
        /// The maximum distance (center point to center point) 
        /// to other agents this agent takes into account in the navigation. 
        /// The larger this number, the longer the running time of the simulation.
        /// </summary>
        float neighborDist { get; set; }

        /// <summary>
        /// The radius of this agent.
        /// </summary>
        float radius { get; set; }

        /// <summary>
        /// The minimal amount of time for which this
        /// agent's velocities that are computed by the simulation are safe with
        /// respect to other agents.The larger this number, the sooner this
        /// agent will respond to the presence of other agents, but the less
        /// freedom this agent has in choosing its velocities.
        /// </summary>
        float timeHorizon { get; set; }

        /// <summary>
        /// The minimal amount of time for which
        /// this agent's velocities that are computed by the simulation are safe
        /// with respect to obstacles.The larger this number, the sooner this
        /// agent will respond to the presence of obstacles, but the less freedom
        /// this agent has in choosing its velocities.
        /// </summary>
        float timeHorizonObst { get; set; }

        float2 newVelocity { get; }

    }

    /// <summary>
    /// Defines an agent in the simulation.
    /// </summary>
    public class ORCAAgent : SolverChild, IORCAAgent
    {

        internal int m_id = 0;

        internal IList<KeyValuePair<float, ORCAAgent>> m_agentNeighbors = new List<KeyValuePair<float, ORCAAgent>>();
        internal IList<KeyValuePair<float, Obstacle>> m_obstacleNeighbors = new List<KeyValuePair<float, Obstacle>>();
        internal IList<ORCALine> m_orcaLines = new List<ORCALine>();

        internal bool m_requirePreparation = false;
        internal int m_preparationOrder = 0;
        
        internal ORCALayer m_layerPresence = ORCALayer.ALL;
        internal ORCALayer m_layerIgnore = ORCALayer.NONE;

        internal float2 m_position;
        internal float2 m_prefVelocity;
        internal float2 m_velocity;
        
        internal int m_maxNeighbors = 0;
        internal float m_maxSpeed = 0.0f;
        internal float m_neighborDist = 0.0f;
        internal float m_radius = 0.0f;
        internal float m_timeHorizon = 0.0f;
        internal float m_timeHorizonObst = 0.0f;
        internal bool m_needDelete = false;
        
        private float2 m_newVelocity;

        public int id
        {
            get { return m_id; }
        }
        

        public bool requirePreparation
        {
            get { return m_requirePreparation; }
            set { m_requirePreparation = value; }
        }

        public int preparationOrder
        {
            get { return m_preparationOrder; }
            set { m_preparationOrder = value; }
        }

        protected HashSet<IORCAAgent> m_ignoreHash = new HashSet<IORCAAgent>();
        public HashSet<IORCAAgent> ignoreHash { get { return m_ignoreHash; } }

        public ORCALayer layerPresence {
            get { return m_layerPresence; }
            set { m_layerPresence = value; }
        }

        public ORCALayer layerIgnore {
            get { return m_layerIgnore; }
            set { m_layerIgnore = value; }
        }

        public virtual float2 position {
            get { return m_position; }
            set { m_position = value; }
        }
        public virtual float2 prefVelocity {
            get { return m_prefVelocity; }
            set { m_prefVelocity = value; }
        }
        public virtual float2 velocity {
            get { return m_velocity; }
            set { m_velocity = value; }
        }

        public virtual int maxNeighbors
        {
            get { return m_maxNeighbors; }
            set { m_maxNeighbors = value; }
        }
        public virtual float maxSpeed {
            get { return m_maxSpeed; }
            set { m_maxSpeed = value; }
        }
        public virtual float neighborDist {
            get { return m_neighborDist; }
            set { m_neighborDist = value; }
        }
        public virtual float radius {
            get { return m_radius; }
            set { m_radius = value; }
        }
        public virtual float timeHorizon {
            get { return m_timeHorizon; }
            set { m_timeHorizon = value; }
        }
        public virtual float timeHorizonObst {
            get { return m_timeHorizonObst; }
            set { m_timeHorizonObst = value; }
        }

        
        public virtual float2 newVelocity
        {
            get { return m_newVelocity; }
        }
        /// <summary>
        /// Computes the neighbors of this agent.
        /// </summary>
        internal virtual void ComputeNeighbors()
        {
            m_obstacleNeighbors.Clear();
            float rangeSq = lengthsq(m_timeHorizonObst * m_maxSpeed + m_radius);
            m_solver.m_kdTree.ComputeObstacleNeighbors(this, rangeSq);

            m_agentNeighbors.Clear();

            if (m_maxNeighbors > 0)
            {
                rangeSq = lengthsq(m_neighborDist);
                m_solver.m_kdTree.ComputeAgentNeighbors(this, ref rangeSq);
            }
        }

        /// <summary>
        /// Computes the new velocity of this agent.
        /// </summary>
        internal virtual void ComputeNewVelocity()
        {
            m_orcaLines.Clear();

            float invTimeHorizonObst = 1.0f / m_timeHorizonObst;

            // Create obstacle ORCA lines. */
            for (int i = 0; i < m_obstacleNeighbors.Count; ++i)
            {

                Obstacle o1 = m_obstacleNeighbors[i].Value;
                Obstacle o2 = o1.next;

                float2 relPos1 = o1.point - m_position;
                float2 relPos2 = o2.point - m_position;
                
                // Check if velocity obstacle of obstacle is already taken care
                // of by previously constructed obstacle ORCA lines.
                bool alreadyCovered = false;

                for (int j = 0; j < m_orcaLines.Count; ++j)
                {
                    if (Maths.Det(invTimeHorizonObst * relPos1 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * m_radius 
                        >= -Maths.EPSILON && Maths.Det(invTimeHorizonObst * relPos2 - m_orcaLines[j].point, m_orcaLines[j].dir) - invTimeHorizonObst * m_radius >= -Maths.EPSILON)
                    {
                        alreadyCovered = true;
                        break;
                    }
                }

                if (alreadyCovered)
                {
                    continue;
                }

                // Not yet covered. Check for collisions.
                float distSq1 = lengthsq(relPos1);
                float distSq2 = lengthsq(relPos2);

                float radiusSq = lengthsq(m_radius);

                float2 obstacleVector = o2.point - o1.point;
                float s = lengthsq(obstacleVector / dot(-relPos1, obstacleVector));
                float distSqLine =  lengthsq(-relPos1 - (s * obstacleVector));

                ORCALine line;

                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    // Collision with left vertex. Ignore if non-convex.
                    if (o1.convex)
                    {
                        line.point = float2(false);
                        line.dir = normalize(float2(-relPos1.y, relPos1.x));
                        m_orcaLines.Add(line);
                    }

                    continue;
                }
                else if (s > 1.0f && distSq2 <= radiusSq)
                {
                    // Collision with right vertex. Ignore if non-convex or if
                    // it will be taken care of by neighboring obstacle.
                    if (o2.convex && Maths.Det(relPos2, o2.dir) >= 0.0f)
                    {
                        line.point = float2(false);
                        line.dir = normalize(float2(-relPos2.y, relPos2.x));
                        m_orcaLines.Add(line);
                    }

                    continue;
                }
                else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    // Collision with obstacle segment.
                    line.point = float2(false);
                    line.dir = -o1.dir;
                    m_orcaLines.Add(line);

                    continue;
                }

                // No collision. Compute legs. When obliquely viewed, both legs
                // can come from a single vertex. Legs extend cut-off line when
                // non-convex vertex.

                float2 lLegDir, rLegDir;

                if (s < 0.0f && distSqLine <= radiusSq)
                {
                 
                    // Obstacle viewed obliquely so that left vertex
                    // defines velocity obstacle.
                    if (!o1.convex)
                    {
                        // Ignore obstacle.
                        continue;
                    }

                    o2 = o1;

                    float leg1 = sqrt(distSq1 - radiusSq);
                    lLegDir = float2(relPos1.x * leg1 - relPos1.y * m_radius, relPos1.x * m_radius + relPos1.y * leg1) / distSq1;
                    rLegDir = float2(relPos1.x * leg1 + relPos1.y * m_radius, -relPos1.x * m_radius + relPos1.y * leg1) / distSq1;
                }
                else if (s > 1.0f && distSqLine <= radiusSq)
                {

                    // Obstacle viewed obliquely so that
                    // right vertex defines velocity obstacle.
                    if (!o2.convex)
                    {
                        // Ignore obstacle.
                        continue;
                    }

                    o1 = o2;

                    float leg2 = sqrt(distSq2 - radiusSq);
                    lLegDir = float2(relPos2.x * leg2 - relPos2.y * m_radius, relPos2.x * m_radius + relPos2.y * leg2) / distSq2;
                    rLegDir = float2(relPos2.x * leg2 + relPos2.y * m_radius, -relPos2.x * m_radius + relPos2.y * leg2) / distSq2;
                }
                else
                {
                    // Usual situation.
                    if (o1.convex)
                    {
                        float leg1 = sqrt(distSq1 - radiusSq);
                        lLegDir = float2(relPos1.x * leg1 - relPos1.y * m_radius, relPos1.x * m_radius + relPos1.y * leg1) / distSq1;
                    }
                    else
                    {
                        // Left vertex non-convex; left leg extends cut-off line.
                        lLegDir = -o1.dir;
                    }

                    if (o2.convex)
                    {
                        float leg2 = sqrt(distSq2 - radiusSq);
                        rLegDir = float2(relPos2.x * leg2 + relPos2.y * m_radius, -relPos2.x * m_radius + relPos2.y * leg2) / distSq2;
                    }
                    else
                    {
                        // Right vertex non-convex; right leg extends cut-off line.
                        rLegDir = o1.dir;
                    }
                }
                
                // Legs can never point into neighboring edge when convex
                // vertex, take cutoff-line of neighboring edge instead. If
                // velocity projected on "foreign" leg, no constraint is added.

                Obstacle leftNeighbor = o1.prev;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                if (o1.convex && Maths.Det(lLegDir, -leftNeighbor.dir) >= 0.0f)
                {
                    // Left leg points into obstacle.
                    lLegDir = -leftNeighbor.dir;
                    isLeftLegForeign = true;
                }

                if (o2.convex && Maths.Det(rLegDir, o2.dir) <= 0.0f)
                {
                    // Right leg points into obstacle.
                    rLegDir = o2.dir;
                    isRightLegForeign = true;
                }

                // Compute cut-off centers.
                float2 leftCutOff = invTimeHorizonObst * (o1.point - m_position);
                float2 rightCutOff = invTimeHorizonObst * (o2.point - m_position);
                float2 cutOffVector = rightCutOff - leftCutOff;

                // Project current velocity on velocity obstacle.

                // Check if current velocity is projected on cutoff circles.
                float t = o1 == o2 ? 0.5f : dot((m_velocity - leftCutOff), cutOffVector) / lengthsq(cutOffVector);
                float tLeft = dot((m_velocity - leftCutOff), lLegDir);
                float tRight = dot((m_velocity - rightCutOff), rLegDir);

                if ((t < 0.0f && tLeft < 0.0f) || (o1 == o2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    // Project on left cut-off circle.
                    float2 unitW = normalize(m_velocity - leftCutOff);

                    line.dir = float2(unitW.y, -unitW.x);
                    line.point = leftCutOff + m_radius * invTimeHorizonObst * unitW;
                    m_orcaLines.Add(line);

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    // Project on right cut-off circle.
                    float2 unitW = normalize(m_velocity - rightCutOff);

                    line.dir = float2(unitW.y, -unitW.x);
                    line.point = rightCutOff + m_radius * invTimeHorizonObst * unitW;
                    m_orcaLines.Add(line);

                    continue;
                }

                // Project on left leg, right leg, or cut-off line, whichever is
                // closest to velocity.
                float distSqCutoff = (t < 0.0f || t > 1.0f || o1 == o2) ? float.PositiveInfinity : lengthsq(m_velocity - (leftCutOff + t * cutOffVector));
                float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : lengthsq(m_velocity - (leftCutOff + tLeft * lLegDir));
                float distSqRight = tRight < 0.0f ? float.PositiveInfinity : lengthsq(m_velocity - (rightCutOff + tRight * rLegDir));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    // Project on cut-off line.
                    line.dir = -o1.dir;
                    line.point = leftCutOff + m_radius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                    m_orcaLines.Add(line);

                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    // Project on left leg.
                    if (isLeftLegForeign)
                    {
                        continue;
                    }

                    line.dir = lLegDir;
                    line.point = leftCutOff + m_radius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                    m_orcaLines.Add(line);

                    continue;
                }

                // Project on right leg.
                if (isRightLegForeign)
                {
                    continue;
                }

                line.dir = -rLegDir;
                line.point = rightCutOff + m_radius * invTimeHorizonObst * float2(-line.dir.y, line.dir.x);
                m_orcaLines.Add(line);
            }

            int numObstLines = m_orcaLines.Count;

            float invTimeHorizon = 1.0f / m_timeHorizon;

            // Create agent ORCA lines.
            for (int i = 0; i < m_agentNeighbors.Count; ++i)
            {
                ORCAAgent other = m_agentNeighbors[i].Value;

                float2 relPos = other.m_position - m_position;
                float2 relVel = m_velocity - other.m_velocity;
                float distSq = lengthsq(relPos);
                float cRad = m_radius + other.m_radius;
                float cRadSq = lengthsq(cRad);

                ORCALine line;
                float2 u;

                if (distSq > cRadSq)
                {
                    // No collision.
                    float2 w = relVel - invTimeHorizon * relPos;

                    // Vector from cutoff center to relative velocity.
                    float wLengthSq = lengthsq(w);
                    float dotProduct1 = dot(w, relPos);

                    if (dotProduct1 < 0.0f && lengthsq(dotProduct1) > cRadSq * wLengthSq)
                    {
                        // Project on cut-off circle.
                        float wLength = sqrt(wLengthSq);
                        float2 unitW = w / wLength;

                        line.dir = float2(unitW.y, -unitW.x);
                        u = (cRad * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        // Project on legs.
                        float leg = sqrt(distSq - cRadSq);

                        if (Maths.Det(relPos, w) > 0.0f)
                        {
                            // Project on left leg.
                            line.dir = float2(relPos.x * leg - relPos.y * cRad, relPos.x * cRad + relPos.y * leg) / distSq;
                        }
                        else
                        {
                            // Project on right leg.
                            line.dir = -float2(relPos.x * leg + relPos.y * cRad, -relPos.x * cRad + relPos.y * leg) / distSq;
                        }

                        float dotProduct2 = dot(relVel, line.dir);
                        u = dotProduct2 * line.dir - relVel;
                    }
                }
                else
                {
                    // Collision. Project on cut-off circle of time timeStep.
                    float invTimeStep = 1.0f / m_solver.m_timeStep;

                    // Vector from cutoff center to relative velocity.
                    float2 w = relVel - invTimeStep * relPos;

                    float wLength = length(w);
                    float2 unitW = w / wLength;

                    line.dir = float2(unitW.y, -unitW.x);
                    u = (cRad * invTimeStep - wLength) * unitW;
                }

                line.point = m_velocity + 0.5f * u;
                m_orcaLines.Add(line);
            }

            int lineFail = LP2(m_orcaLines, m_maxSpeed, m_prefVelocity, false, ref m_newVelocity);

            if (lineFail < m_orcaLines.Count)
            {
                LP3(m_orcaLines, numObstLines, lineFail, m_maxSpeed, ref m_newVelocity);
            }
        }
        
        /// <summary>
        /// Inserts an agent neighbor into the set of neighbors of this agent.
        /// </summary>
        /// <param name="agent">A pointer to the agent to be inserted.</param>
        /// <param name="rangeSq">The squared range around this agent.</param>
        internal virtual void InsertAgentNeighbor(ORCAAgent agent, ref float rangeSq)
        {

            //If the inserted agent is self, or exists only on layers this agent ignores, ignore.
            if (this == agent) { return; }
            if(m_ignoreHash.Count > 0 && m_ignoreHash.Contains(agent)) { return; }
            if((agent.m_layerPresence & ~m_layerIgnore) == 0) { return; }
            
            float distSq = lengthsq(m_position - agent.m_position);

            if (distSq < rangeSq)
            {
                if (m_agentNeighbors.Count < m_maxNeighbors)
                {
                    m_agentNeighbors.Add(new KeyValuePair<float, ORCAAgent>(distSq, agent));
                }

                int i = m_agentNeighbors.Count - 1;

                while (i != 0 && distSq < m_agentNeighbors[i - 1].Key)
                {
                    m_agentNeighbors[i] = m_agentNeighbors[i - 1];
                    --i;
                }

                m_agentNeighbors[i] = new KeyValuePair<float, ORCAAgent>(distSq, agent);

                if (m_agentNeighbors.Count == m_maxNeighbors)
                {
                    rangeSq = m_agentNeighbors[m_agentNeighbors.Count - 1].Key;
                }
            }
            
        }

        /// <summary>
        /// Return whether a given agent will be ignored based solely
        /// on ignoreHash & layer setup.
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        public virtual bool WillIgnore(IORCAAgent agent)
        {
            if (this == agent 
                || (m_ignoreHash.Count > 0 && m_ignoreHash.Contains(agent))
                || ((agent.layerPresence & ~m_layerIgnore) == 0))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Inserts a static obstacle neighbor into the set of neighbors of this agent.
        /// </summary>
        /// <param name="obstacle">The number of the static obstacle to be inserted.</param>
        /// <param name="rangeSq">The squared range around this agent.</param>
        internal virtual void InsertObstacleNeighbor(Obstacle obstacle, float rangeSq)
        {
            Obstacle nextObstacle = obstacle.next;

            float distSq = Maths.DistSqPointLineSegment(obstacle.point, nextObstacle.point, m_position);

            if (distSq < rangeSq)
            {
                m_obstacleNeighbors.Add(new KeyValuePair<float, Obstacle>(distSq, obstacle));

                int i = m_obstacleNeighbors.Count - 1;

                while (i != 0 && distSq < m_obstacleNeighbors[i - 1].Key)
                {
                    m_obstacleNeighbors[i] = m_obstacleNeighbors[i - 1];
                    --i;
                }
                m_obstacleNeighbors[i] = new KeyValuePair<float, Obstacle>(distSq, obstacle);
            }
        }
        
        //
        internal virtual void Prepare()
        {
            
        }

        /// <summary>
        /// Updates the two-dimensional position and two-dimensional velocity of this agent.
        /// </summary>
        internal virtual void Commit()
        {
            m_velocity = m_newVelocity;
            m_position += m_velocity * m_solver.m_timeStep;
        }

        #region Linear programs

        /// <summary>
        /// Solves a one-dimensional linear program on a specified line subject to linear 
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="lineNo">The specified line constraint.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="optVel">The optimization velocity.</param>
        /// <param name="dirOpt">True if the direction should be optimized.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        /// <returns>True if successful.</returns>
        private bool LP1(IList<ORCALine> lines, int lineNo, float radius, float2 optVel, bool dirOpt, ref float2 result)
        {
            float dotProduct = dot(lines[lineNo].point, lines[lineNo].dir);
            float discriminant = lengthsq(dotProduct) + lengthsq(radius) - lengthsq(lines[lineNo].point);

            if (discriminant < 0.0f)
            {
                // Max speed circle fully invalidates line lineNo.
                return false;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                float denominator = Maths.Det(lines[lineNo].dir, lines[i].dir);
                float numerator = Maths.Det(lines[i].dir, lines[lineNo].point - lines[i].point);

                if (abs(denominator) <= Maths.EPSILON)
                {
                    // Lines lineNo and i are (almost) parallel.
                    if (numerator < 0.0f)
                    {
                        return false;
                    }

                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    // Line i bounds line lineNo on the right.
                    tRight = min(tRight, t);
                }
                else
                {
                    // Line i bounds line lineNo on the left.
                    tLeft = max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (dirOpt)
            {
                // Optimize direction.
                if (dot(optVel, lines[lineNo].dir ) > 0.0f)
                {
                    // Take right extreme.
                    result = lines[lineNo].point + tRight * lines[lineNo].dir;
                }
                else
                {
                    // Take left extreme.
                    result = lines[lineNo].point + tLeft * lines[lineNo].dir;
                }
            }
            else
            {
                // Optimize closest point.
                float t = dot(lines[lineNo].dir, (optVel - lines[lineNo].point));

                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].dir;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].dir;
                }
                else
                {
                    result = lines[lineNo].point + t * lines[lineNo].dir;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Solves a two-dimensional linear program subject to linear 
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="optVel">The optimization velocity.</param>
        /// <param name="dirOpt">True if the direction should be optimized.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        /// <returns>The number of the line it fails on, and the number of lines if successful.</returns>
        private int LP2(IList<ORCALine> lines, float radius, float2 optVel, bool dirOpt, ref float2 result)
        {
            if (dirOpt)
            {
                // Optimize direction. Note that the optimization velocity is of
                // unit length in this case.
                result = optVel * radius;
            }
            else if (lengthsq(optVel) > (radius * radius))
            {
                // Optimize closest point and outside circle.
                result = normalize(optVel) * radius;
            }
            else
            {
                // Optimize closest point and inside circle.
                result = optVel;
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                if (Maths.Det(lines[i].dir, lines[i].point - result) > 0.0f)
                {
                    // Result does not satisfy constraint i. Compute new optimal result.
                    float2 tempResult = result;
                    if (!LP1(lines, i, radius, optVel, dirOpt, ref result))
                    {
                        result = tempResult;

                        return i;
                    }
                }
            }

            return lines.Count;
        }
        
        /// <summary>
        /// Solves a two-dimensional linear program subject to linear
        /// constraints defined by lines and a circular constraint.
        /// </summary>
        /// <param name="lines">Lines defining the linear constraints.</param>
        /// <param name="numObstLines">Count of obstacle lines.</param>
        /// <param name="beginLine">The line on which the 2-d linear program failed.</param>
        /// <param name="radius">The radius of the circular constraint.</param>
        /// <param name="result">A reference to the result of the linear program.</param>
        private void LP3(IList<ORCALine> lines, int numObstLines, int beginLine, float radius, ref float2 result)
        {
            float distance = 0.0f;

            for (int i = beginLine; i < lines.Count; ++i)
            {
                if (Maths.Det(lines[i].dir, lines[i].point - result) > distance)
                {
                    // Result does not satisfy constraint of line i.
                    IList<ORCALine> projLines = new List<ORCALine>();
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {
                        ORCALine line;

                        float determinant = Maths.Det(lines[i].dir, lines[j].dir);

                        if (Mathf.Abs(determinant) <= Maths.EPSILON)
                        {
                            // Line i and line j are parallel.
                            if (dot(lines[i].dir, lines[j].dir) > 0.0f)
                            {
                                // Line i and line j point in the same direction.
                                continue;
                            }
                            else
                            {
                                // Line i and line j point in opposite direction.
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            line.point = lines[i].point + (Maths.Det(lines[j].dir, lines[i].point - lines[j].point) / determinant) * lines[i].dir;
                        }

                        line.dir = normalize(lines[j].dir - lines[i].dir);
                        projLines.Add(line);
                    }

                    float2 tempResult = result;
                    if (LP2(projLines, radius, float2(-lines[i].dir.y, lines[i].dir.x), true, ref result) < projLines.Count)
                    {
                        // This should in principle not happen. The result is by
                        // definition already in the feasible region of this
                        // linear program. If it fails, it is due to small
                        // floating point error, and the current result is kept.
                        result = tempResult;
                    }

                    distance = Maths.Det(lines[i].dir, lines[i].point - result);
                }
            }
        }

        #endregion

    }
}
