using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Nebukam.Utils;

namespace Nebukam.ORCA
{

    /// <summary>
    /// Defines a directed line.
    /// </summary>
    public struct ORCALine
    {
        public Vector2 dir;
        public Vector2 point;
    }

    /// <summary>
    /// Defines static obstacles in the simulation.
    /// </summary>
    internal class Obstacle
    {

        internal Obstacle next;
        internal Obstacle prev;
        internal Vector2 dir;
        internal Vector2 point;
        internal int id;
        internal bool convex;

        /// <summary>
        /// Clear this Obstacle references recursively
        /// </summary>
        internal void ClearRecursive()
        {
            next?.ClearRecursive();
            next = null;

            prev?.ClearRecursive();
            prev = null;

            //TODO : Return to pool.
        }

    }

    /// <summary>
    /// Define a generic class for RVO elements that require a pointer
    /// to the solver.
    /// </summary>
    public class SolverChild
    {
        protected ORCASolver m_solver = null;
        public ORCASolver solver
        {
            get { return m_solver; }
            internal set { m_solver = value; }
        }
    }

    /// <summary>
    /// Defines the simulation.
    /// </summary>
    public class ORCASolver
    {

        #region Statics

        static public ORCASolver CreateSolver(
            Vector2 defaultVelocity,
            float timestep = 0.25f,
            float defaultNeighborDist = 15.0f,
            int defaultNeighborCount = 10,
            float defaultTimeHorizon = 15.0f,
            float defaultTimeHorizonObst = 5.0f,
            float defaultRadius = 0.5f,
            float defaultMaxSpeed = 20.0f
            )
        {
            ORCASolver newSolver = new ORCASolver();
            newSolver.timestep = timestep;
            newSolver.SetAgentDefaults(
                defaultNeighborDist,
                defaultNeighborCount,
                defaultTimeHorizon,
                defaultTimeHorizonObst,
                defaultRadius,
                defaultMaxSpeed,
                defaultVelocity
                );

            return newSolver;
        }

        #endregion

        #region Worker

        /**
         * <summary>Defines a worker.</summary>
         */
        private class Worker : SolverChild
        {
            private ManualResetEvent m_doneEvent;
            private int m_end;
            private int m_start;

            /**
             * <summary>Constructs and initializes a worker.</summary>
             *
             * <param name="start">Start.</param>
             * <param name="end">End.</param>
             * <param name="doneEvent">Done event.</param>
             */
            internal Worker(int start, int end, ManualResetEvent doneEvent, ORCASolver s)
            {
                m_solver = s;
                m_start = start;
                m_end = end;
                m_doneEvent = doneEvent;
            }

            internal void config(int start, int end)
            {
                m_start = start;
                m_end = end;
            }

            /**
             * <summary>Performs a simulation step.</summary>
             *
             * <param name="obj">Unused.</param>
             */
            internal void step(object obj)
            {
                for (int index = m_start; index < m_end; ++index)
                {
                    m_solver.m_agents[index].ComputeNeighbors();
                    m_solver.m_agents[index].ComputeNewVelocity();
                }
                m_doneEvent.Set();
            }

            /**
             * <summary>updates the two-dimensional position and
             * two-dimensional velocity of each agent.</summary>
             *
             * <param name="obj">Unused.</param>
             */
            internal void update(object obj)
            {
                for (int index = m_start; index < m_end; ++index)
                {
                    m_solver.m_agents[index].Commit();
                }

                m_doneEvent.Set();
            }
        }

        #endregion

        internal IDictionary<int, int> m_agentNo2indexDict = new Dictionary<int, int>();
        internal IDictionary<int, int> m_index2agentNoDict = new Dictionary<int, int>();
        internal IList<ORCAAgent> m_agents = new List<ORCAAgent>();
        internal IList<Obstacle> m_obstacles = new List<Obstacle>();
        internal IList<Obstacle> m_dynamicObstacles = new List<Obstacle>();
        internal ORCAKdTree m_kdTree;
        internal float m_timeStep;
        
        private ORCAAgent m_defaultAgent = null;
        private ManualResetEvent[] m_doneEvents;
        private Worker[] m_workers;
        private int m_numWorkers;
        private int m_workerAgentCount;
        private float m_globalTime;

        public float timestep { get { return m_timeStep; } set { m_timeStep = value; } }
        public float globalTime { get { return m_globalTime; } set { m_globalTime = value; } }
                

        #region Agents

