# High Level API Components for Runtime NavMesh Building

Here we introduce four high level components for the navigation system:

* __NavMeshSurface__ – for building and enabling a navmesh surface for one agent type.
* __NavMeshModifier__ – affects the navmesh generation of navmesh area types, based on the transform hierarchy.
* __NavMeshModifierVolume__ – affects the navmesh generation of navmesh area types, based on volume.
* __NavMeshLink__ – connects same or different navmesh surfaces for one agent type.

These components comprise the high level controls for building and using NavMeshes at runtime as well as edit time.

Further Documentation (draft) :
https://docs.google.com/document/d/1usMrwMHTPNBFyT1hZRt-nQZzRDTciIQRVzmA7MQsFNw

# How To Get Started

Download the feature build which is based on Unity 5.5

http://beta.unity3d.com/download/9f4c055d6ef4/public_download.html

add the folder `Assets/NavMeshComponents` of this repository to your project.
