using System;
using System.Collections.Generic;
using Keystone.Traversers;


namespace Keystone.Portals
{
    public class GUINode : SceneNodes 
    {

        public GUINode(Controls.Control2D control)
            : base(control)
        {}

        #region Node Members
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion
    }
}
