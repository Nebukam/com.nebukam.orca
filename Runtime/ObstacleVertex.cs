using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public class ObstacleVertex : Vertex
    {

        public float3 m_dir = float3(false);
        public float3 dir { get { return m_dir; } set { m_dir = value; } }

        public float2 dirXY { get { return float2(m_dir.x, m_dir.y); } }
        public float2 dirXZ { get { return float2(m_dir.x, m_dir.z); } }
        
    }
}
