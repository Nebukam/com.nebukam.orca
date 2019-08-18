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

using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{

    public struct RaycastData
    {
        public float2 position;
        public float3 worldPosition;
        public float baseline;
        public float2 direction;
        public float3 worldDir;
        public float distance;
        public ORCALayer layerIgnore;
        public bool twoSided;
    }

    public struct RaycastResult
    {
        public int hitAgent;
        public float3 hitAgentLocation;
        public float2 hitAgentLocation2D;

        public bool dynamicObstacle;
        public int hitObstacle;
        public float3 hitObstacleLocation;
        public float2 hitObstacleLocation2D;
        public int ObstacleVertexA;
        public int ObstacleVertexB;

    }
    
    public class Raycast : Vertex, Pooling.IRequireCleanUp
    {

        internal float3 m_dir = float3(false);
        internal ORCALayer m_layerIgnore = ORCALayer.NONE;
        internal float m_distance = 0f;
        internal bool m_anyHit = false;
        internal bool m_twoSided = false;

        public float3 dir { get { return m_dir; } set { m_dir = value; } }
        public ORCALayer layerIgnore { get { return m_layerIgnore; } set { m_layerIgnore = value; } }
        public float distance { get { return m_distance; } set { m_distance = value; } }
        public bool anyHit { get { return m_anyHit; } }
        public bool twoSided { get { return m_twoSided; } }

        public Obstacle obstacleHit { get; set; }
        public float3 obstacleHitLocation { get; set; }

        public Agent agentHit { get; set; }
        public float3 agentHitLocation { get; set; }

        public virtual void CleanUp()
        {
            m_dir = float3(false);
            m_distance = 0f;
            m_layerIgnore = ORCALayer.NONE;
            m_anyHit = false;
            m_twoSided = false;

            obstacleHit = null;
            agentHit = null;
        }

    }


}
