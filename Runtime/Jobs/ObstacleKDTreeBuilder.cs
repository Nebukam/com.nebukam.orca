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
using Nebukam.Common;

namespace Nebukam.ORCA
{

    public class ObstacleKDTreeBuilder<T, P, KD> : ProcessorChain, IPlanar
        where T : class, IProcessor, IObstacleProvider
        where P : class, T, new()
        where KD : ObstacleKDTree<T>, new()
    {

        #region IPlanar

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_obstacleProvider.plane = value; }
        }

        #endregion

        protected P m_obstacleProvider;
        public IObstacleGroup obstacles { 
            get { return m_obstacleProvider.obstacles; } 
            set { m_obstacleProvider.obstacles = value; } 
        }

        protected ObstacleOrientationPass<T> m_orientation;
        protected ObstacleFix<T> m_fix;
        protected KD m_kdTree;

        public ObstacleKDTreeBuilder()
        {
            Add(ref m_obstacleProvider); //Create base obstacle structure
            Add(ref m_orientation); //Compute obstacle direction & type (convex/concave)
            m_orientation.chunkSize = 64;

            Add(ref m_fix);
            Add(ref m_kdTree); //Compute & split actual KDTree
        }

    }

}
