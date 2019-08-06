using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;

namespace Nebukam.ORCA
{ 

    public class Obstacle : VertexGroup<ObstacleVertex>
    {
        
        protected ORCALayer m_layerOccupation = ORCALayer.ALL;
        protected bool m_collisionEnabled = true;
        protected bool m_convex;
        
        public ObstacleInfos infos {
            get{
                return new ObstacleInfos()
                {
                    layerOccupation = m_layerOccupation,
                    collisionEnabled = m_collisionEnabled,
                    length = Count
                };
            }
        }
    }

}
