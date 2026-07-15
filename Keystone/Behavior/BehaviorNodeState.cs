using System;
using KeyCommon.Traversal;

namespace Keystone.Behavior
{
    internal enum BehaviorFlags : byte
    {
        None = 0,
        Enabled = 1 << 0,
        Activated = 1 << 1,  // indicates the node is part of the selected path last iteration through the behavior tree
        All = Byte.MaxValue
    }

    // http://altdevblogaday.org/2011/04/24/data-oriented-streams-spring-behavior-trees/
    // note how in the above article it talks about creating a flat array of tokens
    // rather than any nested structure as we use here.
    // Hrm...
    // Data-Oriented Design (DOD) for Behavior Trees (BTs) decouples logic from data. 
    // Instead of using traditional object-oriented trees with heavy node classes, agents 
    // process tree logic as linear data streams. This "springs" forth highly efficient, 
    // cache-friendly evaluations, eliminating the random memory access typical of classic 
    // implementations.The Architecture: Streams, Springing, and TreesA Data-Oriented 
    // Behavior Tree framework separates the system into three distinct parts:1. The 
    // Shape (The Data Stream)Instead of nested objects, the tree's structure is flattened 
    // into a 1D array (the stream).Nodes are categorized as pure Data Items (Branches/Deciders 
    // or Action Leaf Nodes).Because the tree is a contiguous block of memory, the interpreter 
    // streams through it rapidly without triggering cache misses.2. The Actor ContextThe actual 
    // "State" of the AI is pulled out of the node and placed inside an Actor Data Structure.
    // The tree engine acts as a stateless interpreter that reads an entity’s current 
    // state (e.g., location, health) and the tree’s shape.Evaluation happens by sending a 'tick' 
    // from the root through the data stream, triggering success, failure, or running states.3. 
    // Execution & BufferingInterpreter data tracks which actions are currently executing for the agent.
    // This pattern acts dynamically; during an update tick, the engine buffers new states, applying 
    // changes on the fly without locking up objects in deep inheritance chains.
    // Why This Approach is 
    // So PowerfulCache Locality: By traversing a flat stream of data, modern CPU hardware is 
    // highly leveraged, improving iteration times for complex NPCs.
    // Live Editing & Streaming: 
    // Since the logic is nothing more than flat data, you can stream new behavior tree layouts 
    // dynamically from an editor directly to the running application.
    // Decoupled Action Execution: Leaf actions simply transform entity data (e.g., changing 
    // destination based on threat levels) making it easier to scale logic to thousands of agents 
    // simultaneously.For further deep dives into this paradigm, you can explore the classic 
    // Game Developer In-Depth: Behavior Tree Entrails article or read research on Parameterizing 
    // Behavior Trees for large-scale environments.Could you tell me what programming language or 
    // engine you are using (e.g., C++, C#, Unreal, or Unity) and how many active agents you need 
    // to support? I can provide specific implementation tips or library recommendations.
    //
    /*
     Representing a behavior tree as a 1D array involves "flattening" the hierarchical tree structure
      into a contiguous array, usually ordered via depth-first traversal. This data-oriented approach 
      eliminates dynamic memory allocation (heap fragmentation) and significantly improves CPU cache 
      efficiency during tree execution.How the Flattening WorksThe behavior tree is evaluated sequentially. 
      Since tree traversal naturally explores children before siblings, we can serialize the tree into an array. 
      There are two main approaches to store node relationships in a 1D format:1. The Child-Count / Index 
      MethodEach node is allocated variable cells or a struct in the array. The first cell dictates the 
      number of children, and the immediately following cells store the array indices of those children.
      Example structure: [Node Type, Number of Children, Child 1 Index, Child 2 Index]
      Evaluation: You tick the Root node at index 0. The execution evaluates the node and recursively 
      checks the child indices stored in the array.2. The Flat Stack / Implicit Traversal MethodThis is 
      heavily used in high-performance data-oriented engines. You discard standard parent-child pointers 
      and simply list the nodes in their exact execution order. To know how far to skip when a node fails 
      or succeeds, each node is paired with an integer representing the end of its subtree.
      Example array: [Selector, Action A, Action B, Action C]Metadata array: You accompany the node 
      with metadata specifying its children or its subtree end index (e.g., "If Action A fails, skip to 
      index 3").Evaluation: You iterate or jump linearly across the array based on the Running, Success, or 
      Failure state of each ticked node.Code Example: Flat Tree Node StructIn languages like C++ or C#, you 
      would define a flattened node for a stack-based array like this:
      
      cstruct BTNode {
            NodeType type;          // e.g., SEQUENCE, SELECTOR, ACTION
            int childCount;         // Number of immediate children
            int firstChildIndex;    // Index of the first child in the 1D array
            int parentIndex;        // Index of the parent node
            int subtreeSize;        // Skip offset to jump over this entire branch
            // (Plus a way to link to the actual execution logic)
        };
    
    Use code with caution.Key ConsiderationsFixed Structure: Because arrays are rigid, this pattern works 
    best for static behavior trees that do not need nodes added or deleted mid-game.Blackboards: A 1D array 
    only holds the tree structure. All state parameters (e.g., target coordinates, health) must be stored in
     a separate, globally accessible memory space known as a Blackboard.
*/


// https://web.archive.org/web/20241225111505/http://archive.vector.org.uk/art10500340

// https://medium.com/@gautamv/advanced-behavior-tree-structures-4b9dc0516f92


