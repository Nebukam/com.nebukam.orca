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
using System.Collections.Generic;
using Unity.Collections;
using static Unity.Mathematics.math;
using Nebukam.Common;

namespace Nebukam.ORCA
{
    public class RaycastsPass : ParallelProcessor<RaycastsJob>
    {

        public AxisPair plane { get; set; } = AxisPair.XY;

        protected NativeArray<RaycastResult> m_results = default;
        public NativeArray<RaycastResult> results { get { return m_results; } }

        #region Inputs

        protected bool m_inputsDirty = true;

        protected IRaycastProvider m_raycastProvider;
        public IRaycastProvider raycastProvider { get { return m_raycastProvider; } }

        protected IAgentProvider m_agentProvider;
        public IAgentProvider agentProvider { get { return m_agentProvider; } }

        protected IAgentKDTreeProvider m_agentKDTreeProvider;
        public IAgentKDTreeProvider agentKDTreeProvider { get { return m_agentKDTreeProvider; } }

        protected IStaticObstacleProvider m_staticObstaclesProvider;
        public IStaticObstacleProvider staticObstaclesProvider { get { return m_staticObstaclesProvider; } }

        protected IStaticObstacleKDTreeProvider m_staticObstacleKDTreeProvider;
        public IStaticObstacleKDTreeProvider staticObstacleKDTreeProvider { get { return m_staticObstacleKDTreeProvider; } }

        protected IDynObstacleProvider m_dynObstaclesProvider;
        public IDynObstacleProvider dynObstaclesProvider { get { return m_dynObstaclesProvider; } }

        protected IDynObstacleKDTreeProvider m_dynObstacleKDTreeProvider;
        public IDynObstacleKDTreeProvider dynObstacleKDTreeProvider { get { return m_dynObstacleKDTreeProvider; } }

        #endregion

        protected override int Prepare(ref RaycastsJob job, float delta)
        {

            if (m_inputsDirty)
            {

                if (!TryGetFirstInCompound(out m_raycastProvider, true)
                    || !TryGetFirstInCompound(out m_agentProvider, true)
                    || !TryGetFirstInCompound(out m_agentKDTreeProvider, true)
                    || !TryGetFirstInCompound(out m_staticObstaclesProvider, true)
                    || !TryGetFirstInCompound(out m_staticObstacleKDTreeProvider, true)
                    || !TryGetFirstInCompound(out m_dynObstaclesProvider, true)
                    || !TryGetFirstInCompound(out m_dynObstacleKDTreeProvider, true))
                {
                    string msg = string.Format("Missing provider : Raycasts = {7}, Agents = {0}, Static obs = {1}, Agent KD = {2}, Static obs KD= {3}, " +
                        "Dyn obs = {5}, Dyn obs KD= {6}, group = {4}",
                        m_agentProvider,
                        m_staticObstaclesProvider,
                        m_agentKDTreeProvider,
                        m_staticObstacleKDTreeProvider,
                        m_dynObstaclesProvider,
                        m_dynObstacleKDTreeProvider, m_compound,
                        m_raycastProvider);

                    throw new System.Exception(msg);
                }

                m_inputsDirty = false;

            }

            int rayCount = m_raycastProvider.outputRaycasts.Length;

            MakeLength(ref m_results, rayCount);

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

        protected override void InternalDispose()
        {
            m_results.Release();
        }

    }
}
