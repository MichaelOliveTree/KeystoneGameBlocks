using System;
using System.Collections.Generic;


namespace KeyScript.Interfaces
{
    public interface IScriptable
    { 
       // Scripts[] GetScriptableEvents ()  // all - read only
       // Scripts[] GetScriptableEvents (int interfaceID) // only those scripts specific to a particular interface - read only
        void AssignScript ();
        void RemoveScript();
    }
}
