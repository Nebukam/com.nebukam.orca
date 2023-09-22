// Copyright (c) 2021 Timothé Lapetite - nebukam@gmail.com
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

using Nebukam.Common;

namespace Nebukam.ORCA
{

    /// <summary>
    /// Obstacle definition within an ORCA simulation.
    /// An Obstacle can be seen as an either open or closed polyline : it is a list of linked points in space
    /// </summary>
    public class Obstacle : PolyLine<ObstacleVertex>, IRequireInit
    {

        protected internal ORCALayer m_layerOccupation = ORCALayer.ANY;
        protected internal bool m_collisionEnabled = true;
        protected internal float m_thickness = 0.0f;
        protected internal float m_height = 1.0f;
        protected internal float m_baseline = 0.0f;
        protected internal bool m_edge = false;

        /// <summary>
        /// Which layer this Obstacle occupies within the simulation.
        /// This define whether an Agent will account for this obstacle or not based on its own layerOccupation.
        /// </summary>
        public ORCALayer layerOccupation { get { return m_layerOccupation; } set { m_layerOccupation = value; } }
        /// <summary>
        /// Whether the collision is enabled or not for this Obstacle.
        /// If True, the agents will avoid it, otherwise ignore it.
        /// </summary>
        public bool collisionEnabled { get { return m_collisionEnabled; } set { m_collisionEnabled = value; } }
        /// <summary>
        /// The thickness of the obstacle' line, in both directions.
        /// </summary>
        public float thickness { get { return m_thickness; } set { m_thickness = value; } }
        /// <summary>
        /// The height of the obstacle. Used by the simulation to check whether an Agent would collide with that obstacle or not based
        /// on both the Obstacle & Agent baseline & height.
        /// </summary>
        public float height { get { return m_height; } set { m_height = value; } }
        /// <summary>
        /// The vertical position of the Obstacle (Z if in XY plane, otherwise Y)
        /// </summary>
        public float baseline { get { return m_baseline; } set { m_baseline = value; } }
        /// <summary>
        /// If true, reat obstacle as an open group of line instead of a closed polygon
        /// </summary>
        public bool edge { get { return m_edge; } set { m_edge = value; } }


        public ObstacleInfos infos
        {
            get
            {
                return new ObstacleInfos()
                {
                    length = Count,
                    layerOccupation = m_layerOccupation,
                    collisionEnabled = m_collisionEnabled,
                    edge = m_collisionEnabled,
                    thickness = m_thickness,
                    baseline = m_baseline,
                    height = m_height
                };
            }
        }

        public virtual void Init()
        {
            m_layerOccupation = ORCALayer.ANY;
            m_collisionEnabled = true;
            m_thickness = 0.0f;
            m_height = 1.0f;
            m_baseline = 0.0f;
            m_edge = false;
        }

    }

}
