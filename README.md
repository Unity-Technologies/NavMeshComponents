> Please use the branch matching the version of your Unity editor: [master](../../tree/master) for the latest released LTS version, [2020.1](../../tree/2020.1), [2019.3](../../tree/2019.3) for up to 2019.4-LTS, [2018.3](../../tree/2018.3) for up to 2018.4-LTS and 2019.2, [2018.2](../../tree/2018.2), [2018.1](../../tree/2018.1), [2017.2](../../tree/2017.2) for up to 2017.4-LTS, [2017.1](../../tree/2017.1), [5.6](../../tree/5.6).\
> You can use the [package](../../tree/package) branch in Unity 2019.4 or newer in order to add this code to a project in the form of a package. For instructions please refer to the [Setup](../../tree/package#setup) section of the README file.

# Components for Runtime NavMesh Building

Here we introduce four components for the navigation system:

* __NavMeshSurface__ – for building and enabling a NavMesh surface for one agent type.
* __NavMeshModifier__ – affects the NavMesh generation of NavMesh area types, based on the transform hierarchy.
* __NavMeshModifierVolume__ – affects the NavMesh generation of NavMesh area types, based on volume.
* __NavMeshLink__ – connects same or different NavMesh surfaces for one agent type.

These components comprise the high level controls for building and using NavMeshes at runtime as well as edit time.

Detailed information can be found in the [Documentation](Documentation~) section or in the [NavMesh building components](https://docs.unity3d.com/Manual/NavMesh-BuildingComponents.html) section of the Unity Manual.

# Setup

In Unity 2019.4 or newer versions follow the instructions in the manual about [Installing a package from a Git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html) in order to use this package directly from the GitHub repository. On short, you can add the following line in the `dependencies` section of you project's `Packages\manifest.json` file.\
``` "com.unity.ai.navigation.components": "https://github.com/Unity-Technologies/NavMeshComponents.git#package" ```

Another way to use the package is by referencing a local clone of this repository in a project as described in the [Installing a package from a local folder](https://docs.unity3d.com/Manual/upm-ui-local.html) instructions.

Alternatively, you can copy the contents of the `Editor`, `Gizmos` and `Runtime` folders to an existing project.

Additional examples are available in the [Samples~](Samples~) folder. To try them out copy the contents of the folder into a Unity project that references this NavMesh Components package. The examples are provided "as is". They are neither generic nor robust, but serve as inspiration.

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

# Notice on the Change of License

Starting with 2020-12-08 the content of this package is [licensed](LICENSE.md) under the [Unity Companion License](https://unity3d.com/legal/licenses/unity_companion_license) for Unity-dependent projects. All content that was accessed under the old MIT license remains under that license.
