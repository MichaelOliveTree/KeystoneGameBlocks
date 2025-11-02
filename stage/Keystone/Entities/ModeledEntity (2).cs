using Keystone.Elements;
using Keystone.Collision;
using Keystone.Types;
using System;
using Keystone.FX;
using KeyCommon.Flags;

namespace Keystone.Entities
{
    
    public class ModeledEntity : Entity
    {
        protected Model mModel;
        protected ModelSelector mSelector; // can be null
        protected float _fixedScreenSpaceSize; // from 1.0 - 0.0 as a screenspace percentage.  At 0.0 we skip render.

        
        // NOTE: Entities don't need static Create's because they can never be shared.
        public ModeledEntity(string id)
            : base(id)
        {
            Visible = true;
            CollisionEnable = true;
            Overlay = false;

            // Triggers and Occluder entities would NOT be .VisibleInGame == true
            SetEntityAttributesValue((uint)EntityAttributes.VisibleInGame, true);
            // default collision enabled and physical simulation enabled
            SetEntityAttributesValue((uint)EntityAttributes.Dynamic, true); 
            SetEntityAttributesValue((uint)EntityAttributes.CollisionEnabled, true); 
            SetEntityAttributesValue((uint)EntityAttributes.Awake, true);            
        }

        #region ITraversable Members
        public override object Traverse(Keystone.Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion 

        #region ResourceBase members
        /// <summary>
        /// 
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {

            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
            Settings.PropertySpec[] properties = new Settings.PropertySpec[1 + tmp.Length];
            tmp.CopyTo(properties, 1);

            properties[0] = new Settings.PropertySpec("fixedscreensize", typeof(float).Name);
            
            if (!specOnly)
            {
                properties[0].DefaultValue = (float)_fixedScreenSpaceSize;
            }

            return properties;
        }

        // TODO: this should return any broken rules ?
        public override void SetProperties(Settings.PropertySpec[] properties)
        {
            if (properties == null) return;
            base.SetProperties(properties);

            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].DefaultValue == null) continue;
                // use of a switch allows us to pass in all or a few of the propspecs depending
                // on whether we're loading from xml or changing a single property via server directive
                switch (properties[i].Name)
                {
                    case "fixedscreensize":
                        _fixedScreenSpaceSize = (float)properties[i].DefaultValue;
                        break;
                }
            }
        }
        #endregion

        //public float TempFrame { get; set; } // temp property used by Elements\Emitter.cs

        public Model Model { get { return mModel; } }

        public bool UseLargeFrustum 
        {
        	get {return (mEntityFlags & EntityAttributes.LargeFrustum) == EntityAttributes.LargeFrustum;}
        	set
            {
                if (value)
                    mEntityFlags |= EntityAttributes.LargeFrustum;
                else
                    mEntityFlags &= ~EntityAttributes.LargeFrustum;
            }
        }
        
        public virtual bool Dynamic
        {
            get { return (mEntityFlags & EntityAttributes.Dynamic) == EntityAttributes.Dynamic; }
            set
            {
                if (value)
                    mEntityFlags |= EntityAttributes.Dynamic;
                else
                    mEntityFlags &= ~EntityAttributes.Dynamic;
            }
        }

        public virtual bool CollisionEnable
        {
            get { return (mEntityFlags & EntityAttributes.CollisionEnabled) == EntityAttributes.CollisionEnabled; }
            set 
            {
                if (value)
                    mEntityFlags |= EntityAttributes.CollisionEnabled;
                else
                    mEntityFlags &= ~EntityAttributes.CollisionEnabled;
            }
        }

        // TODO: ScreenSpaceSize and Overlay (Overlay property is in Entity.cs) move to Model?
        public float ScreenSpaceSize
        {
            get { return _fixedScreenSpaceSize; }
            set
            {
                if (value > 1.0f) value = 1.0f;
                if (value < 0.0f) value = 0.0f;
                _fixedScreenSpaceSize = value;
            }
        }

        public bool UseFixedScreenSpaceSize
        {
            get { return (mEntityFlags & EntityAttributes.UseFixedScreenSpaceSize) == EntityAttributes.UseFixedScreenSpaceSize; }
            set
            {
                if (value)
                    mEntityFlags |= EntityAttributes.UseFixedScreenSpaceSize;
                else
                    mEntityFlags &= ~EntityAttributes.UseFixedScreenSpaceSize;
            }
        }


        public bool HasLOD
        {
            get { return (mSelector != null && mSelector.ChildCount > 0); }
        }

		public bool IsGeometryFullyLoaded ()
        {
        	Model[] models = SelectModel(SelectionMode.Collision, -1);
            // TODO: apply _parameters.PickSearchType to limit scope if applicable
            if (models == null) return false;
            
            for (int i = 0; i < models.Length; i++)
                if (models[i].Geometry == null || models[i].Geometry.TVResourceIsLoaded == false)
       		 		return false;
            
            return true;
        }
		
        public Model[] SelectModel(SelectionMode pass, double distance)
        {
        	using (CoreClient._CoreClient.Profiler.HookUp ("Model Selection"))
        	{
	            if (mSelector != null)
	                return mSelector.Select(pass, this, distance); // ModelSelector.Select also has the open question of model.Enable being conceptually same as model.Visible
	            
	            if (mModel == null || mModel.Enable == false) // models can be independantly disable.  TODO: still waiting to determine if instead we should add a model.Visible property or whether conceptually for models they are the same thing?  because whwne not visible we dont want picking or rendering, but we will still want physics modeling if applicable
	                return null;
	            
	            return new Model[] {mModel};
        	}
        }

        // selects single model at specified index
        public Model SelectModel (uint index)
        {
        	using (CoreClient._CoreClient.Profiler.HookUp ("Model Selection"))
        	{
	            if (mModel != null && mModel.Enable )
	                if (index > 0) return null;
	                else return mModel;
	
	            if (mSelector != null)
	                return mSelector.Select(index);
	            
	            return null;
        	}
        }

        public virtual void AddChild(Model model)
        {
            // Ensure only one instance of Selector _OR_ one Model is allowed, but not one of both
            if (mModel != null && mSelector != null)
                throw new Exception("Model or Selector node already exists.  Only one node of either type allowed.");

            if (model.Parents != null && model.Parents.Length == 1) throw new Exception("Model's cannot be shared.  Can only have one parent.");
            mModel = model;
            AddChild((Node)mModel);
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
        }

        public void AddChild(ModelSelector selectorNode)
        {
            // Ensure only one instance of Selector _OR_ one Model is allowed, but not one of both
            if (mModel != null && mSelector != null)
                throw new Exception("Model or Selector node already exists.  Only one node of either type allowed.");

            mSelector = selectorNode;
            AddChild((Node)mSelector);
            SetChangeFlags(Enums.ChangeStates.GeometryAdded | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
        }
 
        public override void RemoveChild(Node child)
        {
            base.RemoveChild(child);
             if (child is ModelSelector)
            {
                mSelector = null;
                 // TODO: is change flag necessary?  because I think the remove recurse will
                 // propogate up the ultimate removal of geometry?
                SetChangeFlags(Enums.ChangeStates.GeometryRemoved | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
            }
             else if (child is Model)
             {
                 mModel = null;
                 // TODO: is change flag necessary?  because I think the remove recurse will
                 // propogate up the ultimate removal of geometry?
                 SetChangeFlags(Enums.ChangeStates.GeometryRemoved | Keystone.Enums.ChangeStates.BoundingBoxDirty, Enums.ChangeSource.Child);
             }
        }


        public override PickResults Collide(Vector3d start, Vector3d end, KeyCommon.Traversal.PickParameters parameters)
        {
            // NOTE: We don't even collide in ModeledEntity anymore.
            // In Picker.cs we do   Model[] models = entity.SelectModel(Collision, -1);
            // and then call Collide directly on the returned results.
            // It's now mostly only needed for CelledRegions and Lights right?  However we could
            // make the CelledRegion derived from Model to represent the CelledGeometry space within it...
            // this might be a good idea actually...  
            // We could also potentially add a type of model to a light for defining area lights / light shafts.
            // perhaps the shape of the model then becomes the shape of our light.
            throw new NotImplementedException();
        }
    }
}