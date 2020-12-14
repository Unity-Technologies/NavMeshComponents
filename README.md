> Please use the branch matching the version of your Unity editor: [master](../../tree/master) for the latest released LTS version and up to 2020.2, [2019.3](../../tree/2019.3) for up to 2019.4-LTS, [2018.3](../../tree/2018.3) for up to 2018.4-LTS and 2019.2, [2018.2](../../tree/2018.2), [2018.1](../../tree/2018.1), [2017.2](../../tree/2017.2) for up to 2017.4-LTS, [2017.1](../../tree/2017.1), [5.6](../../tree/5.6).\
> You can use the [package](../../tree/package) branch in Unity 2019.4 or newer in order to add this code to a project in the form of a package. For instructions please refer to the [Setup](../../tree/package#setup) section of the README file.

# Components for Runtime NavMesh Building

Here we introduce four components for the navigation system:

* __NavMeshSurface__ – for building and enabling a NavMesh surface for one agent type.
* __NavMeshModifier__ – affects the NavMesh generation of NavMesh area types, based on the transform hierarchy.
* __NavMeshModifierVolume__ – affects the NavMesh generation of NavMesh area types, based on volume.
* __NavMeshLink__ – connects same or different NavMesh surfaces for one agent type.

These components comprise the high level controls for building and using NavMeshes at runtime as well as edit time.

Detailed information can be found in the [Documentation](Documentation) section or in the [NavMesh building components](https://docs.unity3d.com/Manual/NavMesh-BuildingComponents.html) section of the Unity Manual.

# How To Get Started

Download and install Unity 5.6 or newer.

Clone or download this repository and open the project in Unity.
Alternatively, you can copy the contents of `Assets/NavMeshComponents` to an existing project. Make sure to select a branch of the repository that matches the Unity version.

Additional examples are available in the `Assets/Examples` folder.
The examples are provided "as is". They are neither generic nor robust, but serve as inspiration.

_Note: During the beta cycle features and API are subject to change.\
**Make sure to backup an existing project before opening it with a beta build.**_

# FAQ

Q: Can I bake a NavMesh at runtime?  
A: Yes.

Q: Can I use NavMesh'es for more than one agent size?  
A: Yes.

Q: Can I put a NavMesh in a prefab?  
A: Yes - with some limitations.

Q: How do I connect two NavMesh surfaces?  
A: Use the NavMeshLink to connect the two sides.

Q: How do I query the NavMesh for one specific size of agent?  
A: Use the NavMeshQuery filter when querying the NavMesh.

Q: What's the deal with the 'DefaultExecutionOrder' attribute?  
A: It gives a way of controlling the order of execution of scripts - specifically it allows us to build a NavMesh before the
(native) NavMeshAgent component is enabled.

Q: What's the use of the new delegate 'NavMesh.onPreUpdate'?  
A: It allows you to hook in to controlling the NavMesh data and links set up before the navigation update loop is called on the native side.

Q: Can I do moving NavMesh platforms?  
A: No - new API is required for consistently moving platforms carrying agents.

Q: Is OffMeshLink now obsolete?  
A: No - you can still use OffMeshLink - however you'll find that NavMeshLink is more flexible and have less overhead.

Q: What happened to HeightMesh and Auto Generated OffMeshLinks?  
A: They're not supported in the new NavMesh building feature. HeightMesh will be added at some point. Auto OffMeshLink generation will possibly be replaced with a solution that allows better control of placement.
