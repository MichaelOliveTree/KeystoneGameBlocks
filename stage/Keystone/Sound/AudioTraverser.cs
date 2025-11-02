using System;

namespace Keystone.Sound
{
	/// <summary>
	/// Scenegraph traverser that focuses on finding SoundNode3D and SoundNode2D.
	/// The Listener as with Camera is always at 0,0,0 and sound nodes
	/// must be translated to "Listener space."
	/// 
	/// X3D notes:
	/// The location field determines the location of the sound emitter in the 
	/// local coordinate system. A Sound node's output is audible only if it is 
	/// part of the traversed scene. Sound nodes that are descended from LOD, 
	/// Switch, or any grouping or prototype node that disables traversal 
	/// (i.e., drawing) of its children are not audible unless they are traversed. 
	/// If a Sound node is disabled by a Switch or LOD node, and later it becomes 
	/// part of the traversal again, the sound shall resume where it would have 
	/// been had it been playing continuously.
	/// 
	/// </summary>
	public class AudioTraverser
	{
		private int mCurrentPosition; // or is this a float? we need to be able to resume play as described in class Summary
		public AudioTraverser()
		{
		}
		
		
		void Apply (SoundNode2D sound)
		{
			
		}
		
		void Apply (SoundNode3D sound)
		{
			
			// if the sound volume is already playing, ignore it
			
			// otherwise, if we've entered the range of this sound
			// and it's not playing, start to play it.
		}
		
	}
}
