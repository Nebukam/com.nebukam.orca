![preview](https://img.shields.io/badge/-preview-red.svg)
![version 0.0.1](https://img.shields.io/badge/version-0.0.1-blue.svg)
![in development](https://img.shields.io/badge/status-in%20development-blue.svg)

# N:ORCA
#### Optimal Reciprocal Collision Avoidance for Unity

N:ORCA is a Library to add _local collision avoidance_ in Unity. It allows you to create simulations in which you register agents that move toward a goal and avoid each other smoothly without the need for any physic colliders.

### Features
N:ORCA is currently in development, and while the repo is available to use and download, there is no documentation yet. It supports moving agents, static agents (dynamic, circle-shaped obstacles), as well as edges & polygon (concave or convex) obstacles. Simulation are threaded, and you may run any number of simulation in parallel.
The simulation occurs on the XY (2D) plane, though results can be used for 3D projects as well.

Upcoming features include : 
- Collision check matrix
- Dynamic obstacle support (complex polygons)
- Converting Unity Polygon Collider 2D into obstacles.


### High level concept
The basic principles are similar to classic physics approach, where entities are registered to a simulation as either static (obstacles) or dynamic (agents). The simulation is updated each frame with a specified timestep, and dynamic agents are updated accordingly.

---
## Hows

### Installation
To be used with Unity's Package Manager.

See [Unity's Package Manager : Getting Started](https://docs.unity3d.com/Manual/upm-parts.html)

### Scene setup
#### Basics
In order to run a simulation, you need to create an _ORCASolver_ object. That _ORCASolver_ will then be used to create & register _ORCAAgent_. 
These _ORCAAgent_ can be assigned to a GameObject's _ORCABehaviour_ to control its position & heading as the simulation gets updated.

Each _ORCASolver_ needs to be manually updated (along with its timestep) each frame.


---
## Dependencies
- **Unity.Mathematics 1.1.0** -- [com.unity.mathematics](https://github.com/Unity-Technologies/Unity.Mathematics)
- **N:Utils** -- [com.nebukam.utils](https://github.com/Nebukam/com.nebukam.utils)

---
## Credits

The core calculations are ported from the [RVO2 Library](http://gamma.cs.unc.edu/RVO2/) : _Copyright © and trademark ™ 2008-16 University of North Carolina at Chapel Hill._
