using System;
using KeyScript.Host ;
using KeyScript.Interfaces;
using Settings;
using Keystone.Types;
using Antlr4.StringTemplate;
using Antlr4.StringTemplate.Compiler;
using Antlr4.StringTemplate.Extensions;
using Antlr4.StringTemplate.Misc;

namespace KeyScript
{
    public class BaseScript
    {
        // Host object
        protected static IScriptingHost mHost;

        // APIs
        protected static ITimingAPI TimingAPI;
        protected static IDatabaseAPI DatabaseAPI;
        protected static IGameAPI GameAPI;
        protected static IGraphicsAPI GraphicsAPI;
        protected static IEntityAPI EntityAPI;
        protected static IPhysicsAPI PhysicsAPI;
        protected static IAIAPI AIAPI; // artificial intelligence
        protected static IVisualFXAPI VisualFXAPI;
        protected static IAudioFXAPI AudioFXAPI;
        protected static IAnimationAPI AnimationAPI;
        protected static IWindowingAPI WindowingAPI;
        
        
        public static void Initialize(IScriptingHost host)
        {

            mHost = host;
            // reference these to static vars to make it easier to reference these API's in script
            DatabaseAPI = mHost.DatabaseAPI;
            TimingAPI = mHost.TimingAPI;
            GameAPI = mHost.GameAPI;
            GraphicsAPI = mHost.GraphicsAPI;
            EntityAPI = mHost.EntityAPI;
            PhysicsAPI = mHost.PhysicsAPI;
            AIAPI = mHost.AIAPI;
            
            VisualFXAPI = mHost.VisualFXAPI;
            AudioFXAPI = mHost.AudioFXAPI;
            AnimationAPI = mHost.AnimationAPI ;
            
            WindowingAPI = mHost.WindowingAPI;

            Game01.GameObjects.EmissionValue ev;

        }

        public static Vector3d[] QueryCellPlacement(string entityID, string celledRegionID, Vector3d position, byte cellRotation)
        {
            const double TO_DEGREES = 360d / 256d;
            Vector3d scale, rotation;
            scale.x = 1;
            scale.y = 1;
            scale.z = 1;
            
            rotation.x = 0;
            rotation.y = (double)(cellRotation * TO_DEGREES);
            rotation.z = 0;

            // tile location is basically x, z pixel coordinate with y as floor level
            // we need to convert this to a celledRegion relative coordinate which will give us
            // a 3D position that represents the CENTER of that tile (not the top/left corner)
            // including the Y center of the Cell the tile is in.
            //position = EntityAPI.CellMap_GetTilePosition3D(celledRegionID, tileLocation);
            Vector3d size = EntityAPI.CellMap_GetTileSize(celledRegionID);


            // determine if the origin.Y of this mesh is same as mesh boundingBox.Center.y


            // assuming this generic component has it's Y position at floor (not centered in it's bbox)
            // then we need to compute a new position.y that is lower so that the component appeasr to sit
            // on the floor of the cell.	
            // the problem here is, for an entire hierarchical entity we can find the lowest y point
            // and then position the entity such that this lowest point is at floor height
            // but for hierarchical entities, this is problematic i think.  
            double floorHeight = position.y - (size.y * 0.5);
            

            Vector3d entityCurrentPosition = EntityAPI.GetPositionRegionSpace(entityID);
            double entityFloor = EntityAPI.GetFloor(entityID);
            position.y = entityCurrentPosition.y - (entityFloor - floorHeight); 
            // return values and dont use EntityAPI to set, since it's a Query and not an Update
            return new Vector3d[] { position, scale, rotation };
        }
        
        // TODO: this function shouldn't be here.  It should perhaps be part of VisualFXAPI.???
    	public static void DrawHealthBar (string contextID, string entityID, Vector3d cameraSpacePosition, Vector3d[] cameraSpaceBoundingBoxVertices, float barWidth, float barHeight)
		{
			
			int health = (int)EntityAPI.GetCustomPropertyValue (entityID, "health");
			int maxHealth = 100;
			int barLength = (int)((health / (float)maxHealth) * barWidth);
			if (barLength <= 0) return;
				
			// find min/max of projected bbox coordinates
			Vector3d min, max;
			min.x = min.y = min.z = double.MaxValue;
			max.x = max.y = max.z = double.MinValue;
			for (int i = 0; i < cameraSpaceBoundingBoxVertices.Length; i++)
			{
				Vector3d temp = GraphicsAPI.Project (contextID, cameraSpaceBoundingBoxVertices[i]);
				min.x = Math.Min (min.x, temp.x);
				min.y = Math.Min (min.y, temp.y);
				min.z = Math.Min (min.z, temp.z);
				
				max.x = Math.Max (max.x, temp.x);
				max.y = Math.Max (max.y, temp.y);
				max.z = Math.Max (max.z, temp.z);
			}
	
	//      scale bar width and height by viewport width & height		
	//		int vpwidth = GraphicsAPI.GetViewportWidth(contextID);
	//		int vpheight = GraphicsAPI.GetViewportHeight(contextID);
	
			float creepHalfWidth = (float)(max.x - min.x) / 2f;
			// find left such that bar is centered over creep
			float left = (float)max.x - creepHalfWidth - barWidth / 2f;
			float top = (float)min.y;
			int innerColor  = VisualFXAPI.RGBA(1f, 0f, 0f, 1f);
			int outerColor = VisualFXAPI.RGBA (0f,0f,0f, 1f);
			
			// outer black health bar
			VisualFXAPI.DrawQuad (contextID, 
			left - 1,
			top - 1,
			left + barWidth + 1, 
			top + barHeight + 1,
	        outerColor);
			
			// inner color health bar based on current health level
			VisualFXAPI.DrawQuad (contextID, 
			left,
			top,
			left + barLength, 
			top + barHeight,
			innerColor);
		}
    }
}