        public void RemoveAgent(IORCAAgent agent)
        {
            RemoveAgent(agent.id);
        }

        public void RemoveAgent(int agentNo)
        {
            m_agents[m_agentNo2indexDict[agentNo]].m_needDelete = true;
        }

        void UpdateDeleteAgent()
        {
            ORCAAgent agent;
            bool isDelete = false;
            for (int i = m_agents.Count - 1; i >= 0; i--)
            {
                agent = m_agents[i];
                if (agent.m_needDelete)
                {
                    m_agents.RemoveAt(i);
                    agent.solver = null;
                    isDelete = true;
                }
            }
            if (isDelete)
                OnAgentRemoved();
        }

        private int s_totalID = 0;
        
        /// <summary>
        /// Adds an existing agent to the simulation
        /// </summary>
        /// <param name="agent"></param>
        /// <returns></returns>
        public IORCAAgent AddAgent(IORCAAgent agent)
        {

            ORCAAgent a = agent as ORCAAgent;

            if (m_defaultAgent == null || a == null || m_agents.Contains(a))
            {
                return null;
            }

            a.m_id = s_totalID;
            s_totalID++;
            m_agents.Add(a);

            a.solver = this;
            
            OnAgentAdded();
            return agent;
        }

        public IORCAAgent AddAgent(Vector2 position)
        {
            return AddAgent<ORCAAgent>(position);
        }

        /// <summary>
        /// Adds a new agent with default properties to the solver.
        /// </summary>
        /// <param name="position">The two-dimensional starting position of this agent.</param>
        /// <returns>The number of the agent, or -1 when the agent defaults have not been set.</returns>
        public IORCAAgent AddAgent<T>(Vector2 position)
            where T : ORCAAgent, IORCAAgent, new()
        {
            if (m_defaultAgent == null)
            {
                return null;
            }

            T agent = new T();
            agent.m_id = s_totalID;
            s_totalID++;
            agent.maxNeighbors = m_defaultAgent.maxNeighbors;
            agent.maxSpeed = m_defaultAgent.maxSpeed;
            agent.neighborDist = m_defaultAgent.neighborDist;
            agent.position = position;
            agent.radius = m_defaultAgent.radius;
            agent.timeHorizon = m_defaultAgent.timeHorizon;
            agent.timeHorizonObst = m_defaultAgent.timeHorizonObst;
            agent.velocity = m_defaultAgent.velocity;
            agent.solver = this;
            m_agents.Add(agent);
            OnAgentAdded();
            return agent;
        }

        void OnAgentRemoved()
        {
            m_agentNo2indexDict.Clear();
            m_index2agentNoDict.Clear();

            for (int i = 0; i < m_agents.Count; i++)
            {
                int agentNo = m_agents[i].m_id;
                m_agentNo2indexDict.Add(agentNo, i);
                m_index2agentNoDict.Add(i, agentNo);
            }
        }

        void OnAgentAdded()
        {
            if (m_agents.Count == 0)
                return;

            int index = m_agents.Count - 1;
            int agentNo = m_agents[index].m_id;
            m_agentNo2indexDict.Add(agentNo, index);
            m_index2agentNoDict.Add(index, agentNo);
        }

        public IORCAAgent AddAgent(
            Vector2 position,
            float neighborDist,
            int maxNeighbors,
            float timeHorizon,
            float timeHorizonObst,
            float radius,
            float maxSpeed,
            Vector2 velocity)
        {
            return AddAgent<ORCAAgent>(
                position,
                neighborDist,
                maxNeighbors,
                timeHorizon,
                timeHorizonObst,
                radius,
                maxSpeed,
                velocity
                );
        }

