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


Keystone Game Blocks is a 3D Game Framework written in c#. The primary goals for KGB were to make a 3D game framework:

- EASY to use for developers which means the architecture and organization of the code was the most important thing to be designed well. 
 
- EXTENSIBILE via plugins, c# scripting and in adding new features and capabilities such as a new renderers using DirectX12 or Vulcan for instance. (it currently just uses DirectX9 via a wrapper called Truevision3D, but because the KGB architecture is well designed, it's very easy to add new renderers) 

- PERFORMANT is the third consideration because this framework is designed for AA games that focus on GAMEPLAY and not AAA games that often focus on GRAPHICS and AESTHETICS.

- NEWORKING/MULTIPLAYER - the entire framework currently uses LOOPBACK messaging for virtually everything. This means making real network enabled games will be as simple as changing the HOST to target a remote server instead of LOCALHOST. This will also eventually allow for the EDITOR to support collaborative EDITING and UNDO/REDO options very easily.

 It features typical game framework things:
- 3D Editor
- Hybrid Scene Graph with Scene Management functions including 
 o - multi-threaded hierarchical culling
 o - finding of visibility sets
 o - optional spatial tree partitioning including Octree and Quadtrees
 o - optional Zone based partitioning
 o - origin-based camera rendering to support artifact-free rendering far from the logical camera position.
 o - Save / Load of Scenes

- Prefabs
- Wavefront .Obj loading
- Linear Interpolation Animation Editing via Plugin (can create CPU driven animations that interpolate Light Entities' diffuse/ambient/specular, material diffuse/ambient/specular/emissive, Translation Scale and Rotation interpolation of both Models and Entities, 
- Multiple rendering viewports on screen at the same time (which is used by my also work-in-progress game SciFi Command which features simultaneous Exterior Ship views typical of most space-sims, with the addition of Interior Deck Plan and "Away-Team" views onscreen at the same time for a unique Capital Ship Management Simulation) 
- SM3 shaders

WARNING: All code in the \\stage\\ folder needs to be integrated into the main branch but I can't do it at this time as I'm working to buy a new laptop computer.

I'm most interested in getting the Generic Memory<T> code integrated to replace the slow per-Entity 'updates()' (for movement for example) to a "Data Processing Model" that iterates once per frame over contiguous memory of all relevant Entities' data with a single Memory<T> instance. This should result in significant CPU performance improvements.


Thank you.
Michael P. Joseph