        //struct BTPreviousRun
        //{
        //    public int LastIndex;

        //}

        //NodeState[] mBTNodeStates;
        //NodeData[] mBTNodeData;      // eg. entityIndex + struct name + key names for instance for looking up key values into the ComponentStoreCollection
        //Func <BTPreviousRun previousInfo, Keystone.Entities.Entity, double, BehaviorResult>[] mBTNodeLogic;


        /* flat array nodes  <-- note: it does not contain a function pointer to the code logic and uses
        //                    a seperate BehaviorTree class below to iterate through the nodes array
        public enum NodeType { Selector, Sequence, Action }
        public enum NodeState { Success, Failure, Running }

        public struct BTNode
        {
            public NodeType Type;
            public NodeState State;  // MichaelOliveTree - the State data can too be placed in a array that shares the same indices along with a ComponentStoreCollection for the Entities
            public int ChildIndex;  // Start of children
            public int ChildCount;  // Number of children
            public int CustomDataIndex; // Index to a separate flat array of node variables
        }

        public class BehaviorTree
        {

            public int Create(NodeType t)
            {
                
            }



            // The flat array containing all nodes
            public BTNode[] nodes;
            
            public NodeState Tick(int nodeIndex)
            {
                BTNode node = nodes[nodeIndex];
                
                switch (node.Type)
                {
                    case NodeType.Action:
                        // Execute action logic here (e.g., MoveTo, Attack)
                        return node.State;

                    case NodeType.Sequence:
                        for (int i = 0; i < node.ChildCount; i++)
                        {
                            int childIndex = node.ChildIndex + i;
                            NodeState result = Tick(childIndex);
                            if (result != NodeState.Success) return result;
                        }
                        return NodeState.Success;

                    case NodeType.Selector:
                        for (int i = 0; i < node.ChildCount; i++)
                        {
                            int childIndex = node.ChildIndex + i;
                            NodeState result = Tick(childIndex);
                            if (result != NodeState.Failure) return result;
                        }
                        return NodeState.Failure;
                        
                    default:
                        return NodeState.Failure;
                }
            }
        }
        */

        /*
            Starting in C# 9, fast, raw function pointers are supported natively via the delegate* syntax. Unlike traditional C# delegates (like Func<T> or Action), function pointers emit the ultra-efficient calli (call indirect) IL opcode. This bypasses virtual dispatch, avoids heap allocations, and eliminates garbage collection (GC) overhead completely.Enabling Function PointersBecause function pointers handle raw memory addresses, they require an unsafe context.Open your .csproj file and add the AllowUnsafeBlocks tag:xml<PropertyGroup>
            <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
            </PropertyGroup>
            Use code with caution.Wrap your code inside an unsafe block or mark your method/class as unsafe.Managed Function Pointers (delegate* managed<...>)You can create a pointer to a static managed method using the address-of (&) operator. The final type parameter in the angle brackets always specifies the return type.csharppublic unsafe class PerformanceMath
            {
                // A standard static method
                public static int Multiply(int a, int b) => a * b;

                public void Execute()
                {
                    // Declaring a function pointer: takes two ints, returns an int
                    delegate* managed<int, int, int> ptr = &Multiply;

                    // High-speed execution with zero allocations
                    int result = ptr(5, 4); 
                    Console.WriteLine(result); // 20
                }
            }
            Use code with caution.Unmanaged Function Pointers (delegate* unmanaged<...>)When optimizing code for Native Interop (P/Invoke) to communicate with C/C++ or Rust libraries, use unmanaged function pointers. You can also specify exact native calling conventions inside brackets, such as Stdcall or Cdecl.csharpusing System.Runtime.InteropServices;

            public unsafe class NativeInterop
            {
                // Expose a managed method to native code safely
                [UnmanagedCallersOnly(CallConventions = new[] { typeof(CallConvCdecl) })]
                public static int NativeAdd(int a, int b) => a + b;

                public void Run()
                {
                    // Pointer explicitly using unmanaged Cdecl convention
                    delegate* unmanaged[Cdecl]<int, int, int> nativePtr = &NativeAdd;
                    
                    int result = nativePtr(10, 20);
                }
            }
            
            Use code with caution.Delegate vs. Function Pointer ComparisonFeatureDelegates (Func, Action, delegate)
            Function Pointers (delegate*)Memory AllocationAllocates an object on the managed heapZero allocations 
            (lives on the stack/registers)GC PressureTriggers Garbage Collection over timeNo GC tracking or pressure
            IL Opcode
            Uses callvirt (slower virtual call)Uses calli (fast raw address hop)Instance Methods
            Supported naturally (myObj.Method)Restricted (strictly designed for static methods)Safety 
            TypeSafe managed codeUnsafe (can cause memory crashes if misused)Important Rules & LimitationsNo 
            Instance Methods Directly: Function pointers cannot cleanly point to non-static instance methods because 
            they do not implicitly capture the object's this context.No Closures: They cannot be used with lambdas that 
            capture external variables.Use for Hot Paths: Only substitute delegates for function pointers in tight loops, 
            math engines, parsing algorithms, or low-level wrappers where invocation overhead is measurable.
        */


