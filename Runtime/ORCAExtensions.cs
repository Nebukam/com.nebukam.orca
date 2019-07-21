using System.Collections.Generic;
using UnityEngine;

namespace Nebukam.ORCA
{
    public static class ORCAExtensions
    {
        
        /// <summary>
        /// Returns the specified agent neighbor of the specified agent.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="neighborNo">The number of the agent neighbor to be retrieved.</param>
        /// <returns></returns>
        public static IORCAAgent GetAgentNeighbor(this IORCAAgent a, int neighborNo)
        {
            //m_agents[m_agentNo2indexDict[agentNo]].m_agentNeighbors[neighborNo].Value.m_id;
            ORCAAgent typedAgent = a as ORCAAgent;
            return a.solver.GetAgent(typedAgent.m_agentNeighbors[neighborNo].Value.m_id);
        }
        
        /// <summary>
        /// Returns the count of agent neighbors taken into account to
        /// compute the current velocity for the specified agent.
        /// </summary>
        /// <param name="a"></param>
        /// <returns>The count of agent neighbors taken into account to compute
        /// the current velocity for the specified agent.</returns>
        public static int GetAgentNeighborCount(this IORCAAgent a)
        {
            //m_agents[m_agentNo2indexDict[agentNo]].m_agentNeighbors.Count;
            ORCAAgent typedAgent = a as ORCAAgent;
            return typedAgent.m_agentNeighbors.Count;
        }
        
        /// <summary>
        /// Returns the count of obstacle neighbors taken into account
        /// to compute the current velocity for the specified agent.
        /// </summary>
        /// <param name="a"></param>
        /// <returns>The count of obstacle neighbors taken into account to
        /// compute the current velocity for the specified agent.</returns>
        public static int GetObstaclesNeighborCount(this IORCAAgent a)
        {
            //m_agents[m_agentNo2indexDict[agentNo]].m_obstacleNeighbors.Count;
            ORCAAgent typedAgent = a as ORCAAgent;
            return typedAgent.m_obstacleNeighbors.Count;
        }
        
        public static IList<Line> GetOrcaLines(this IORCAAgent a)
        {
            return (a as ORCAAgent).m_orcaLines;
        }


    }
}
