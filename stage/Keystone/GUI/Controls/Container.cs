using System;

namespace Keystone.GUI
{
	public abstract class Container : ModeledEntity, IInputCapture // or simply IContainer really
	{

      public Container(string id)
            : base(id)
      {
        
      }
      
      
      
      
	}
}