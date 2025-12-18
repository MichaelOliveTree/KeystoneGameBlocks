using System;
using System.Collections.Generic;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;

namespace KeyEdit.Scripting
{

    /// <summary>
    ///
    /// </summary>
    public class ScriptingHost : IScriptingHost
    {
        private IDatabaseAPI mDatabaseAPI;
        private ITimingAPI mTimingAPI;
    	private IGameAPI mGameAPI;
        private IGraphicsAPI mGraphicsAPI;
        private IEntityAPI mEntityAPI;  // setting & getting properties of an entity
        private IPhysicsAPI mPhysicsAPI;
        private IAIAPI mAIAPI;
        private IVisualFXAPI mVisualFXAPI;    // explosions, temporary particles that would not be considered permanent part of a model (such as flame particle on a Fire Sword)
        private IAudioFXAPI mAudioFX;     // play music and sounds (actually this just dispatches the sounds to the actual Keystone Audio Manager
        private IAnimationAPI mAnimationAPI;
        

        public ScriptingHost(IDatabaseAPI databaseAPI, ITimingAPI timingAPI, IGameAPI gameAPI, IGraphicsAPI graphicsAPI, IEntityAPI entityAPI, IPhysicsAPI physicsAPI, IAIAPI aiAPI, IVisualFXAPI visualAPI, IAnimationAPI animationAPI, IAudioFXAPI audioAPI)
        {
            mDatabaseAPI = databaseAPI;
            mTimingAPI = timingAPI;
        	mGameAPI = gameAPI;
            mGraphicsAPI = graphicsAPI;
            mEntityAPI = entityAPI;
            mPhysicsAPI = physicsAPI;
            mAIAPI = aiAPI;
            mVisualFXAPI = visualAPI;
            mAudioFX = audioAPI;
            mAnimationAPI = animationAPI ;
        }

        public IDatabaseAPI DatabaseAPI { get { return mDatabaseAPI; } }
        public ITimingAPI TimingAPI {get {return mTimingAPI;}}
        public IGameAPI GameAPI {get {return mGameAPI;}}
        public IGraphicsAPI GraphicsAPI { get { return mGraphicsAPI; } }
        public IEntityAPI EntityAPI { get { return mEntityAPI; } }
        public IPhysicsAPI PhysicsAPI { get { return mPhysicsAPI; } }
        public IAIAPI AIAPI {get {return mAIAPI; } }
        public IVisualFXAPI VisualFXAPI { get { return mVisualFXAPI; } }
        public IAudioFXAPI AudioFXAPI { get { return mAudioFX; } }
        public IAnimationAPI AnimationAPI {get {return mAnimationAPI; } }
    }
}

//using System.Security;
//using System.Security.Permissions;
//using CSScriptLibrary;

//namespace KeyEdit.Scripting
//{


//    //class SpeedUpEffect
//    //{
//    //    public void AddEffect(GameEntity entity)
//    //    {
//    //        entity.Speed += 1;
//    //    }

//    //    public void RemoveEffect(GameEntity entity)
//    //    {
//    //        entity.Speed -= 1;
//    //    }
//    //}


//    public interface IHost
//    {

//        //string CreateEntity();

//        //void RegisterInfiniteCallback(int interval);
//       // void RegisterFiniteCallback(int interval, int count);


//    }

//    class Host
//    {

//        public void Test()
//        {

////            var CreateSomeFile = CSScript.LoadMethod(
////                        @"using System.IO;
////                          public static void Test()
////                          {
////                              try
////                              {  
////                                  using (var f = File.Open(""somefile.txt"", FileMode.OpenOrCreate))
////                                    Console.WriteLine(""File.Open: success"");
////                               }
////                               catch (Exception e)
////                               {
////                                   Console.WriteLine(e.GetType().ToString() + "": "" + e.Message);
////                               }
////                          }")
////                         .GetStaticMethod();


//            ////this is a logical equivalent of Sandbox.With.Execute syntactic sugar
//            //ExecuteInSandbox(permissionSet,               //call will fail as the set of permissions is insufficient
//            //                () => PerformSomeAction());
//        }

//        static void PerformSomeAction()
//        {
//            //StaticScene.InitBluePlanet(AppMain._core.PrimaryCamera.Region );
//        }

//        static void ExecuteInSandbox(PermissionSet permissionSet, Action action)
//        {
//            //permissionSet.PermitOnly();
//            //try
//            //{
//            //    action();
//            //}
//            //catch (Exception e)
//            //{
//            //    Console.WriteLine(e.GetType().ToString() + ": " + e.Message);
//            //}
//            //finally
//            //{
//            //    CodeAccessPermission.RevertPermitOnly();
//            //}
//        }
//    }
//}