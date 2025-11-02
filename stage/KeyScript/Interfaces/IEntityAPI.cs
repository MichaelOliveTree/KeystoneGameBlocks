using System;
using Keystone.CSG;
using Keystone.Types;
using KeyScript.Rules;
using KeyScript.Routes;

namespace KeyScript.Interfaces
{
    public interface IEntityAPI
    {
        // nodes
        void SetFlag(string nodeID, byte flag, bool value);
        void SetFlag (string nodeID, string flagName, bool value);
        bool GetFlag(string nodeID, byte flag);
        bool GetFlag (string nodeID, string flagName);
        string GetName(string nodeID);
        string GetTypename(string nodeID);

        object GetPropertyValue(string entityID, string propertyName);
        void SetPropertyValue(string entityID, string propertyName, object propertyValue);

        // execution of scripted methods in other entities (eg child entities being added)
        object Execute(string entityID, string methodName, object[] args);

        string FindDescendantByName(string nodeID, string name);
        string GetDescendantOfType (string startingNodeID, string descendantTypename);
        string[] GetComponentsOfType(string vehicleID, uint componentClassID);
        

        void SetEntityFlag(string entityID, uint flag, bool value);
        bool GetEntityFlag(string entityID, uint flag);

        // note: custom flag values are stored in the Entity and thus are Not shared.  They are unique per entity instance
        void SetCustomFlagValue(string entityID, UInt32 flag, bool value);
        bool GetCustomFlagValue(string entityID, UInt32 flag);
               
        // note: Component flags reside in the DomainObject and are not per Entity instance
        void SetComponentFlagValue(string domainObjectID, uint flag, bool value);
        bool GetComponentFlagValue(string domainObjectID, uint flag);

        KeyCommon.Data.UserData GetAIBlackboardData(string entityID);
        void SetAIBlackboardData(string entityID, KeyCommon.Data.UserData data);

        Vector3d GetRegionOffsetRelative (string regionA, string regionB);


        // these GetPosition/SetPosition are shortcuts for the general purpose GetProperty/SetProperty functions
        bool EntityValid(string entityID);
        string GetEntitySceneID (string entityID);
        string GetEntityRegionID (string entityID);
        string GetOwnerID(string entityID); // returns the owning container if applicable, else returns null
        string GetParentID (string entityID);
        
        void Spawn (string entityID, string regionID, string prefabRelativePath, Vector3d position);
        
        bool GetVisible (string entityID);
        void SetVisible(string entityID, bool value);
        
        double GetRadius (string entityID);
        double GetFloor(string entityID);
        double GetHeight(string entityID);

        uint GetBrushStyle(string entityID);

        void GetPosition(string entityID, out Vector3d local, out Vector3d position, out Vector3d global);
        Vector3d GetPositionGlobalSpace (string entityID);
        Vector3d GetPositionRegionSpace(string entityID);
        Vector3d GetPositionLocalSpace(string entityID);
        void SetPositionRegionSpace(string entityID, Vector3d position);
        void SetPositionLocalSpace(string entityID, Vector3d position);
        void SetPositionGlobalSpace(string entityID, Vector3d position);

        void GetRotation(string entityID, out Quaternion local, out Quaternion rotation, out Quaternion global);
        Quaternion GetRotationRegionSpace (string entityID);
        Quaternion GetRotationLocalSpace(string entityID);
        Quaternion GetRotationGlobalSpace(string entityID);

        void SetRotationRegionSpace (string entityID, Quaternion rotation);
        void SetRotationGlobalSpace(string entityID, Quaternion rotation);
        void SetRotationLocalSpace(string entityID, Quaternion rotation);

        Vector3d GetVelocity(string entityID);
        void SetVelocity(string entityID, Vector3d velocity);
        Vector3d GetAcceleration(string entityID);
        void SetAcceleration(string entityID, Vector3d acceleration);
        Vector3d GetAngularVelocity(string entityID);
        void SetAngularVelocity(string entityID, Vector3d velocity);
        Vector3d GetAngularAcceleration(string entityID);
        void SetAngularAcceleration(string entityID, Vector3d acceleration);

        // celled region specific
        void CellMap_RegisterObserver(string interiorID, string entityID, string layerName);
        void CellMap_UnregisterObserver(string interiorID, string entityID, string layerName);
        uint CellMap_GetCellIndexFromWorldPosition(string interiorID, string entityID);
        uint CellMap_GetCellIndexFromWorldPosition(string interiorID, Vector3d position);
        Vector3d CellMap_GetCellPosition(string interiorID, uint cellID);

