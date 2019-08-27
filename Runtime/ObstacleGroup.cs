// Copyright (c) 2019 Timothé Lapetite - nebukam@gmail.com
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Nebukam.Pooling;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Nebukam.ORCA
{

    public interface IObstacleGroup
    {
        int Count { get; }
        Obstacle this[int i] { get; }
    }

    public class ObstacleGroup : IObstacleGroup
    {

        protected Pooling.Pool.OnItemReleased m_onObstacleReleased;

        public ObstacleGroup()
        {
            m_onObstacleReleased = OnObstacleReleased;
        }

        //Store obsctacles
        protected List<Obstacle> m_obstacles = new List<Obstacle>();

        public int Count { get { return m_obstacles.Count; } }
        public Obstacle this[int i] { get { return m_obstacles[i]; } }

        public Obstacle Add(Obstacle obstacle)
        {
            if (m_obstacles.Contains(obstacle)) { return obstacle; }
            m_obstacles.Add(obstacle);
            obstacle.onRelease(m_onObstacleReleased);
            return obstacle;
        }

        public Obstacle Add(IList<float3> m_vertices, bool inverseOrder = false)
        {
            Obstacle obstacle = Pool.Rent<Obstacle>();

            int count = m_vertices.Count;
            if (!inverseOrder)
            {
                for (int i = 0; i < count; i++)
                    obstacle.Add(m_vertices[i]);
            }
            else
            {
                for (int i = count - 1; i >= 0; i--)
                    obstacle.Add(m_vertices[i]);
            }

            return Add(obstacle);
        }

        public void Remove(Obstacle obstacle)
        {
            m_obstacles.Remove(obstacle);
        }

        protected void OnObstacleReleased(IPoolItem obstacle)
        {
            Remove(obstacle as Obstacle);
        }

        public void Clear(bool release = false)
        {
            if (release)
            {
                int count = m_obstacles.Count;
                while (count != 0)
                {
                    m_obstacles[count - 1].Release();
                    count = m_obstacles.Count;
                }
            }

            m_obstacles.Clear();
        }

    }
}