        /**
            * <summary>Adds a new agent to the simulation.</summary>
            *
            * <returns>The number of the agent.</returns>
            *
            * <param name="position">The two-dimensional starting position of this
            * agent.</param>
            * <param name="neighborDist">The maximum distance (center point to
            * center point) to other agents this agent takes into account in the
            * navigation. The larger this number, the longer the running time of
            * the simulation. If the number is too low, the simulation will not be
            * safe. Must be non-negative.</param>
            * <param name="maxNeighbors">The maximum number of other agents this
            * agent takes into account in the navigation. The larger this number,
            * the longer the running time of the simulation. If the number is too
            * low, the simulation will not be safe.</param>
            * <param name="timeHorizon">The minimal amount of time for which this
            * agent's velocities that are computed by the simulation are safe with
            * respect to other agents. The larger this number, the sooner this
            * agent will respond to the presence of other agents, but the less
            * freedom this agent has in choosing its velocities. Must be positive.
            * </param>
            * <param name="timeHorizonObst">The minimal amount of time for which
            * this agent's velocities that are computed by the simulation are safe
            * with respect to obstacles. The larger this number, the sooner this
            * agent will respond to the presence of obstacles, but the less freedom
            * this agent has in choosing its velocities. Must be positive.</param>
            * <param name="radius">The radius of this agent. Must be non-negative.
            * </param>
            * <param name="maxSpeed">The maximum speed of this agent. Must be
            * non-negative.</param>
            * <param name="velocity">The initial two-dimensional linear velocity of
            * this agent.</param>
            */
        public IORCAAgent AddAgent<T>(
            Vector2 position, 
            float neighborDist, 
            int maxNeighbors, 
            float timeHorizon, 
            float timeHorizonObst, 
            float radius, 
            float maxSpeed, 
            Vector2 velocity)
            where T : ORCAAgent, IORCAAgent, new()
        {
            T agent = new T();
            agent.m_id = s_totalID;
            s_totalID++;
            agent.m_maxNeighbors = maxNeighbors;
            agent.m_maxSpeed = maxSpeed;
            agent.m_neighborDist = neighborDist;
            agent.m_position = position;
            agent.m_radius = radius;
            agent.m_timeHorizon = timeHorizon;
            agent.m_timeHorizonObst = timeHorizonObst;
            agent.m_velocity = velocity;
            m_agents.Add(agent);
            OnAgentAdded();
            return agent;
        }

        #endregion

        #region Obstacles

        /// <summary>
        /// Adds a new obstacle to the simulation.
        /// </summary>
        /// <param name="vertices">List of the vertices of the polygonal obstacle
        /// in counterclockwise order.</param>
        /// <returns>The number of the first vertex of the obstacle, or -1 when
        /// the number of vertices is less than two.</returns>
        /// <remarks>
        /// To add a "negative" obstacle, e.g. a bounding polygon around
        /// the environment, the vertices should be listed in clockwise order.
        /// </remarks>
        public int AddObstacle(IList<Vector2> vertices)
        {
            if (vertices.Count < 2)
            {
                return -1;
            }

            int obstacleNo = m_obstacles.Count;

            for (int i = 0; i < vertices.Count; ++i)
            {
                Obstacle obstacle = new Obstacle();
                obstacle.point = vertices[i];

                if (i != 0)
                {
                    obstacle.prev = m_obstacles[m_obstacles.Count - 1];
                    obstacle.prev.next = obstacle;
                }

                if (i == vertices.Count - 1)
                {
                    obstacle.next = m_obstacles[obstacleNo];
                    obstacle.next.prev = obstacle;
                }

                obstacle.dir = (vertices[(i == vertices.Count - 1 ? 0 : i + 1)] - vertices[i]).normalized;

                if (vertices.Count == 2)
                {
                    obstacle.convex = true;
                }
                else
                {
                    obstacle.convex = (Maths.LeftOf(vertices[(i == 0 ? vertices.Count - 1 : i - 1)], vertices[i], vertices[(i == vertices.Count - 1 ? 0 : i + 1)]) >= 0.0f);
                }

                obstacle.id = m_obstacles.Count;
                m_obstacles.Add(obstacle);
            }

            return obstacleNo;
        }

        #endregion

        #region Simulation

        /// <summary>
        /// Clears the simulation.
        /// </summary>
        public void Clear()
        {
            m_agents.Clear();
            m_agentNo2indexDict.Clear();
            m_index2agentNoDict.Clear();
            m_defaultAgent = null;
            m_kdTree = new ORCAKdTree();
            m_kdTree.solver = this;
            m_obstacles.Clear();
            m_dynamicObstacles.Clear();

            m_globalTime = 0.0f;
            m_timeStep = 0.1f;

            SetNumWorkers(0);
        }

