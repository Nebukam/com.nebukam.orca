using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public struct ObstacleVertexData
    {
        public int infos;
        public int index;
        public int next;
        public int prev;
        public bool convex;
        public float2 pos;
        public float2 dir;
    }

}
