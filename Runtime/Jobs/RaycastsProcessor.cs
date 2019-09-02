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
using System.Collections.Generic;
using Unity.Collections;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public class RaycastsProcessor : ParallelProcessor<RaycastsJob>
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        /// 
        /// Fields
        /// 

        protected IRaycastProvider m_raycastProvider;
        protected IAgentProvider m_agentProvider;
        protected IAgentKDTreeProvider m_agentKDTreeProvider;
        protected IStaticObstacleProvider m_staticObstaclesProvider;
        protected IStaticObstacleKDTreeProvider m_staticObstacleKDTreeProvider;
        protected IDynObstacleProvider m_dynObstaclesProvider;
        protected IDynObstacleKDTreeProvider m_dynObstacleKDTreeProvider;

        protected NativeArray<RaycastResult> m_results = new NativeArray<RaycastResult>(0, Allocator.Persistent);

        /// 
        /// Properties
        /// 

        public IRaycastProvider raycastProvider { get { return m_raycastProvider; } }
        public IAgentProvider agentProvider { get { return m_agentProvider; } }
        public IAgentKDTreeProvider agentKDTreeProvider { get { return m_agentKDTreeProvider; } }
        public IStaticObstacleProvider staticObstaclesProvider { get { return m_staticObstaclesProvider; } }
        public IStaticObstacleKDTreeProvider staticObstacleKDTreeProvider { get { return m_staticObstacleKDTreeProvider; } }
        public IDynObstacleProvider dynObstaclesProvider { get { return m_dynObstaclesProvider; } }
        public IDynObstacleKDTreeProvider dynObstacleKDTreeProvider { get { return m_dynObstacleKDTreeProvider; } }

        public NativeArray<RaycastResult> results { get { return m_results; } }

        protected override void InternalLock() { }
        protected override void InternalUnlock() { }

        protected override int Prepare(ref RaycastsJob job, float delta)
        {

            if (!TryGetFirstInGroup(out m_raycastProvider, true)
                || !TryGetFirstInGroup(out m_agentProvider, true)
                || !TryGetFirstInGroup(out m_agentKDTreeProvider, true)
                || !TryGetFirstInGroup(out m_staticObstaclesProvider, true)
                || !TryGetFirstInGroup(out m_staticObstacleKDTreeProvider, true)
                || !TryGetFirstInGroup(out m_dynObstaclesProvider, true)
                || !TryGetFirstInGroup(out m_dynObstacleKDTreeProvider, true))
            {
                string msg = string.Format("Missing provider : Raycasts = {7}, Agents = {0}, Static obs = {1}, Agent KD = {2}, Static obs KD= {3}, " +
                    "Dyn obs = {5}, Dyn obs KD= {6}, group = {4}",
                    m_agentProvider,
                    m_staticObstaclesProvider,
                    m_agentKDTreeProvider,
                    m_staticObstacleKDTreeProvider,
                    m_dynObstaclesProvider,
                    m_dynObstacleKDTreeProvider, m_group,
                    m_raycastProvider);

                throw new System.Exception(msg);
            }

            int rayCount = m_raycastProvider.outputRaycasts.Length;
            if (m_results.Length != rayCount)
            {
                m_results.Dispose();
                m_results = new NativeArray<RaycastResult>(rayCount, Allocator.Persistent);
            }

            job.m_plane = plane;

            //Agent data
            job.m_inputAgents = m_agentProvider.outputAgents;
            job.m_maxAgentRadius = m_agentProvider.maxRadius;
            job.m_inputAgentTree = m_agentKDTreeProvider.outputTree;

            //Static obstacles data
            job.m_staticObstacleInfos = m_staticObstaclesProvider.outputObstacleInfos;
            job.m_staticRefObstacles = m_staticObstaclesProvider.referenceObstacles;
            job.m_staticObstacles = m_staticObstaclesProvider.outputObstacles;
            job.m_staticObstacleTree = m_staticObstacleKDTreeProvider.outputTree;

            //Dynamic obstacles data
            job.m_dynObstacleInfos = m_dynObstaclesProvider.outputObstacleInfos;
            job.m_dynRefObstacles = m_dynObstaclesProvider.referenceObstacles;
            job.m_dynObstacles = m_dynObstaclesProvider.outputObstacles;
            job.m_dynObstacleTree = m_dynObstacleKDTreeProvider.outputTree;

            job.m_inputRaycasts = m_raycastProvider.outputRaycasts;
            job.m_results = m_results;

            return rayCount;

        }

        protected override void Apply(ref RaycastsJob job)
        {
            //Update raycast data object.
            List<Raycast> lockedRaycasts = m_raycastProvider.lockedRaycasts;
            Raycast ray;

            RaycastData rayData;
            RaycastResult rayResult;

            Obstacle o;

            int index = -1;

            for (int i = 0, rayCount = lockedRaycasts.Count; i < rayCount; i++)
            {
                ray = lockedRaycasts[i];
                ray.m_anyHit = false;

                rayData = job.m_inputRaycasts[i];
                rayResult = job.m_results[i];

                //Agent hit
                index = rayResult.hitAgent;
                if (index != -1)
                {

                    ray.agentHit = m_agentProvider.lockedAgents[index];
                    ray.agentHitLocation = rayResult.hitAgentLocation;
                    ray.m_anyHit = true;

                }
                else{ ray.agentHit = null; }

                //Obstacle hit
                index = rayResult.hitObstacle;
                if(index != -1)
                {

                    if (!rayResult.dynamicObstacle)
                        o = m_staticObstaclesProvider.obstacles[job.m_staticObstacleInfos[index].index];
                    else
                        o = m_dynObstaclesProvider.obstacles[job.m_dynObstacleInfos[index].index];

                    ray.obstacleHit = o;
                    ray.obstacleHitLocation = rayResult.hitObstacleLocation;
                    ray.m_anyHit = true;

                }
                else{ ray.obstacleHit = null; }

            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) { return; }

            m_results.Dispose();

        }

    }
}
