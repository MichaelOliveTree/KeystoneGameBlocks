using System;
using Keystone.Types;
using KeyScript;
using KeyScript.Interfaces;
using KeyScript.Host;

namespace KeyEdit.Scripting
{
	/// <summary>
	/// Description of AnimationAPI.
	/// </summary>
	public class AnimationAPI : IAnimationAPI
	{
		void IAnimationAPI.Animation_Play (string entityID, string animationName, bool loop = false)
		{
			Keystone.Entities.Entity ent = EntityAPI.GetEntity(entityID);

            if (ent == null) return;
            if (ent.Animations == null)
            {
                System.Diagnostics.Debug.WriteLine("AnimationAPI.Animation_Play() - ERROR: Animations is NULL");
                return;
            }
			ent.Animations.Play (animationName, loop);
		}

        float IAnimationAPI.Animation_GetLength(string entityID, string animationName)
        {
            Keystone.Entities.Entity ent = EntityAPI.GetEntity(entityID);

            if (ent == null || ent.Animations == null) return 0;

            return ent.Animations.GetLength(animationName);
        }
	}
}