        Vector3d CellMap_GetTileSnapPosition(string interorID, string childID, Vector3d position, byte rotation);
        Vector3d CellMap_GetTilePosition3D(string entityID, Vector3i tileLocation);
        Vector3d CellMap_GetTileSize(string entityID);
        uint[] CellMap_GetMapDimensions(string entityID);
        Vector3d CellMap_GetCellSize(string entityID);
        Vector3d CellMap_GetEdgePosition (string interiorID, Vector3d position, out Vector3d rotation, out int edgeID);
        int[] CellMap_GetEdgeAdjacents(string interiorID, int edgeID, bool parallelsOnly);
        bool CellMap_EdgeHasWall(string interiorID, int edgeID);
        CellEdge CellMap_GetEdge(string celledRegionID, uint edgeID);
        uint[] CellMap_Unflatten(string celledRegionID, uint cellID);
        //Vector3d CellMap_GetOriginOffset(string entityID);
        float[] CellMap_GetStartIndices(string entityID);
        void CellMap_UpdateLinkNetwork(string entityID, uint tileID, string layerName, bool value);
        object CellMap_GetTileSegmentState (string entityID, uint cellID);
        void CellMap_SetTileSegmentState (string entityID, uint cellID, object segment_state);
        void CellMap_SetTileSegmentStyle(string entityID, uint edgeID, object style);
        object CellMap_GetEdgeSegmentState(string entityID, uint edgeID);
        void CellMap_SetEdgeSegmentState(string entityID, uint edgeID, object segment_state);
        void CellMap_SetEdgeSegmentStyle(string entityID, uint edgeID, object style);

        void CellMap_SetDataLayerValue(string entityID, string layerName, uint elementIndex, bool value);
        object CellMap_GetDataLayerValue(string entityID, string layerName, uint elementIndex);

        //void CellMap_SetCellBoundsValue(string entityID, uint cellID, bool value);
        void CellMap_SetFloorCollapseState(string entityID, uint cellIndex, bool unCollapsed);
        void CellMap_SetCeilingCollapseState(string entityID, uint cellIndex, bool unCollapsed);
        void CellMap_SetFloorAtlasTexture(string entityID, uint cellIndex, uint atlasTextureIndex);
        void CellMap_SetCeilingAtlasTexture(string entityID, uint cellIndex, uint atlasTextureIndex);

        void CellMap_ApplyFootprint(string entityID, uint edgeID, object value);

        // returns the id's of all nearby lights
        string[] GetNearbyLights(string entityID, int maxDirLights, int maxLights, bool priortizeByRange);

            

              

        // Entity Scripting related
        // TODO: the following set of Get/Set property or propertyValue methods
        // should work against any custom or intrinsic property of Entity or an Entity's script
        void AddCustomProperties(string scriptID, Settings.PropertySpec[] properties);
        Settings.PropertySpec[] GetCustomProperties(string entityID, bool specOnly);
        object GetCustomPropertyValue(string entityID, string propertyName);

        void SetCustomPropertyValues(string entityID, string[] propertyNames, object[] values, bool raiseEvent = false);
        
        void SetCustomPropertyValue(string entityID, string propertyName, object value, bool raiseEvent = false);
        void AddRule(string scriptID, string propertyName, Rule rule);
        
        void PropertyChangedEventAdd(string scriptID, string propertyName, KeyScript.Events.PropertyChangedEventDelegate handler);
        
        void PropertyChangedEventSubscribe(string subscriberID, string entityThatGeneratesTheEvent, string eventName, KeyScript.Events.PropertyChangedEventDelegate eventHandler);

        void EventAdd(string scriptID, string eventName, KeyScript.Events.EventDelegate eventHandler);
        
        void EventRaise(string entityThatGeneratedTheEvent, string eventName);
      
        void EventSubscribe(string subscriberID, string entityThatGeneratesTheEvent, string eventName, KeyScript.Events.EventDelegate eventHandler);

        void AnimationEventSubscribe(string entityThatGeneratesTheEvent, string subscriberID, string animationName, KeyScript.Events.AnimatioCompletedEventDelegate eventHandler);

        // production and consumption
        //        void AssignForceProductionHandler(string scriptID, KeyCommon.Simulation.Production_Delegate productionHandler); // ForceProductionHandler is for gravity and engine thrust which must run at high Hz
        void AssignProductionHandler(string scriptID, uint productID, KeyCommon.Simulation.Production_Delegate productionHandler); // UserProduction can run at different Hz for different product types.
        //void CreateProductionStore (string scriptID, string productID, double capacity);
        void CreateConsumption(string scriptID, string productID, uint productType, KeyCommon.Simulation.Consumption_Delegate consumptionHandler);

        void RegisterProduction(string entityID, uint productID);
        void UnRegisterProduction(string entityID, uint productID);

        //void CreateTransmitter(string scriptID, string transmitterName, uint flag);
        //void CreateReceiver(string scriptID, string receiverName, uint flag);
    }
}
