![preview](https://img.shields.io/badge/-preview-red.svg)
![version 0.0.1](https://img.shields.io/badge/version-0.2.0-blue.svg)
![in development](https://img.shields.io/badge/status-in%20development-blue.svg)

# N:ORCA
#### Optimal Reciprocal Collision Avoidance for Unity

N:ORCA is a Library to add _local collision avoidance_ to your Unity projects. It allows you to create simulations in which you register agents that move toward a goal and avoid each other smoothly without the need for any physic.

> **NOTE:**  This package only include the core code of the simulation.
Check [**N:ORCA.Components**](https://github.com/Nebukam/com.nebukam.orca-components.git) for a less 'dry' implementation.

### Features
N:ORCA is currently in development, and while the repo is available to use and download, there is no documentation yet. It supports moving agents, static agents (dynamic, circle-shaped obstacles), as well as polygonal obstacles. **Simulation is implemented using Unity's Job System & Burst**, meaning it's relatively fast. 
The simulation occurs on a planar coordinate system (by default XY), though it can be used for 3D projects (XZ) as well.

Notable features :
- Multithreaded
- Include & exclude collision using a layer system
- Dynamic & Static obstacles 

### High level concept
The basic principles are similar to classic physics approach, where entities are registered to a simulation as either static (obstacles) or dynamic (agents). The simulation is updated each frame with a specified timestep, and dynamic agents are updated accordingly.

Be aware this is not a pathfinding solution on its own, and will yield best results when combined with either A* Pathfinding or Vector Fields.

---
## Hows

### Installation
To be used with Unity's Package Manager.

See [Unity's Package Manager : Getting Started](https://docs.unity3d.com/Manual/upm-parts.html)

### Scene setup
#### Sample: Setup
The package include a Setup sample showing how to use the library. Note that it does not use models, and instead draw debug visualization in the Editor window.

#### Infos

- Favor limited amount of obstacles with lots of segments over large amount of obstacles with few segments.
- Avoid concave obstacles with acute angles. (Concave obstacles are OK otherwise)
- Intersecting & crossing segments in a same obstacle will cause undesired behaviors. (Overlapping different obstacles is OK.)
- **A dynamic obstacle is considered 'dynamic' because it lives in a stack that is recomputed _each cycle_**, compared to 'static' ones (e.g. undestructible, unmovable) which are computed at the beginning of the simulation only (as well as when new obstacles are added to the obstacle group). Although static obstacles can be recomputed at will, not doing so is much more efficient. As such, dynamic obstacles are not _physical_ obstacles. Moving such obstacles may cause inter-penetration with agents. If you are looking for "push" behavior, use an Agent with ```navigationEnabled = false;``` instead.

#### Rough overview of job chain using [**N:JobAssist**](https://github.com/Nebukam/com.nebukam.job-assist.git)

![Imgur](https://i.imgur.com/aG6MvCY.png)

---
## Dependencies
- **Unity.Burst 1.1.2** [com.unity.burst](),
- **Unity.Jobs 0.0.7** [com.unity.jobs](),
- **Unity.Collections 0.0.9** [com.unity.collections](),
- **Unity.Mathematics 1.1.0** -- [com.unity.mathematics](https://github.com/Unity-Technologies/Unity.Mathematics)
- **N:Common** -- [com.nebukam.common](https://github.com/Nebukam/com.nebukam.common.git)
- **N:Utils** -- [com.nebukam.utils](https://github.com/Nebukam/com.nebukam.utils.git)
- **N:JobAssist** -- [com.nebukam.job-assist](https://github.com/Nebukam/com.nebukam.job-assist.git)



---
## Credits

While the core calculations & equations are heavily tweaked, they are based on the famous [RVO2 Library](http://gamma.cs.unc.edu/RVO2/).
