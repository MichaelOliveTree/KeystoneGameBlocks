Keystone Game Blocks and SciFi Command are copyright October 2025 by Michael Preston Joseph (TP_WalksWithGod@proton.me 
or
MichaelWalksWithGod@gmail.com)

If you've found any of this source code anywhere else but at this GitHub repository (excluding those of the open source libraries used therein), BE WARNED THAT, THAT CODE IS STOLEN.  

NOTE: This source code was originally uploaded by me to www.github.com/WalksWithGod/KeystoneGameBlocks , however after having my phone stolen again, I was locked out of that site due to not having access to the 2FA devise, so I've created this GitHub.com/MichaelOliveTree/KeystoneGameBlocks

The relevant source code contained within this repository that is copyright by me, has currently NOT BEEN LICENSED to any persons, organizations or businesses.

Please report any illegal distributions of my work to me at TP_WalksWithGod@proton.me
or
MichaelWalksWithGod@gmail.com

Also, if you are interested in licensing from me the applicable code contained within this Game Framework (the code to which I have full copyrights), contact me via either of the previous email addresses provided.  I am very busy these days, but I will try to get back to you ASAP.

_______________________________________________________________________________________________________________________
Keystone Game Blocks(KGB) is a 3D Game Framework written in C#.  The primary goal was to make a 3D game framework that met the following criteria:

- EASY to use for developers  which means the architecture and organization of the code had to be well designed. 
- EXTENSIBILE via plugins, C# scripting and in adding new features and capabilities such as a new renderers using DirectX12 or Vulcan for instance. (it currently just uses DirectX9 via a wrapper called Truevision3D, but because the KGB architecture is well designed, it's very easy to add new renderers) 
- PERFORMANT is the third consideration because this framework is designed for AA games that focus on GAMEPLAY and not AAA games that often focus on GRAPHICS and AESTHETICS.
- NEWORKING/MULTIPLAYER - the entire framework currently uses LOOPBACK messaging for virtually everything.  This means making real network enabled games will be as simple as changing the HOST to target a remote server instead of LOCALHOST.  This will also eventually allow for the EDITOR to support collaborative EDITING and UNDO/REDO options very easily.  Keystone Game Blocks can use either the Lidgren UDP Connection objects or a custom derived version of the base Lidgren Connection object, which I developed myself, that uses TCP instead of UDP, and does so in an agnostic way so that the calling application does need to care whether it's working with a UDP Lidren Connection or the custom TCP Connection object.

 KGB features typical game framework things:
 
* 3D Editor
* Hybrid Scene Graph (Nodes not ECS) with Scene Management functions including
    * 64bit precision for storing Entity Translations, Scales and Rotations.
    * multi-threaded hierarchical culling
    * finding and grouping of visible sets into Buckets.
    * camera-relative rendering to support artifact-free rendering far from the logical camera position.
    * Save / Load of Scenes
    * Region based spatial partioning.  Every "Region" such as a Starship's Interior uses its own coordinate system with origin (0,0,0) and is rendered in camera relative position with respect to the logical "world" coordinates.  This allows KGB to handle rendering large planets and stars at realistic scales while also rendering comparatively tiny objects like chairs within the interior of a ship at the same time.
    * optional spatial tree partitioning including Octree and Quadtrees within Regions.
    * optional Zone based partitioning.  Zones are inheritied from Region, but the difference is that a Zone can never be translated, rotated or scaled.
* C# Scripting.
    * ability to define custom Entity properties
    * ability to define custom Events and allow any Entity to subscribe to the Events of another.
* CPU Portal and Occlusion Culling options
* A* Pathfinding for Agents
* Prefabs
* Wavefront .Obj loading
* Linear Interpolation Animation Editing via Plugin (can create CPU driven animations that interpolate Model & Entity Translations Scale and Rotation as well as Light Entities' diffuse/ambient/specular, and Materials' diffuse/ambient/specular/emissive, and 
* Multiple rendering viewports on screen at the same time (which is used by my also work-in-progress game SciFi Command which features simultaneous Exterior Ship views typical of most space-sims, with the addition of Interior Deck Plan and "Away-Team" views onscreen at the same time for a unique Capital Ship Management Simulation) 
* SM3 shaders

WARNING: All code in the \\stage\\ folder needs to be integrated into the main branch but I can't do it at this time as I'm working to buy a new laptop computer.

I'm most interested in getting the Generic Memory<T> code integrated to replace the slow per-Entity 'updates()' (for movement for example) to a "Data Processing Model" that iterates once per frame over contiguous memory of all relevant Entities' data with a single Memory<T> instance.  This should result in significant CPU performance improvements.

Thank you.
Michael P. Joseph

TODO:
- Mission Editor (create a Reference Version - and not to be confused with the already built in Scene Editor) designed for SciFi Command and loadable as a Plugin.  The Mission Editor can be used to Add/Remove/Modify "mission objects" to a Scene that are stored and loaded seperately from the main scene.  The goal is to allow users to create custom Mission Editors that are game specific.
- 2D and 3D GUI that is built on top of the existing Node based 3D Scene Composition to provide a consistant and intuitive way for developing User Interfaces, including UIs that can be attached to in world 3D objects.
- Stats + Skills + Attributes "Status Effects aka Buffs/Debuffs" System
- Physics
- DX12 and Vulkan renderers
- Linux / WINE support testing.  I believe the Truevision3d graphics engine will work on Linux using WINE but I have not tested it myself nor have I been using Mono.  Would love to test this in the future however.
