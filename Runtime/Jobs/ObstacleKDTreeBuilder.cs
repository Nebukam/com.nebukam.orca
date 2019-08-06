using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{

    public class ObstacleKDTreeBuilder<T, P, KD> : ProcessorChain, IPlanar
        where T : class, IProcessor, IObstacleProvider
        where P : class, T, new()
        where KD : ObstacleKDTreeProcessor<T>, new()
    {

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_obstacleProvider.plane = value; }
        }

        protected P m_obstacleProvider;
        public IObstacleGroup obstacles { get { return m_obstacleProvider.obstacles; } set { m_obstacleProvider.obstacles = value; } }

        protected ObstacleOrientationProcessor<T> m_orientation;
        protected ObstacleFixProcessor<T> m_fix;
        protected KD m_kdTree;

        public ObstacleKDTreeBuilder()
        {
            Add(ref m_obstacleProvider); //Create base obstacle structure
            Add(ref m_orientation); //Compute obstacle direction & type (convex/concave)
            m_orientation.chunkSize = 64;
            Add(ref m_fix);
            Add(ref m_kdTree); //Compute & split actual KDTree
        }

        protected override void Apply()
        {
            base.Apply();
        }

    }

}
