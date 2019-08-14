using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;
using Nebukam.JobAssist;

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
        protected NativeArray<ObstacleInfos> m_outputObstacleInfos = new NativeArray<ObstacleInfos>(0, Allocator.Persistent);
        protected NativeArray<ObstacleVertexData> m_referenceObstacles = new NativeArray<ObstacleVertexData>(0, Allocator.Persistent);
        protected NativeArray<ObstacleVertexData> m_outputObstacles = new NativeArray<ObstacleVertexData>(0, Allocator.Persistent);


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
        protected override void InternalUnlock() { }

        protected override void Prepare(ref Unemployed job, float delta)
        {
            int obsCount = m_obstacles == null ? 0 : m_obstacles.Count,
             refCount = m_referenceObstacles.Length, vCount = 0;

            if (m_outputObstacleInfos.Length != obsCount)
            {
                m_outputObstacleInfos.Dispose();
                m_outputObstacleInfos = new NativeArray<ObstacleInfos>(obsCount, Allocator.Persistent);

                m_recompute = true;
            }

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

            if (refCount != vCount)
            {
                m_referenceObstacles.Dispose();
                m_referenceObstacles = new NativeArray<ObstacleVertexData>(vCount, Allocator.Persistent);

                m_outputObstacles.Dispose();
                m_outputObstacles = new NativeArray<ObstacleVertexData>(vCount, Allocator.Persistent);
            }

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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { return; }

            m_obstacles = null;
            m_outputObstacleInfos.Dispose();
            m_referenceObstacles.Dispose();
            m_outputObstacles.Dispose();
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