        /// <summary>
        /// Performs a simulation step and updates the two-dimensional
        /// position and two-dimensional velocity of each agent.
        /// </summary>
        /// <returns>The global time after the simulation step.</returns>
        public float DoStep()
        {
            UpdateDeleteAgent();

            if (m_workers == null)
            {
                m_workers = new Worker[m_numWorkers];
                m_doneEvents = new ManualResetEvent[m_workers.Length];
                m_workerAgentCount = GetNumAgents();

                for (int block = 0; block < m_workers.Length; ++block)
                {
                    m_doneEvents[block] = new ManualResetEvent(false);
                    m_workers[block] = new Worker(
                        block * GetNumAgents() / m_workers.Length, 
                        (block + 1) * GetNumAgents() / m_workers.Length, 
                        m_doneEvents[block], this);
                }
            }

            if (m_workerAgentCount != GetNumAgents())
            {
                m_workerAgentCount = GetNumAgents();
                for (int block = 0; block < m_workers.Length; ++block)
                {
                    m_workers[block].config(block * GetNumAgents() / m_workers.Length, (block + 1) * GetNumAgents() / m_workers.Length);
                }
            }

            ProcessDynamicObstacles();
            m_kdTree.BuildAgentTree();

            for (int block = 0; block < m_workers.Length; ++block)
            {
                m_doneEvents[block].Reset();
                ThreadPool.QueueUserWorkItem(m_workers[block].step);
            }

            WaitHandle.WaitAll(m_doneEvents);

            for (int block = 0; block < m_workers.Length; ++block)
            {
                m_doneEvents[block].Reset();
                ThreadPool.QueueUserWorkItem(m_workers[block].update);
            }

            WaitHandle.WaitAll(m_doneEvents);

            m_globalTime += m_timeStep;

            return m_globalTime;
        }

        #endregion

        public IORCAAgent GetAgent(int agentNo)
        {
            return m_agents[m_agentNo2indexDict[agentNo]];
        }
        
        /// <summary>
        /// Processes the obstacles that have been added so that they
        /// are accounted for in the simulation.
        /// </summary>
        /// <remarks>
        /// Obstacles added to the simulation after this function has
        /// been called are not accounted for in the simulation.
        /// </remarks>
        public void ProcessObstacles()
        {
            m_kdTree.BuildObstacleTree();
        }

        /// <summary>
        /// Processes the dynamic obstacles so that they
        /// are accounted for in the simulation.
        /// </summary>
        /// <remarks>
        /// Obstacles added to the simulation after this function has
        /// been called are not accounted for in the simulation.
        /// </remarks>
        protected void ProcessDynamicObstacles()
        {
            m_kdTree.BuildDynamicObstacleTree();
        }


        #region Utils
       

        /**
         * <summary>Returns the specified obstacle neighbor of the specified
         * agent.</summary>
         *
         * <returns>The number of the first vertex of the neighboring obstacle
         * edge.</returns>
         *
         * <param name="agentNo">The number of the agent whose obstacle neighbor
         * is to be retrieved.</param>
         * <param name="neighborNo">The number of the obstacle neighbor to be
         * retrieved.</param>
         */
        public int GetAgentObstacleNeighbor(int agentNo, int neighborNo)
        {
            return m_agents[m_agentNo2indexDict[agentNo]].m_obstacleNeighbors[neighborNo].Value.id;
        }
        
        /**
         * <summary>Returns the count of agents in the simulation.</summary>
         *
         * <returns>The count of agents in the simulation.</returns>
         */
        public int GetNumAgents()
        {
            return m_agents.Count;
        }

        /**
         * <summary>Returns the count of obstacle vertices in the simulation.
         * </summary>
         *
         * <returns>The count of obstacle vertices in the simulation.</returns>
         */
        public int GetNumObstacleVertices()
        {
            return m_obstacles.Count;
        }

        /**
         * <summary>Returns the count of workers.</summary>
         *
         * <returns>The count of workers.</returns>
         */
        public int GetNumWorkers()
        {
            return m_numWorkers;
        }

        /**
         * <summary>Returns the two-dimensional position of a specified obstacle
         * vertex.</summary>
         *
         * <returns>The two-dimensional position of the specified obstacle
         * vertex.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex to be
         * retrieved.</param>
         */
        public Vector2 GetObstacleVertex(int vertexNo)
        {
            return m_obstacles[vertexNo].point;
        }

        /**
         * <summary>Returns the number of the obstacle vertex succeeding the
         * specified obstacle vertex in its polygon.</summary>
         *
         * <returns>The number of the obstacle vertex succeeding the specified
         * obstacle vertex in its polygon.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex whose
         * successor is to be retrieved.</param>
         */
        public int GetNextObstacleVertexNo(int vertexNo)
        {
            return m_obstacles[vertexNo].next.id;
        }

