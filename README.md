# High Level Components for Runtime NavMesh Building

Here we introduce four high level components for the navigation system:

* __NavMeshSurface__ – for building and enabling a navmesh surface for one agent type.
* __NavMeshModifier__ – affects the navmesh generation of navmesh area types, based on the transform hierarchy.
* __NavMeshModifierVolume__ – affects the navmesh generation of navmesh area types, based on volume.
* __NavMeshLink__ – connects same or different navmesh surfaces for one agent type.

These components comprise the high level controls for building and using NavMeshes at runtime as well as edit time.

### Further Documentation (draft)

High Level Components:  
https://docs.google.com/document/d/1usMrwMHTPNBFyT1hZRt-nQZzRDTciIQRVzmA7MQsFNw

Low Level API:  
https://docs.google.com/document/d/1g1KE59K7HQq25kAMxweRt5ePXkEMvgNnAU3zfX_R7m8

# How To Get Started

Download the feature build which is based on Unity 5.5

http://beta.unity3d.com/download/98b817aefff4/public_download.html

Clone or download this repository and open the project using the feature build.
Alternatively, you can copy the contents of `Assets/` to an existing project.

_Note: the Unity build above is an experimental feature build, features and API is subject to change_ 

# FAQ

Q: Can I bake navmesh at runtime?  
A: yes

Q: Can I use navmesh'es for more than one agent size?  
A: yes

Q: Can I put a navmesh in a prefab?  
A: yes - with some limitiations.

Q: How do i connect two navmesh surfaces?  
A: Use the NavMeshLink to connect the two sides

Q: How do i query the navmesh for one specific size of agent?  
A: Use the NavMeshQuery filter when querying the navmesh

Q: What's the deal with the 'DefaultExecutionOrder' attribute?  
A: It gives a way of controlling the order of execution of scripts - specifically it allows us to build a navmesh before the
(native) navmeshagent component is enabled.

Q: What's the use of the new delegate 'NavMesh.onPreUpdate'?  
A: It allows you to hook in to controlling the navmesh data and links set up before the navigation update loop is called on the native side.

Q: Can I do moving NavMesh platforms?  
A: No - new API is required for consistently moving platforms carrying agents.

Q: Is OffMeshLink now obsolete?  
A: No - you can still use OffMeshLink - however you'll find that NavMeshLink is more flexible and have less overhead.

Q: What happened to HeightMesh and Auto Generated OffMeshLinks?  
A: They're not supported in the new navmesh building feature. HeightMesh will be added at some point. Auto OffMeshLink generation will possibly be replaced with a solution that allows better control of placement.
