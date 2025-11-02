using System;

namespace Game01.Rules
{
    // Delegate signature used is in KeyCommon.DelegateHelper.cs
    // it currently looks like this: delegate int SelectorNodeDelegate(object obj);
    
    // game01.dll
    
    /// <summary>
    /// Used to select the index of a "damage" Model under a ModelSelector switch
    /// based on the current "health" of the Entity
    /// </summary>
    public class SciFiCommand_Selection_Delegates
    {
        public static int SelectBasedOnHealth (Entity entity)
        {
            ModelSelector selector = entity.Selector;
            int arrayIndexMin = 0;
            int arrayIndexMax = selector.ChildCount - 1;
            
            int currentHealth = entity.GetHealth();
            int minPossibleHealth = 0;
            int maxPossibleHealth = 100;

            // TODO: Keystone.Utilities is actually KeyStandardLibrary.Utilities but we use the Keystone namespace
            //       still.  I need to change this because it's too confusing.
            int selectedIndex =  Keystone.Utilities.InterpolationHelper.MapValue(arrayIndexMin, arrayIndexMax, 
                                                                                 minPossibleHealth, maxPossibleHealth, currentHealth);
          
            return selectedIndex;
          
        }
    }
}




