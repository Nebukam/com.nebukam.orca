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


namespace Nebukam.ORCA
{

    public class Obstacle : VertexGroup<ObstacleVertex>
    {
        
        internal ORCALayer m_layerOccupation = ORCALayer.ANY;
        internal bool m_collisionEnabled = true;
        internal float m_thickness = 0.0f;
        internal float m_height = 1.0f;
        internal float m_baseline = 0.0f;
        internal bool m_edge = false;

        public ORCALayer layerOccupation { get { return m_layerOccupation; } set { m_layerOccupation = value; } }
        public bool collisionEnabled { get { return m_collisionEnabled; } set { m_collisionEnabled = value; } }
        public float thickness { get { return m_thickness; } set { m_thickness = value; } }
        public float height { get { return m_height; } set { m_height = value; } }
        public float baseline { get { return m_baseline; } set { m_baseline = value; } }
        /// <summary>
        /// Treat obstacle as an open group of line instead of a closed polygon
        /// </summary>
        public bool edge { get { return m_edge; } set { m_edge = value; } }


        public ObstacleInfos infos {
            get{
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
    }

}
