using System;
using System.Diagnostics;
using Keystone.Types;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using Keystone.Elements;
using Keystone.Traversers;

namespace Keystone.Sound
{
 
	//       // TODO: rename as AudioEmitter.cs for 3D sound added to Scene as ambient entity.
	//       //  then it can have a volume and we can use a ListenerTraverser to find/play sounds
	//       //  that are not "culled." by the Listener traverser
	//       // https://msdn.microsoft.com/en-us/library/bb195055.aspx
	// TODO: a SoundNode2D.cs can reference multiple AudioClips.cs so that they are played and mixed together simultaneously.
	//       It is analagous to an Animation containing multiple AnimationClips.
	
	/// <summary>
	/// 2D Sound node useful for button presses and such on a GUI or HUD.
	/// </summary>
	public class SoundNode2D : Keystone.Elements.BoundTransformGroup  
    {
        private AudioClip mAudioClip;


        // The MinDistance property determines at which distance the sound volume is no longer increased.
        // You can also use this setting to make certain sounds appear louder even if they were recorded 
        // at the same volume (see the DirectX documentation for a detailed explanation of this).
        // The default value for this property is 1 meter, meaning that the sound is at full volume when 
        // the distance between the listener and the sound source equals 1 meter.

        // The MaxDistance property is the opposite and determines the distance after which the sound no longer
        // decreases in volume. The default value for this property is 1 billion meters, which is well beyond 
        // hearing range anyway. To avoid unnecessary processing, you should set this value to a reasonable value 
        // and set the Mute3DAtMaximumDistance property of the BufferDescription to true.

        // Finally, we can also specify values for the sound cone if the sound is directional. A sound cone is 
        // almost identical to the cone produced by a spotlight (see article 6). It consists of a set of angles, 
        // one for the inside and one for the outside cone, and orientation, and an outside volume property.
        // Check out the DirectX documentation for more detail on sound cones.
        // http://blogs.msdn.com/coding4fun/archive/2006/11/06/999786.aspx
        //
        // The DopplerFactor and RolloffFactor properties are a number between 0 and 10.
        // Zero means that the value is turned off. One represents the real world values of these acoustic effects. 
        // All other values are multiples, so that a 2 means doubling the real world effect, 3 means tripling it, and so on.
        // http://blogs.msdn.com/coding4fun/archive/2006/11/06/999786.aspx
        internal SoundNode2D(string id) : base (id)
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
        
        internal AudioClip AudioClip2D 
        {
        	get 
        	{
        		return mAudioClip;
        	}
        }
        
        #region IGroup Members
        internal void AddChild(AudioClip child)
        {
        	if (child == null) throw new ArgumentNullException("SoundNode3D.AddChild() - child cannot be null.");
        	
        	base.AddChild (child);
        	mAudioClip = child;
        }
        
        public override void RemoveChild(Node child)
		{
        	if (child as AudioClip3D == null) throw new Exception();
        	mAudioClip = null;
        	base.RemoveChild(child);
		} 
        #endregion
        

        public void Play(Vector3d translation)
        {
        	// TODO: the following might be needed if we want sounds to play when the app window loses focus 
        	// BufferDescription bd;
        	// bd.Flags = BufferDescriptionFlags.GlobalFocus
        	// 
        	this.Play (translation, 0, BufferPlayFlags.Default );
        }
        
        public void Play(Vector3d translation, int priority, BufferPlayFlags flags)
        {
            mAudioClip.Play(priority, flags);
        }

        #region IDisposable
		protected override void DisposeManagedResources()
		{
			base.DisposeManagedResources();
		} 
		#endregion
    }
}