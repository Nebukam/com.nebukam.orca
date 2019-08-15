using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;

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
