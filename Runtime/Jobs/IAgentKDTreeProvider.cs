using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.JobAssist;

namespace Nebukam.ORCA
{
    public interface IAgentKDTreeProvider : IProcessor
    {
        NativeArray<AgentTreeNode> outputTree { get; }
    }
}