        /*
        using Node = std::function<Status()>;

        // --- Control Flow ---

        Node Sequence(std::vector<Node> children) {
            return [children]() {
                for (const auto& child : children) {
                    Status status = child();
                    if (status == Status::RUNNING || status == Status::FAILURE) {
                        return status;
                    }
                }
                return Status.SUCCESS;
            };
        }

        Node Selector(std::vector<Node> children) {
            return [children]() {
                for (const auto& child : children) {
                    Status status = child();
                    if (status == Status::RUNNING || status == Status::SUCCESS) {
                        return status;
                    }
                }
                return Status.FAILURE;
            };
        }

        // --- Leaf Actions ---

        struct Agent {
            int battery;
            int ammo;
        };

        Node CheckBattery(Agent& agent) {
            return [&agent]() {
                return (agent.battery > 20) ? Status::SUCCESS : Status::FAILURE;
            };
        }

        Node AttackEnemy(Agent& agent) {
            return [&agent]() {
                if (agent.ammo > 0) {
                    agent.ammo--;
                    std::cout << "Attacking enemy! Pew pew.\n";
                    return Status.SUCCESS;
                }
                return Status.FAILURE;
            };
        }

        Node Flee() {
            return []() {
                std::cout << "Fleeing to safety...\n";
                return Status.SUCCESS;
            };
        }

        // --- Execution ---

        int main() {
            Agent my_agent { .battery = 15, .ammo = 5 };

            // Build the behavior tree closure
            Node behavior_tree = Selector({
                Sequence({
                    CheckBattery(my_agent),
                    AttackEnemy(my_agent)
                }),
                Flee()
            });

            // Tick the tree
            behavior_tree();

            return 0;
        }
        */

    internal class BehaviorNodeState
    {
        // TODO: remove codeplex BehaviorTree from keystone solution... i dont use it afterall.
        private BehaviorResult mLastResult; 
        private BehaviorFlags mFlags;
        private BehaviorNodeState[] mChildren; // TODO: i forget, but why do we have child node states?  isn't this just more a traversal state?
        // private Parameters[] mParameters;   TODO: parameters must be part of the state since they can't be persisted by the Behavior node
                                               // because those nodes are designed to be free of state info so that they can be shared

        // indicates whether the current node was already activated last tick and thus
        // can be used to determine if OnEnter needs to be called and whether
        // if de-activated in the current turn, OnExit needs to be invoked
        public bool IsActivated
        {
            get { return ((mFlags & BehaviorFlags.Activated) == BehaviorFlags.Activated); }
            set { mFlags |= BehaviorFlags.Activated; }
        }

        public bool Enabled
        {
            get { return ((mFlags & BehaviorFlags.Enabled) == BehaviorFlags.Enabled); }
            set { mFlags |= BehaviorFlags.Enabled; }
        }

        public BehaviorResult LastResult
        {
            get { return mLastResult; }
        }

        public BehaviorNodeState[] Children
        {
            get { return mChildren; }
        }

        public void AddChild(BehaviorNodeState child)
        {
            int length = 0;
            if (mChildren != null)
                length = mChildren.Length;

            BehaviorNodeState[] tmp = new BehaviorNodeState[length + 1];
            
            if (length > 0)
                mChildren.CopyTo (tmp, 0);

            tmp[length] = child;
            mChildren = tmp;
        }

        public void RemoveChild()
        {
 
        }
    }
}
