using System;
using Keystone.Types;


namespace Keystone.Culling
{
    public struct FrustumInfo
    {
        public ViewFrustum Frustum;
        public Matrix Projection;
        public double Near;
        public double Far;
    }
}
