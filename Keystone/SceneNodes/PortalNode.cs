using System;
using Keystone.Traversers;
using Keystone.Elements;

namespace Keystone.SceneNodes
{
    public class PortalNode : EntityNode 
    {
        public PortalNode(Portal portal) : base (portal)
        {
        }

        /// <summary>
        /// Primary reason for PortalNode is just to have the unique Traverse handler
        /// </summary>
        /// <param name="target"></param>
        public override object Traverse(ITraverser target, object data)
        {
            return target.Apply(this, data);
        }

        protected override void UpdateBoundVolume()
        {
            base.UpdateBoundVolume();
        }
    }
}