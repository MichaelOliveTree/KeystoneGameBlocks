msvcr71.dll is an old dll from .net 1.1 days and there is no official redistributable still available for it.

mtv3d65.dll requires this to be placed in C:\Windows\SysWOW64

note: i did not have to regsrv32 it.

in CoreCliet.Initialize() on line _audio.Initialize(graphics.Handle); i have the same filenotfoundexception.  
im not sure if its vorbisdotnet or directsound.  just commenting out _audio.Initialize(graphics.Handle); allows the app to run.





