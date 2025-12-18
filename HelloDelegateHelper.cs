// I recall having a separate module for it that was exe specific 
//
// make selector.cs our base type for switch and sequence nodes. maybe base type for any type for any type of switch such as appearance switch node.
// (NOTE: We CANNOT DO THAT RENAME since we have a Behavior Tree node named "Selector")

// recall our Switch nodes we want to use linear interpolation to determine which node to select 
// todo: look at different types of interpolation functions that are weighted differently 
//
//   e.g. health amount as determinant for which damage model node to select 
//         minValue = 0
//         maxValue = 100
//
// indeed, it was for the Selector node.  and the delegate to use could be selected from the plugin.
// We added a variable to the Selector node to serialize the Module.Class.MethodName so that it can loaded on Deserialization
//
// int selection index =  Keystone.Utilities.InterpolationHelper.MapValue(minHealth, maxHealth, minIndex, maxIndex, currentHealth);

//
// delegate int SelectModelDelegate (Entity entity);
//
// SelectModel mSelectDelegate;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;



public class Test
{
    
    static void Main()
    {
        string fullyQualifiedName = "HelloWorld.SciFiCommand_Selection_Delegates.SelectDamageModelBasedOnHealth";
        object oTestFQ = DelegateHelper.GetByFullyQualifiedName(fullyQualifiedName, typeof(DelegateHelper.SelectModelDelegate));
        DelegateHelper.SelectModelDelegate d1 = (DelegateHelper.SelectModelDelegate)oTestFQ;
        Console.WriteLine(d1.Invoke(null));
        
        
        string moduleName = "HelloWorld.exe";
        string className = "SciFiCommand_Selection_Delegates"; //"SciFiCommand_Delegates"; //"game01.dll";
        string methodName = "SelectDamageModelBasedOnHealth";
        
        object o = DelegateHelper.GetByName(moduleName, className, typeof(DelegateHelper.SelectModelDelegate), methodName);
        DelegateHelper.SelectModelDelegate d2 = (DelegateHelper.SelectModelDelegate)o;

        Console.WriteLine(d2.Invoke(null));
    }
}

public class SciFiCommand_Selection_Delegates
{
    public static int SelectDamageModelBasedOnHealth (object entity)
    {
        return 3;
      
    }
}


// KeyCommon.dll
    public class DelegateHelper
    {
         
         public delegate int SelectModelDelegate(object obj);
    
    
        /// <summary>
        /// Returns an Assembly that is already loaded, based on it's name
        /// </summary>
        public static Assembly GetAssembly(string name)
        {
            // linkq version
            //return AppDomain.CurrentDomain.GetAssemblies()
            //      .FirstOrDefault(a => a.GetName().Name == assemblyName);
                
            Assembly[] asm = AppDomain.CurrentDomain.GetAssemblies();
            
            for (int i = 0; i < asm.Length; i++)
              if (asm[i].GetName().Name == name)
                return asm[i];
              
            return null;
              
            // Get the currently executing assembly
            //Assembly currentAssembly = Assembly.GetExecutingAssembly();
            //return currentAssembly;
            
        }
    
        public static object GetByFullyQualifiedName (string fullyQualifiedName, Type type)
        {
            string[] names = fullyQualifiedName.Split(".");
            
            return GetByName(names[0], names[1], type, names[2]);
        }
      
        ///<summary>
        ///
        ///</summary>
        public static object GetByName (string moduleName, string className, Type type, string methodName)
        {
            // Get the currently executing assembly
            // Often it will be the NameSpace, but if no namespace is used, it will be the EXE or DLL name without the file extension
            Assembly currentAssembly = GetAssembly("HelloWorld"); //Assembly.GetExecutingAssembly();
            if (currentAssembly == null) throw new Exception ("GetByName() - Assembly is NULL.");
            Console.WriteLine("CurrentAssembly.Name == " + currentAssembly.GetName());
            
            Module[] allMods = currentAssembly.GetModules();
            for (int i = 0; i < allMods.Length; i++)
              Console.WriteLine("Module.Name == " + allMods[i].Name);
            
            // this will be a .DLL or .EXE
            Module module = currentAssembly.GetModule(moduleName);
            if (module == null) throw new Exception ("DelegateHelper.GetByName() - Module is NULL.");
            
            className = System.IO.Path.GetFileNameWithoutExtension(moduleName) + "." + className;
            Console.WriteLine("ClassName == " + className);
            
            // NOTE: the following call module.GetType() is failing on Online Compiler only I suspect
            Type classType = module.GetType(className, true, true);
            Console.WriteLine("Class.Name == " + classType.Name);
            
            
            Type[] allTypes = module.GetTypes();
            for (int i = 0; i < allTypes.Length; i++)
            {
                Console.WriteLine("Class.Name == " + allTypes[i].Name);
                if (allTypes[i].Name == className)
                  classType = allTypes[i];
                  break;
            }
            
            
            if (classType == null) throw new Exception ("DelegateHelper.GetByName() - classType is NULL.");
            
            // TODO: get just the method's that have the correct signature (aka parameters and return type)
            MethodInfo method = classType.GetMethod(methodName);
            if (method == null) throw new Exception ("DelegateHelper.GetByName() - MethodInfo is NULL.");
            
           // creates a delegate for a static method, NOT an instance method (eg. from a class that is instantiated)
            return Delegate.CreateDelegate(type, method);
        }
         
        public static object GetByName(object target, string methodName)
        {
            MethodInfo method = target.GetType()
                .GetMethod(methodName, 
                           BindingFlags.Public 
                           | BindingFlags.Instance 
                           | BindingFlags.FlattenHierarchy);
    
            // Insert appropriate check for method == null here
    
            return Delegate.CreateDelegate (target.GetType(), target, method);
        }
    
          // use like this ->  var methods =  this.GetType().GetMethodsBySig(typeof(void), typeof(int), typeof(string));
        public static IEnumerable<MethodInfo> GetMethodsBySig(Type type, Type returnType, params Type[] parameterTypes)
        {
            return type.GetMethods().Where((m) =>
            {
                if (m.ReturnType != returnType) return false;
                var parameters = m.GetParameters();
                
                if ((parameterTypes == null || parameterTypes.Length == 0))
                    return parameters.Length == 0;
                if (parameters.Length != parameterTypes.Length)
                    return false;
                    
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameters[i].ParameterType != parameterTypes[i])
                        return false;
                }
                return true;
            });
        }
        
    }