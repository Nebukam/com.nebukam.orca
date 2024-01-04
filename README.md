![preview](https://img.shields.io/badge/-stable-darkgreen.svg)
![version](https://img.shields.io/badge/dynamic/json?color=blue&label=version&query=version&url=https%3A%2F%2Fraw.githubusercontent.com%2FNebukam%2Fcom.nebukam.orca%2Fmaster%2Fpackage.json)
![in development](https://img.shields.io/badge/license-MIT-black.svg)
[![doc](https://img.shields.io/badge/documentation-darkgreen.svg)](https://nebukam.github.io/docs/unity/com.nebukam.orca/)
![in development](https://img.shields.io/badge/Unity-2023.2.3f1-black.svg)

# N:ORCA
#### Optimal Reciprocal Collision Avoidance for Unity 

N:ORCA is a Library to add _local collision avoidance_ to your Unity projects. It allows you to create simulations in which you register agents that move toward a goal and avoid each other smoothly without the need for any physic.

> **NOTE:**  This package only include the core code of the simulation.
Check [**N:Beacon.ORCA**](https://github.com/Nebukam/com.nebukam.beacon-orca.git) for a user-friendly, out-of-the-box implementation.

### Features
N:ORCA is currently in development, and while the repo is available to use and download, there is no documentation yet. It supports moving agents, static agents (dynamic, circle-shaped obstacles), as well as polygonal obstacles. **Simulation is implemented using Unity's Job System & Burst**, meaning it's relatively fast.
The simulation occurs on a planar coordinate system (by default XY), though it can be used for 3D projects (XZ) as well.

Notable features :
- Multithreaded
- 2D & 3D friendly (XY or XZ simulation planes)
- Include & exclude collision using a layer system
- Height & vertical sorting/exclusion (XY:Z or XZ:Y)
- Dynamic & Static obstacles 
- Raycasting

### High level concept
The basic principles are similar to classic physics approach, where entities are registered to a simulation as either static (obstacles) or dynamic (agents). The simulation is updated each frame with a specified timestep, and dynamic agents are updated accordingly.

Be aware this is not a pathfinding solution on its own, and will yield best results when combined with either A* Pathfinding or Vector Fields.

---
## Hows

- Check out this git' [Wiki](https://github.com/Nebukam/com.nebukam.orca/wiki) to get started!

---
### Installation
> To be used with [Unity's Package Manager](https://docs.unity3d.com/Manual/upm-ui-giturl.html).  
> ⚠ [Git Dependency Resolver For Unity](https://github.com/mob-sakai/GitDependencyResolverForUnity) must be installed *before* in order to fetch nested git dependencies. (See the [Installation troubleshooting](#installation-troubleshooting) if you encounter issues).  

See [Unity's Package Manager : Getting Started](https://docs.unity3d.com/Manual/upm-parts.html)

### Quick Start

The most straightforward way to setup and use ORCA is by using a ```ORCABundle<T>``` object :

```CSharp

    using Nebukam.ORCA;
    using Unity.Mathematics;

    ...

    // Create the "bundle" object, containing all 
    // necessary components for the simulation to run.
    ORCABundle<Agent> bundle = new ORCABundle<Agent>();
    bundle.plane = AxisPair.XY;

    // Add an agent
    Agent myFirstAgent = bundle.agents.Add(float3(0f));
    // Set its preferred velocity
    myFirstAgent.prefVelocity = float3(1f,1f,0f);

    ...

    void Update(){

        // Complete the simulation job only if the handle is flagged as completed.
        if(bundle.orca.TryComplete())
        {
            // Simulation step completed.
            myFirstAgent.velocity // -> simulated agent velocity
            myFirstAgent.position // -> simulated agent position

            bundle.orca.Schedule(Time.deltaTime);
            
        }else{
            // Keep the job updated with current delta time
            // ensuring we are framerate-agnostic
            bundle.orca.Schedule(Time.deltaTime);
        }

        

    }
    

```


### Scene setup
#### Sample: Setup
The package include a Setup sample showing how to use the library. Note that it does not use models, and instead draw debug visualization in the Editor window.

#### Infos

- Favor limited amount of obstacles with lots of segments over large amount of obstacles with few segments.
- Avoid concave obstacles with acute angles. (Concave obstacles are OK otherwise)
- Intersecting & crossing segments in a same obstacle will cause undesired behaviors. (Overlapping different obstacles is OK.)
- **A dynamic obstacle is considered 'dynamic' because it lives in a stack that is recomputed _each cycle_**, compared to 'static' ones (e.g. undestructible, unmovable) which are computed at the beginning of the simulation only (as well as when new obstacles are added to the obstacle group). Although static obstacles can be recomputed at will, not doing so is much more efficient. As such, dynamic obstacles are not _physical_ obstacles. Moving such obstacles may cause inter-penetration with agents. If you are looking for "push" behavior, use an Agent with ```navigationEnabled = false;``` instead.

- Current raycasting implementation is slow, and asynchronous (it is processed right after agents simulation) -- if you are using Unity's colliders in your project, you will likely get better performance out of Unity's Physic raycast.

---
## Dependencies
- **Unity.Burst 1.7.4** [com.unity.burst]()
- **Unity.Collections 1.3.1** [com.unity.collections]()
- **Unity.Mathematics 1.1.0** -- [com.unity.mathematics](https://github.com/Unity-Technologies/Unity.Mathematics)
- **N:Common** -- [com.nebukam.common](https://github.com/Nebukam/com.nebukam.common.git)
- **N:JobAssist** -- [com.nebukam.job-assist](https://github.com/Nebukam/com.nebukam.job-assist.git)



---
## Credits

While the core calculations & equations are heavily tweaked, they are based on the famous [RVO2 Library](http://gamma.cs.unc.edu/RVO2/).

---
---
## N:Packages for Unity

| Package | Infos |
| :---| :---|
|**_Standalone_**|
|[com.nebukam.easing](https://github.com/Nebukam/com.nebukam.easing.git)|**N:Easing** provide barebone, garbage-free easing & tweening utilities.|
|[com.nebukam.signals](https://github.com/Nebukam/com.nebukam.signals.git)|**N:Signals** is a lightweight, event-like signal/slot lib.|
|**_General purpose_**|
|[com.nebukam.common](https://github.com/Nebukam/com.nebukam.common.git)|**N:Common** are shared resources for non-standalone N:Packages|
|[com.nebukam.job-assist](https://github.com/Nebukam/com.nebukam.job-assist.git)|**N:JobAssist** is a lightweight lib to manage Jobs & their resources.|
|[com.nebukam.slate](https://github.com/Nebukam/com.nebukam.slate.git)|**N:Slate** is a barebone set of utilities to manipulate graphs (node & their connections).|
|[com.nebukam.v-field](https://github.com/Nebukam/com.nebukam.v-field.git)|**N:V-Field** is a barebone lib to work vector fields|
|[com.nebukam.geom](https://github.com/Nebukam/com.nebukam.geom.git)|**N:Geom** is a procedural geometry toolkit.|
|[com.nebukam.splines](https://github.com/Nebukam/com.nebukam.splines.git)|**N:Splines** is a procedural geometry toolkit focused on splines & paths.|
|[com.nebukam.ffd](https://github.com/Nebukam/com.nebukam.ffd.git)|**N:FFD** is a lightweight set of utilities to create free-form deformation envelopes.|
|**_Procgen_**|
|[com.nebukam.wfc](https://github.com/Nebukam/com.nebukam.wfc.git)|**N:WFC** is a spinoff on the popular Wave Function Collapse algorithm.|
|**_Navigation_**|
|[com.nebukam.orca](https://github.com/Nebukam/com.nebukam.orca.git)|**N:ORCA** is a feature-rich Optimal Reciprocal Collision Avoidance lib|
|[com.nebukam.beacon](https://github.com/Nebukam/com.nebukam.beacon.git)|**N:Beacon** is a modular navigation solution|
|[com.nebukam.beacon-orca](https://github.com/Nebukam/com.nebukam.beacon-orca.git)|**N:Beacon** module providing a user-friendly **N:ORCA** implementation.|
|[com.nebukam.beacon-v-field](https://github.com/Nebukam/com.nebukam.beacon-v-field.git)|**N:Beacon** module providing a user-friendly **N:V-Field** implementation.|

---
## Installation Troubleshooting

After installing this package, Unity may complain about missing namespace references error (effectively located in dependencies). What [Git Dependency Resolver For Unity](https://github.com/mob-sakai/GitDependencyResolverForUnity) does, instead of editing your project's package.json, is create local copies of the git repo *effectively acting as custom local packages*.
Hence, if you encounter issues, try the following:
- In the project explorer, do a ```Reimport All``` on the **Packages** folder (located at the same level as **Assets** & **Favorites**). This should do the trick.
- Delete Library/ScriptAssemblies from you project, then ```Reimport All```.
- Check the [Resolver usage for users](https://github.com/mob-sakai/GitDependencyResolverForUnity#usage)
