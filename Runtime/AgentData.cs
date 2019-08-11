using Unity.Mathematics;

namespace Nebukam.ORCA
{

    public struct AgentData
    {
        public int index;
        public int kdIndex;

        public float2 position;
        public float2 prefVelocity;
        public float2 velocity;
        
        public float radius;
        public float radiusObst;
        public float maxSpeed;

        public int maxNeighbors;
        public float neighborDist;

        public float timeHorizon;
        public float timeHorizonObst;

        public ORCALayer layerOccupation;
        public ORCALayer layerIgnore;
        public bool navigationEnabled;
        public bool collisionEnabled;
    }

    public struct AgentDataResult
    {

        public float2 position;
        public float2 velocity;

    }

}
