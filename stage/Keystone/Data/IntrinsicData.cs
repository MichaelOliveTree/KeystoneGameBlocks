using Keystone.Types;

namespace Keystone.Data
{
    
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformData
    {
        public Vector3d Translation;
        public Vector3d Scale;
        public Quaternion Rotation;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct BoundsData
    {
        public Vector3d Min;
        public Vector3d Max;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsData
    {
        public Vector3d mVelocity;
        public Vector3d mAcceleration;
        public Vector3d mForce;
        public Vector3d mAngularVelocity;
        public Vector3d mAngularAcceleration;
        public Vector3d mAngularForce;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct BasicEntityData
    {
        public int UserDataIndex;
        public Transform Transform;
        public Bounds Bounds;
        
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct AdvancedEntityData
    {
        public BasicEntityData BasicData;
        public PhysicsData Physics;
        
    }
}