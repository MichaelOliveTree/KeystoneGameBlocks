using System;
using Keystone.Elements;
using Keystone.Entities;
using Keystone.Portals;
using Keystone.Types;

namespace Keystone.Helpers
{
    public class Functions
    {

        // TODO: All of these should be moved to KeyStandardLibrary\Utilities 
        // 


        /// <summary>
        /// Returns a converted RegionSpaceRay from source Region to Destination Region
        /// </summary>
        /// <param name="regionSpaceRay"></param>
        /// <param name="regionSpaceRayOriginRegion"></param>
        /// <param name="targetRegion"></param>
        /// <returns></returns>
        public static Ray GetRegionSpaceRay(Ray regionSpaceRay, Region sourceRegion, Region destinationRegion)
        {  
            Matrix dummy;
            Matrix source2DestTransform = Matrix.Source2Dest (sourceRegion.GlobalMatrix, destinationRegion.GlobalMatrix);
            
            // now that we have computed a transform, apply it and return a region space ray in destination region space
            Ray destRegionSpaceRay = regionSpaceRay.Transform(source2DestTransform);
    
            return destRegionSpaceRay;
        }
                
        
        public static Matrix GetToModelSpaceMatrix(Region sourceRegion, Elements.Transform target, Region destinationRegion, Matrix minimeshElementLocalMatrix, out Matrix toDestinationRegionSpace)
        {
            // Compute a transform that will allow us to get a model space ray from a camera space ray.
        	// Remember that a camera space ray has camera at origin and our target entities\models positioned in
        	// relation to the camera
            Matrix entityRegionMatrix;
            Matrix regionSpaceToModelSpaceTransform;
            
            // target IS a region _and_ IS IN FACT THE SAME Region as originRegion
            if (target is Region && target == sourceRegion)
            {
                // we know every Region entity must have a RegionMatrix == Identity
            	entityRegionMatrix = Matrix.Identity();
                
                // no point inverting identity matrix
            	regionSpaceToModelSpaceTransform = minimeshElementLocalMatrix * entityRegionMatrix; 
            }
            // target IS NOT A Region (so it's own RegionMatrix is not Identity)
			// but target IS INSIDE SAME Region as originRegion (can both be null also)
            else if (destinationRegion == sourceRegion)
            {
            	// make copy of target's RegionMatrix so as not to change original's contents and then
	            // translate to camera space 
            	entityRegionMatrix = new Matrix (minimeshElementLocalMatrix * target.RegionMatrix);         	
	        	regionSpaceToModelSpaceTransform = Matrix.Inverse(entityRegionMatrix);
            }
            // target is NOT inside originRegion so we must compute a region matrix to transform 
            // the target into space that is _relative_ to the originRegion's.
            else
            {
            	// TODO: can we substract the .GlobalMatrix offsets first?  multiplying minimeshElement by what could be huge GlobalMatrix is just no good
            	//       ideally we could subtract the relative offset and have originRegion's matrix as Identity. 
            	//       TODO: or can we multiply the minimeshElementLocalMatrix afterwards and still have same result?
            	entityRegionMatrix = Matrix.Source2Dest (sourceRegion.GlobalMatrix, minimeshElementLocalMatrix * target.GlobalMatrix);
	        	// IMPORTANT: Always test this functionality where Ray starting Zone AND target Zone ARE NOT the default statup Zone.
	        	//            This way starting Zone's and target Zone's GlobalMatrix will not be Identity.            	
	        	regionSpaceToModelSpaceTransform = Matrix.Inverse(entityRegionMatrix);
            }
 
            toDestinationRegionSpace = entityRegionMatrix;
            return regionSpaceToModelSpaceTransform;
        }
        
