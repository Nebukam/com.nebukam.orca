using Unity.Mathematics;

namespace Nebukam.ORCA
{
    public struct ObstacleTreeNode
    {

        public const int MAX_LEAF_SIZE = 10;

        public int index;
        public int vertex;
        public int left;
        public int right;

        public int begin;
        public int end;
        public float maxX;
        public float maxY;
        public float minX;
        public float minY;
    }
}
