using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{

    public class ObstacleKDTreeBuilder : ProcessorChain, IPlanar
    {

        protected AxisPair m_plane = AxisPair.XY;
        public AxisPair plane
        {
            get { return m_plane; }
            set { m_plane = m_obstacleProvider.plane = value; }
        }

        protected ObstacleProvider m_obstacleProvider;
        public IObstacleGroup obstacles { get { return m_obstacleProvider.obstacles; } set { m_obstacleProvider.obstacles = value; } }

        protected ObstacleOrientationProcessor m_orientation;
        protected ObstacleFixProcessor m_fix;
        protected ObstacleKDTreeProcessor m_kdTree;

        public ObstacleKDTreeBuilder()
        {

            //TODO : Only actually process chain if required (i.e once, or when modified)
            m_obstacleProvider = Add(new ObstacleProvider()); //Create base obstacle structure
            m_orientation = Add(new ObstacleOrientationProcessor()); //Compute obstacle direction & type (convex/concave)
            m_orientation.chunkSize = 64;

            m_fix = Add(new ObstacleFixProcessor());
            m_kdTree = Add(new ObstacleKDTreeProcessor()); //Compute & split actual KDTree

        }

        protected override void Apply()
        {
            base.Apply();
        }

    }

}