        /**
         * <summary>Returns the number of the obstacle vertex preceding the
         * specified obstacle vertex in its polygon.</summary>
         *
         * <returns>The number of the obstacle vertex preceding the specified
         * obstacle vertex in its polygon.</returns>
         *
         * <param name="vertexNo">The number of the obstacle vertex whose
         * predecessor is to be retrieved.</param>
         */
        public int GetPrevObstacleVertexNo(int vertexNo)
        {
            return m_obstacles[vertexNo].prev.id;
        }

        #endregion
        
        /**
         * <summary>Performs a visibility query between the two specified points
         * with respect to the obstacles.</summary>
         *
         * <returns>A boolean specifying whether the two points are mutually
         * visible. Returns true when the obstacles have not been processed.
         * </returns>
         *
         * <param name="point1">The first point of the query.</param>
         * <param name="point2">The second point of the query.</param>
         * <param name="radius">The minimal distance between the line connecting
         * the two points and the obstacles in order for the points to be
         * mutually visible (optional). Must be non-negative.</param>
         */
        public bool QueryVisibility(Vector2 point1, Vector2 point2, float radius)
        {
            return m_kdTree.QueryVisibility(point1, point2, radius);
        }

        public IORCAAgent QueryNearAgent(Vector2 point, float radius)
        {
            if (GetNumAgents() == 0)
                return null;

            int index = m_kdTree.QueryNearAgent(point, radius);

            if (index == -1)
                return null;

            return GetAgent(index);
        }

        /**
         * <summary>Sets the default properties for any new agent that is added.
         * </summary>
         *
         * <param name="neighborDist">The default maximum distance (center point
         * to center point) to other agents a new agent takes into account in
         * the navigation. The larger this number, the longer he running time of
         * the simulation. If the number is too low, the simulation will not be
         * safe. Must be non-negative.</param>
         * <param name="maxNeighbors">The default maximum number of other agents
         * a new agent takes into account in the navigation. The larger this
         * number, the longer the running time of the simulation. If the number
         * is too low, the simulation will not be safe.</param>
         * <param name="timeHorizon">The default minimal amount of time for
         * which a new agent's velocities that are computed by the simulation
         * are safe with respect to other agents. The larger this number, the
         * sooner an agent will respond to the presence of other agents, but the
         * less freedom the agent has in choosing its velocities. Must be
         * positive.</param>
         * <param name="timeHorizonObst">The default minimal amount of time for
         * which a new agent's velocities that are computed by the simulation
         * are safe with respect to obstacles. The larger this number, the
         * sooner an agent will respond to the presence of obstacles, but the
         * less freedom the agent has in choosing its velocities. Must be
         * positive.</param>
         * <param name="radius">The default radius of a new agent. Must be
         * non-negative.</param>
         * <param name="maxSpeed">The default maximum speed of a new agent. Must
         * be non-negative.</param>
         * <param name="velocity">The default initial two-dimensional linear
         * velocity of a new agent.</param>
         */
        public void SetAgentDefaults(
            float neighborDist, 
            int maxNeighbors, 
            float timeHorizon, 
            float timeHorizonObst, 
            float radius, 
            float maxSpeed, 
            Vector2 velocity)
        {
            if (m_defaultAgent == null)
            {
                m_defaultAgent = new ORCAAgent();
            }

            m_defaultAgent.m_maxNeighbors = maxNeighbors;
            m_defaultAgent.m_maxSpeed = maxSpeed;
            m_defaultAgent.m_neighborDist = neighborDist;
            m_defaultAgent.m_radius = radius;
            m_defaultAgent.m_timeHorizon = timeHorizon;
            m_defaultAgent.m_timeHorizonObst = timeHorizonObst;
            m_defaultAgent.m_velocity = velocity;
        }
        
        /**
         * <summary>Sets the number of workers.</summary>
         *
         * <param name="numWorkers">The number of workers.</param>
         */
        public void SetNumWorkers(int numWorkers)
        {
            m_numWorkers = numWorkers;

            if (m_numWorkers <= 0)
            {
                int completionPorts;
                ThreadPool.GetMinThreads(out m_numWorkers, out completionPorts);
            }
            m_workers = null;
            m_workerAgentCount = 0;
        }

        /**
         * <summary>Constructs and initializes a simulation.</summary>
         */
        public ORCASolver()
        {
            Clear();
        }

        public static float m(Vector2 A, Vector2 B)
        {
            return A.x * B.x + A.y * B.y;
        }
    }
}
