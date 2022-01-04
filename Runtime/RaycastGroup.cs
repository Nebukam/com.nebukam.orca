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

using Unity.Mathematics;
using Nebukam.Common;

namespace Nebukam.ORCA
{

    public interface IRaycastGroup : IVertexGroup<Raycast>
    {

    }

    public class RaycastGroup : VertexGroup<Raycast>, IRaycastGroup
    {
        /// <summary>
        /// Create a Raycast object from a given set of parameters.
        /// It will hold its own results when the simulation is applied.
        /// Note The returned Raycast object has additional properties beyond the one exposed in this method.
        /// </summary>
        /// <param name="origin">Origin point of the raycast</param>
        /// <param name="dir">Direction of the raycast</param>
        /// <param name="distance">Distance of the raycast</param>
        /// <param name="filter"></param>
        /// <returns>The newly created Raycast.</returns>
        public Raycast Add(float3 origin, float3 dir, float distance, RaycastFilter filter = RaycastFilter.ANY)
        {
            Raycast v = Add(origin);
            v.m_dir = dir;
            v.m_distance = distance;
            v.m_filter = filter;
            return v;
        }

        protected override void OnVertexAdded(Raycast v)
        {
            base.OnVertexAdded(v);
            v.onRelease(m_onVertexReleasedCached);
        }

        protected override void OnVertexRemoved(Raycast v)
        {
            base.OnVertexRemoved(v);
            v.offRelease(m_onVertexReleasedCached);
        }
    }
}
