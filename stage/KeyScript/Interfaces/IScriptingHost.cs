using System;

namespace KeyScript.Interfaces
{
    public interface IScriptingHost
    {
        IDatabaseAPI DatabaseAPI { get; }
    	IGameAPI GameAPI {get;}
    	IGraphicsAPI GraphicsAPI { get; }
        IEntityAPI EntityAPI { get;}
        IPhysicsAPI PhysicsAPI { get;}
        IAIAPI AIAPI {get;}
        IVisualFXAPI VisualFXAPI { get; }
        IAudioFXAPI AudioFXAPI { get; }
        IAnimationAPI AnimationAPI {get;}
	
        // or how about the actual layout's paths are specified for us
        // and handled by whatever is doing localization.  all we care about is the path
        // eg.  data/mods/
        // TODO: our scripts are inside of zip files, not specifying a full path will definetly
        // give us problems when trying to search startup folders
        // TODO: i have no idea how to find layout scripts that are inside of zip files...
        // - we would have to recognize that and make an API call to get it into a string

        // DataPath
        // Layouts = DataPath + "//layouts"
        // Culture = "EN-US"
    }
}
