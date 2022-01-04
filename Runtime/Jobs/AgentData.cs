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

namespace Nebukam.ORCA
{

    /// <summary>
    /// Job-friendly Agent data.
    /// </summary>
    [BurstCompile]
    public struct AgentData
    {
        public int index;
        public int kdIndex;

        public float2 position;
        public float baseline;
        public float2 prefVelocity;
        public float2 velocity;

        public float height;
        public float radius;
        public float radiusObst;
        public float maxSpeed;

        public int maxNeighbors;
        public float neighborDist;
        public float neighborElev;

        public float timeHorizon;
        public float timeHorizonObst;

        public ORCALayer layerOccupation;
        public ORCALayer layerIgnore;
        public bool navigationEnabled;
        public bool collisionEnabled;

        public float3 worldPosition;
        public float3 worldVelocity;
    }

    /// <summary>
    /// Agent result data after a simulation step.
    /// </summary>
    [BurstCompile]
    public struct AgentDataResult
    {

        public float2 position;
        public float2 velocity;

    }

}
