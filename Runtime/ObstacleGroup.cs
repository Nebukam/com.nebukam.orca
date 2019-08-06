using System;
using System.Collections.Generic;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    public interface IObstacleGroup
    {
        int Count { get; }
        Obstacle this[int i] { get; }
    }


    public class ObstacleGroup : IObstacleGroup
    {
        //Store obsctacles
        protected List<Obstacle> m_obstacles = new List<Obstacle>();

        public int Count { get { return m_obstacles.Count; } }
        public Obstacle this[int i] { get { return m_obstacles[i]; } }

        public Obstacle Add( Obstacle obstacle )
        {
            if (m_obstacles.Contains(obstacle)) { return obstacle; }
            m_obstacles.Add(obstacle);
            return obstacle;
        }

        public Obstacle Add(IList<float3> m_vertices, bool inverseOrder = false)
        {
            Obstacle obstacle = new Obstacle();

            int count = m_vertices.Count;
            if (!inverseOrder)
            {
                for (int i = 0; i < count; i++)
                    obstacle.Add(m_vertices[i]);
            }
            else
            {
                for (int i = count-1; i >= 0; i--)
                    obstacle.Add(m_vertices[i]);
            }
            
            return Add(obstacle);
        }

    }
}