        public static Matrix GetToModelSpaceMatrix (Region sourceRegion, Elements.Transform target, Region destinationRegion, out Matrix toDestinationRegionSpace)
        {
        	// Compute a transform that will allow us to get a model space ray from a camera space ray.
        	// Remember that a camera space ray has camera at origin and our target entities\models positioned in
        	// relation to the camera
            Matrix entityRegionMatrix;
            Matrix regionSpaceToModelSpaceTransform;

            // target IS a region _and_ IS IN FACT THE SAME Region as originRegion
            if (target is Region && target == sourceRegion)
            {

                // we know every Region entity must have a RegionMatrix == Identity
                entityRegionMatrix = Matrix.Identity();

                // no point inverting identity matrix 
                regionSpaceToModelSpaceTransform = entityRegionMatrix;
            }
            // target IS NOT A Region (so it's own RegionMatrix is not Identity)
            // but target IS INSIDE SAME Region as originRegion (can both be null also)
            else if (destinationRegion == sourceRegion)
            {
                // make copy of target's RegionMatrix so as not to change original's contents and then
                // translate to camera space 
           	    entityRegionMatrix = new Matrix (target.RegionMatrix);

                regionSpaceToModelSpaceTransform = Matrix.Inverse(entityRegionMatrix);
            }
            // target is NOT inside originRegion so we must compute a region matrix to transform 
            // the target into space that is _relative_ to the originRegion's.
            else
            {
            	entityRegionMatrix = Matrix.Source2Dest (sourceRegion.GlobalMatrix, target.GlobalMatrix); 
	        	// IMPORTANT: Always test this functionality where Ray starting Zone AND target Zone ARE NOT the default statup Zone.
	        	//            This way starting Zone's and target Zone's GlobalMatrix will not be Identity.            	
	        	regionSpaceToModelSpaceTransform = Matrix.Inverse(entityRegionMatrix);
            }
            
            toDestinationRegionSpace = entityRegionMatrix;
            return regionSpaceToModelSpaceTransform;
        }
        
        public static Ray GetModelSpaceRay(Ray regionSpaceRay, Region sourceRegion, Elements.Transform target, Region destinationRegion)
        {  
            Matrix dummy;
    		Matrix regionSpaceToModelSpaceTransform = GetToModelSpaceMatrix (sourceRegion, target, destinationRegion, out dummy);
            


            // now that we have computed a transform, apply it and return a model space ray
            Ray modelSpaceRay = regionSpaceRay.Transform(regionSpaceToModelSpaceTransform);
    
            return modelSpaceRay;
        }

        public static Ray GetBillboardModelSpaceRay(Ray regionSpaceRay, Region sourceRegion, Elements.Transform target, Transform targetParent, Region destinationRegion, Vector3d cameraSpacePosition, Vector3d cameraPosition, Vector3d up, Vector3d lookAt)
        {
            Model model = (Model)target;
            Matrix regionSpaceToModelSpaceTransform;
            Matrix m = Model.GetRotationOverrideMatrix(targetParent.DerivedRotation, model.Rotation, cameraSpacePosition, cameraPosition, up, lookAt, true, true); ;

            Matrix sMat = Matrix.CreateScaling(model.DerivedScale);
            Matrix tmat = Matrix.CreateTranslation(model.DerivedTranslation);
            m = sMat * m * tmat;

            regionSpaceToModelSpaceTransform = Matrix.Inverse(m);
           


            Ray modelSpaceRay = regionSpaceRay.Transform(regionSpaceToModelSpaceTransform);
            return modelSpaceRay;
        }
        
        public static Ray GetMiniMeshElementModelSpaceRay (Ray regionSpaceRay, Region sourceRegion, Elements.Transform target, 
                                                           Region destinationRegion, Matrix minimeshElementLocalMatrix)
        {
    		Matrix dummy;
    		Matrix regionSpaceToModelSpaceTransform = GetToModelSpaceMatrix (sourceRegion, target, destinationRegion, minimeshElementLocalMatrix, out dummy);
            
            // now that we have computed a transform, apply it and return a model space ray
            Ray modelSpaceRay = regionSpaceRay.Transform(regionSpaceToModelSpaceTransform);
    
            return modelSpaceRay;
        }

        // GetModelSpaceBox() is mostly used to cull at SceneNode/EntityNode/RegionNode bounding box level
        // NOTE: Entity must be in same Region as the Camera
        // Recall: Geometry is already in ModelSpace.
        public static BoundingBox GetModelSpaceBox(Entity target)
        {
            // conversions of Entity to MODEL SPACE is easy.  This works for and across Zones or Single Region world 
            // make copy so as not to change original <-- is this still necessary?     
            Matrix entityRegionMatrix = new Matrix (target.RegionMatrix);
                   
            Matrix toModelSpace = Matrix.Inverse(entityRegionMatrix);
            BoundingBox modelSpaceBox = BoundingBox.Transform(target.SceneNode.BoundingBox, toModelSpace);
            return modelSpaceBox;
        }
    }
}
