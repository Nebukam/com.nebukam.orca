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

using Nebukam.JobAssist;
using static Nebukam.JobAssist.Extensions;
using Unity.Collections;
using Nebukam.Common;

namespace Nebukam.ORCA
{

    public interface IObstacleProvider : IProcessor, IPlanar
    {
        IObstacleGroup obstacles { get; set; }

        bool recompute { get; } //Allows KDTree builders to rebuild or skip rebuild
        NativeArray<ObstacleInfos> outputObstacleInfos { get; }
        NativeArray<ObstacleVertexData> referenceObstacles { get; }
        NativeArray<ObstacleVertexData> outputObstacles { get; }
    }

    public interface IDynObstacleProvider : IObstacleProvider { }
    public interface IStaticObstacleProvider : IObstacleProvider { }

    public class ObstacleProvider : Processor<Unemployed>, IObstacleProvider
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        /// 
        /// Fields
        ///

        protected bool m_recompute = true;
        protected IObstacleGroup m_obstacles = null;
        protected NativeArray<ObstacleInfos> m_outputObstacleInfos = default;
        protected NativeArray<ObstacleVertexData> m_referenceObstacles = default;
        protected NativeArray<ObstacleVertexData> m_outputObstacles = default;


        /// 
        /// Properties
        ///

        public bool recompute { get { return m_recompute; } set { m_recompute = true; } }
        public IObstacleGroup obstacles
        {
            get { return m_obstacles; }
            set { m_obstacles = value; m_recompute = true; }
        }
        public NativeArray<ObstacleInfos> outputObstacleInfos { get { return m_outputObstacleInfos; } }
        public NativeArray<ObstacleVertexData> referenceObstacles { get { return m_referenceObstacles; } }
        public NativeArray<ObstacleVertexData> outputObstacles { get { return m_outputObstacles; } }

        protected override void InternalLock() { }
        
        protected override void Prepare(ref Unemployed job, float delta)
        {
            int obsCount = m_obstacles == null ? 0 : m_obstacles.Count,
             refCount = m_referenceObstacles.Length, vCount = 0;

            m_recompute = !MakeLength(ref m_outputObstacleInfos, obsCount);

            Obstacle o;
            ObstacleInfos infos;

            for (int i = 0; i < obsCount; i++)
            {
                o = m_obstacles[i];
                //Keep collision infos & ORCALayer up-to-date
                //there is no need to recompute anything else.
                infos = o.infos;
                infos.index = i;
                infos.start = vCount;
                m_outputObstacleInfos[i] = infos;

                vCount += infos.length;
            }

            if (!m_recompute)
            {
                if (refCount != vCount)
                {
                    m_recompute = true;
                }
                else
                {
                    return;
                }
            }

            MakeLength(ref m_referenceObstacles, vCount);
            MakeLength(ref m_outputObstacles, vCount);

            ObstacleVertexData oData;
            int gIndex = 0, index = 0, vCountMinusOne, firstIndex, lastIndex;

            if (plane == AxisPair.XY)
            {
                for (int i = 0; i < obsCount; i++)
                {
                    o = m_obstacles[i];

                    vCount = o.Count;
                    vCountMinusOne = vCount - 1;
                    firstIndex = gIndex;
                    lastIndex = gIndex + vCountMinusOne;

                    if (!o.edge)
                    {
                        //Obstacle is a closed polygon
                        for (int v = 0; v < vCount; v++)
                        {
                            oData = new ObstacleVertexData()
                            {
                                infos = i,
                                index = index,
                                pos = o[v].XY,
                                prev = v == 0 ? lastIndex : index - 1,
                                next = v == vCountMinusOne ? firstIndex : index + 1
                            };
                            m_referenceObstacles[index++] = oData;
                        }
                    }
                    else
                    {
                        //Obstacle is an open path
                        for (int v = 0; v < vCount; v++)
                        {
                            oData = new ObstacleVertexData()
                            {
                                infos = i,
                                index = index,
                                pos = o[v].XY,
                                prev = v == 0 ? index : index - 1,
                                next = v == vCountMinusOne ? index : index + 1
                            };
                            m_referenceObstacles[index++] = oData;
                        }

                    }

                    gIndex += vCount;
                }
            }
            else
            {
                for (int i = 0; i < obsCount; i++)
                {
                    o = m_obstacles[i];
                    vCount = o.Count;
                    vCountMinusOne = vCount - 1;
                    firstIndex = gIndex;
                    lastIndex = gIndex + vCountMinusOne;

                    if (!o.edge)
                    {
                        //Obstacle is a closed polygon
                        for (int v = 0; v < vCount; v++)
                        {
                            oData = new ObstacleVertexData()
                            {
                                infos = i,
                                index = index,
                                pos = o[v].XZ,
                                prev = v == 0 ? lastIndex : index - 1,
                                next = v == vCountMinusOne ? firstIndex : index + 1
                            };
                            m_referenceObstacles[index++] = oData;
                        }
                    }
                    else
                    {
                        //Obstacle is an open path
                        for (int v = 0; v < vCount; v++)
                        {
                            oData = new ObstacleVertexData()
                            {
                                infos = i,
                                index = index,
                                pos = o[v].XZ,
                                prev = v == 0 ? index : index - 1,
                                next = v == vCountMinusOne ? index : index + 1
                            };
                            m_referenceObstacles[index++] = oData;
                        }

                    }

                    gIndex += vCount;
                }
            }

            m_referenceObstacles.CopyTo(m_outputObstacles);


        }

        protected override void Apply(ref Unemployed job)
        {
            m_recompute = false;
        }

        protected override void InternalDispose()
        {
            m_obstacles = null;
            m_outputObstacleInfos.Release();
            m_referenceObstacles.Release();
            m_outputObstacles.Release();
        }

    }

    public class StaticObstacleProvider : ObstacleProvider, IStaticObstacleProvider { }
    public class DynObstacleProvider : ObstacleProvider, IDynObstacleProvider
    {
        protected override void Prepare(ref Unemployed job, float delta)
        {
            m_recompute = true; //force always recompute 
            base.Prepare(ref job, delta);
        }
    }


}
