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
using Unity.Burst;

namespace Nebukam.ORCA
{

    [BurstCompile]
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

    public interface IObstacleKDTreeProvider : IProcessor
    {
        NativeArray<ObstacleTreeNode> outputTree { get; }
    }

    public interface IDynObstacleKDTreeProvider : IObstacleKDTreeProvider { }
    public interface IStaticObstacleKDTreeProvider : IObstacleKDTreeProvider { }

    public class ObstacleKDTree<T> : Processor<ObstacleKDTreeJob>, IObstacleKDTreeProvider
        where T : class, IProcessor, IObstacleProvider
    {

        protected NativeArray<ObstacleTreeNode> m_outputTree = default;
        public NativeArray<ObstacleTreeNode> outputTree { get { return m_outputTree; } }

        #region Inputs

        protected bool m_inputsDirty = true;

        protected T m_obstaclesProvider;

        #endregion

        protected override void Prepare(ref ObstacleKDTreeJob job, float delta)
        {

            if (m_inputsDirty)
            {

                if (!TryGetFirstInCompound(out m_obstaclesProvider))
                {
                    throw new System.Exception("IObstacleProvider missing.");
                }

                m_inputsDirty = false;

            }

            if (m_obstaclesProvider.recompute)
            {
                job.m_recompute = true;

                int obsCount = 2 * m_obstaclesProvider.referenceObstacles.Length;

                MakeLength(ref m_outputTree, obsCount);
            }
            else
            {
                job.m_recompute = false;
            }

            job.m_inputObstacleInfos = m_obstaclesProvider.outputObstacleInfos;
            job.m_referenceObstacles = m_obstaclesProvider.referenceObstacles;
            job.m_inputObstacles = m_obstaclesProvider.outputObstacles;
            job.m_outputTree = m_outputTree;


        }

        protected override void InternalDispose()
        {
            m_outputTree.Release();
        }

    }

    public class DynObstacleKDTreeProcessor : ObstacleKDTree<IDynObstacleProvider>, IDynObstacleKDTreeProvider { }
    public class StaticObstacleKDTreeProcessor : ObstacleKDTree<IStaticObstacleProvider>, IStaticObstacleKDTreeProvider { }

}
