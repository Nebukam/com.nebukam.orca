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

using Nebukam.JobAssist;
using Unity.Collections;

namespace Nebukam.ORCA
{

    public interface IObstacleKDTreeProvider : IProcessor
    {
        NativeArray<ObstacleTreeNode> outputTree { get; }
    }

    public interface IDynObstacleKDTreeProvider : IObstacleKDTreeProvider { }
    public interface IStaticObstacleKDTreeProvider : IObstacleKDTreeProvider { }

    public class ObstacleKDTreeProcessor<T> : Processor<ObstacleKDTreeJob>, IObstacleKDTreeProvider
        where T : class, IProcessor, IObstacleProvider
    {

        /// 
        /// Fields
        /// 

        protected T m_obstaclesProvider;
        protected NativeArray<ObstacleTreeNode> m_outputTree = new NativeArray<ObstacleTreeNode>(0, Allocator.Persistent);
        public NativeArray<ObstacleTreeNode> outputTree { get { return m_outputTree; } }

        /// 
        /// Properties
        /// 

        public T obstaclesProvider { get { return m_obstaclesProvider; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override void Prepare(ref ObstacleKDTreeJob job, float delta)
        {

            if (!TryGetFirstInGroup(out m_obstaclesProvider))
            {
                throw new System.Exception("No IObstacleSplitProvider or IObstacleProvider in chain !");
            }

            if (m_obstaclesProvider.recompute)
            {
                job.m_recompute = true;

                int obsCount = 2 * m_obstaclesProvider.referenceObstacles.Length;
                if (m_outputTree.Length != obsCount)
                {
                    m_outputTree.Dispose();
                    m_outputTree = new NativeArray<ObstacleTreeNode>(obsCount, Allocator.Persistent);
                }

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

        protected override void Apply(ref ObstacleKDTreeJob job)
        {

        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { return; }

            m_outputTree.Dispose();
        }

    }

    public class DynObstacleKDTreeProcessor : ObstacleKDTreeProcessor<IDynObstacleProvider>, IDynObstacleKDTreeProvider { }
    public class StaticObstacleKDTreeProcessor : ObstacleKDTreeProcessor<IStaticObstacleProvider>, IStaticObstacleKDTreeProvider { }

}
