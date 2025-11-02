using System;
using Microsoft.DirectX.DirectSound;
using Keystone.Elements;
using Keystone.Traversers;

namespace Keystone.Sound
{
	/// <summary>
	/// 3D Node for the scenegraph that can host multiple (TODO)
	/// AudioClip3D children.
	/// </summary>
	public class SoundNode3D : Elements.BoundTransformGroup 
	{
		private AudioClip3D mAudioClip3d;
		        
        internal SoundNode3D( string id) : base (id)
		{
		}
                        
        #region ITraversable Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        internal override ChildSetter GetChildSetter()
        {
            throw new NotImplementedException();
        }
        
        #region ResourceBase members
        /// <summary>
        /// </summary>
        /// <param name="specOnly">True returns the properties without any values assigned</param>
        /// <returns></returns>
        public override Settings.PropertySpec[] GetProperties(bool specOnly)
        {
//            Settings.PropertySpec[] tmp = base.GetProperties(specOnly);
//            Settings.PropertySpec[] properties = new Settings.PropertySpec[2 + tmp.Length];
//            tmp.CopyTo(properties, 2);
//
//            properties[0] = new Settings.PropertySpec("flags", typeof (byte).Name);
//            properties[1] = new Settings.PropertySpec("name", typeof(string).Name);
//
//            if (!specOnly)
//            {
//                properties[0].DefaultValue = (byte) mNodeFlags;
//                properties[1].DefaultValue = mFriendlyName;
//            }
//
//            return properties;

			return base.GetProperties (specOnly);
        }

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
//                    case "flags":
//                        // NOTE: calling the full property will allow any overridden 
//                        // "Enable" to be called such as with Light.css
//                        mNodeFlags = (NodeFlags)(byte)properties[i].DefaultValue;
//
//                        break;
//                    case "name":
//                        mFriendlyName = (string)properties[i].DefaultValue;
//                        break;
                }
            }
        }
        #endregion 

        internal AudioClip3D AudioClip3D 
        {
        	get 
        	{
        		return mAudioClip3d;
        	}
        }
        
        #region IGroup Members
        internal void AddChild(AudioClip3D child)
        {
        	if (child == null) throw new ArgumentNullException("SoundNode3D.AddChild() - child cannot be null.");
        	
        	base.AddChild (child);
        	mAudioClip3d = child;
        }
        
        public override void RemoveChild(Node child)
		{
        	if (child as AudioClip3D == null) throw new Exception();
        	mAudioClip3d = null;
        	base.RemoveChild(child);
		} 
        #endregion
	}
}
