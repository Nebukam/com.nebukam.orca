using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public struct AgentTreeNode
    {

        public const int MAX_LEAF_SIZE = 10;

        public int begin;
        public int end;
        public int left;
        public int right;
        public float maxX;
        public float maxY;
        public float minX;
        public float minY;
    }
}
