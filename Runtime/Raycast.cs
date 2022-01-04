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

using Unity.Burst;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;

namespace Nebukam.ORCA
{

    /// <summary>
    /// Defines which type of object the raycast should include in its checks.
    /// </summary>
    [System.Flags]
    public enum RaycastFilter
    {
        NONE = 0,
        AGENTS = 1,
        OBSTACLE_STATIC = 2,
        OBSTACLE_DYNAMIC = 4,
        OBSTACLES = OBSTACLE_STATIC | OBSTACLE_DYNAMIC,
        ANY = AGENTS | OBSTACLES
    }

    /// <summary>
    /// Job-friendly representation of a Raycast.
    /// Primarily used within RaycastProvider.
    /// </summary>
    [BurstCompile]
    public struct RaycastData
    {
        public float2 position;
        public float3 worldPosition;
        public float baseline;
        public float2 direction;
        public float3 worldDir;
        public float distance;
        public ORCALayer layerIgnore;
        public RaycastFilter filter;
        public bool twoSided;
    }

    /// <summary>
    /// Result of a raycast.
    /// Primarily used within RaycastPass
    /// </summary>
    [BurstCompile]
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

    /// <summary>
    /// A Raycast object. Holds the Raycast settings as well as its results.
    /// </summary>
    public class Raycast : Vertex, IRequireCleanUp
    {

        protected internal float3 m_dir = float3(0f);
        protected internal ORCALayer m_layerIgnore = ORCALayer.NONE;
        protected internal RaycastFilter m_filter = RaycastFilter.ANY;
        protected internal float m_distance = 0f;
        protected internal bool m_anyHit = false;
        protected internal bool m_twoSided = false;

        /// <summary>
        /// The direction of the raycast
        /// </summary>
        public float3 dir { get { return m_dir; } set { m_dir = value; } }
        /// <summary>
        /// Layers to be ignored by the Raycast when resolved
        /// </summary>
        public ORCALayer layerIgnore { get { return m_layerIgnore; } set { m_layerIgnore = value; } }
        /// <summary>
        /// Filters the type of ingredients this Raycast will hit
        /// </summary>
        public RaycastFilter filter { get { return m_filter; } set { m_filter = value; } }
        /// <summary>
        /// Distance of the Raycast.
        /// This is the length/reach of the Raycast, anything beyond that distance will be ignored.
        /// </summary>
        public float distance { get { return m_distance; } set { m_distance = value; } }
        /// <summary>
        /// Whether that raycast hit anything
        /// </summary>
        public bool anyHit { get { return m_anyHit; } }
        /// <summary>
        /// Whether that raycast is two-sided.
        /// If true, will check for backface collisions, otherwise not. This is useful in situation
        /// where the Raycast origin may be within an ingredient (agent or collision).
        /// </summary>
        public bool twoSided { get { return m_twoSided; } }

        /// <summary>
        /// The Obstacle hit by the Raycast, if any
        /// </summary>
        public Obstacle obstacleHit { get; set; }
        /// <summary>
        /// The location on the Obstacle surface where the Raycast hit.
        /// </summary>
        public float3 obstacleHitLocation { get; set; }

        /// <summary>
        /// The Agent hit by the Raycast, if any
        /// </summary>
        public Agent agentHit { get; set; }
        /// <summary>
        /// The location on the Agent surface (radius-driven circle) where the Raycast hit
        /// </summary>
        public float3 agentHitLocation { get; set; }

        /// <summary>
        /// Cleanup/Reset the Raycast object when interacting with pools.
        /// </summary>
        public virtual void CleanUp()
        {
            m_dir = float3(0f);
            m_distance = 0f;
            m_layerIgnore = ORCALayer.NONE;
            m_filter = RaycastFilter.ANY;
            m_anyHit = false;
            m_twoSided = false;

            obstacleHit = null;
            agentHit = null;
        }

    }


}
