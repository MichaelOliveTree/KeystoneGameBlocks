using Settings;
using System;
using System.Collections.Generic;


namespace Game01.Rules
{
    public static class Processors
    {
        // TODO: This may require that we have already loaded in the user defined types
      
      
      
        // TestUserProcessor() must match delegate in KeyCommon.Processors.DataProcessors.class
        //     public delegate void Processor<T>(ComponentStore<T> store, object parameters, Random r);
        //     but we may adjust it to
        //     public delegate void Processor<T>(ComponentStore<T> store, object parameters, Scene scene, Random r,  GameTime gt);
        //              we may pass in an "int seed" instead of a "Random r"
        DataProcessors.Processor p = Game01.Rules.Processors.TestUserProcessor;
        
        //mIntrinsicProcessors = new KeyCommon.Processors.DataProcessors();
        //mIntrinsicProcessors.Add("STEER", p);
        // then -> p[i].Invoke(store, parameters, r);
        
        
        public static void TestUserProcessor(ComponentStore<TestStruct> store, object parameters, Random r)
        //private static void TestIntrinsicProcessor(ComponentStore<TestStruct> store, object parameters, Scene scene, GameTime gt)
        {
      			System.Diagnostics.Debug.WriteLine("Game01.Rules.Processors.TestUserProcessor() - RUNNING.");
      			Console.WriteLine("Game01.Rules.Processors.TestUserProcessor() - RUNNING.");
            //Entities[] entities = scene.ActiveEntities;
            //Memory<TestStruct> mem = store.Components.Slice(0, store.Components.Length);
            Span<TestStruct> ts = store.Span;
      	
            for (int i = 0; i < (int)store.Size; i++)
            {
        	      int rIndex = r.Next (0, (int)store.Size);
        				
        				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - EntityID = Mem[i]'" + mem.Span[i].EntityID + "'");
        				//Console.WriteLine("TestIntrinsicProcessor() - EntityID = Span[i]'" + ts[i].EntityID + "'");
        				
        				
        				ts[i].X *= 2f;
        				ts[i].X *= ts[rIndex].X * FRACTION;
        				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - X = Mem[i]'" + mem.Span[i].X.ToString() + "'");
        				//Console.WriteLine("TestIntrinsicProcessor() - X = Span[i]'" + ts[i].X.ToString() + "'");
        				rIndex = r.Next (0, (int)store.Size);
        				ts[i].Y *= 2f;
        				ts[i].Y *= ts[rIndex].Y * FRACTION;
        				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - Y = Mem[i]'" + mem.Span[i].Y.ToString() + "'");
        				//Console.WriteLine("TestIntrinsicProcessor() - Y = Span[i]'" + ts[i].Y.ToString() + "'");
        				rIndex = r.Next (0, (int)store.Size);
        				ts[i].Z *= 2f;
        				ts[i].Z *= ts[rIndex].Z * FRACTION;
        				//System.Diagnostics.Debug.WriteLine("TestIntrinsicProcessor() - Z = Mem[i]'" + mem.Span[i].Z.ToString() + "'");
        				//Console.WriteLine("TestIntrinsicProcessor() - Z = Span[i]'" + ts[i].Z.ToString() + "'");
        				
        				// Verify editing the span above is also modifying the underlying memory
        				//System.Diagnostics.Debug.Assert(ts[i].Equals(mem.Span[i]));
            }
      			
        			System.Diagnostics.Debug.WriteLine("Game01.Rules.Processors.TestUserProcessor() - COMPLETED.");
        			Console.WriteLine("Game01.Rules.Processors.TestUserProcessor() - COMPLETED.");
          }
        
        
        
    }
}
 