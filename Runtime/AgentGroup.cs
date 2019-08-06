using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Nebukam.Common;

namespace Nebukam.ORCA
{

    public interface IAgentGroup : IVertexGroup
    {
        
    }

    public class AgentGroup<V> : VertexGroup<V>, IAgentGroup
        where V : Agent, IAgent, new()
    {
        
    }
}
