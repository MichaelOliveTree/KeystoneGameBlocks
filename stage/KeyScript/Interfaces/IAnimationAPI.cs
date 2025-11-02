using System;

namespace KeyScript.Interfaces
{
	/// <summary>
	/// Description of IAnimationAPI.
	/// </summary>
	public interface IAnimationAPI
	{
		void Animation_Play(string entityID, string animationName, bool loop = false);
        float Animation_GetLength(string entityID, string animationName);

		
	}
}
