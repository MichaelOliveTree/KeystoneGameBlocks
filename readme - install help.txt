DirectX 12 
	- d3dx_42.dll needs to be installed.  Install DX12 and if any problems, find it and place it
          in the \\Libs\TV3D65\ dll where mtv3d65.dll is located
--------------------------------------

msvcr71.dll is an old dll from .net 1.1 days and there is no official redistributable still available for it.
	- mtv3d65.dll requires this to be placed in C:\Windows\SysWOW64
          I have included a copy in \\Libs\TV3D65\
	- I need to get Sylvain Dupont to remove this dependancy for me.
	- note: i did not have to regsrv32 it.

in CoreCliet.Initialize() on line _audio.Initialize(graphics.Handle); i have the same filenotfoundexception.  
im not sure if its vorbisdotnet or directsound.  just commenting out _audio.Initialize(graphics.Handle); allows the app to run.

--------------------------------------
Windows Defender "Controlled Folder Acess" & File Attributes = Read Only
	- if for some reason you have issues with .css scripts being copied from mods\\caesar\\scripts to \\bin\\...\\
          make sure the folder and file attributes are NOT read only.
	- Make sure if Windows Defender has "Controlled Folder Access" enabled and the relevant path is affected, 
          add KeyEdit.exe as an "Allowed App"


Truevision3d Watermark / Logo
--------------------------------------
I received BUT NOT YET TESTED, an updated MTV3D65.DLL from a fellow Truevision3d User who goes by the nickname "Waterman"
He is the developers for a boat simulator named "Stormind Simulator" (https://www.stormwind.fi/en)

This version of the MTV3D65.DLL was made for him by the lead developer of Truevion3d who is named 
Sylvain Dupont.
https://www.linkedin.com/in/sylvain-dupont-a7058912/

and he is the primary developer behind Meteociel
https://www.meteociel.fr/

This updated MTV3D65.DLL should have the logo/watermark removed and it also adds 64bit variables for the built in Newton Physics Engine.

It is located currently in \\Libs\kgb install support folder\\waterman\\extracted

--------------------------------------
If any strange issues loading any other external DLL that fails to load, try
Dependency Walker (binaries available)
https://github.com/lucasg/Dependencies





