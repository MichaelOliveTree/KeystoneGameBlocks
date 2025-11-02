using System;
using Keystone.Events;
using Keystone.Entities;

namespace Keystone.GUI
{
  // GUI EventHandlers will typically reside off of game01//GUI//EventHandlers.csharp
  //
    /*
    You can restore an EventHandler from a string in C# using reflection. Because a string is a simple data type and a delegate (like EventHandler) is a type-safe function pointer, you cannot directly cast or convert a string to a delegate. 
  The general process is to use reflection to find the method that corresponds to the method name stored in the string, and then create a delegate from that MethodInfo object. 
  Steps to restore an EventHandler from a string
  1. Define the handler method
  First, ensure you have a method with a signature that matches the delegate you want to create (e.g., EventHandler or EventHandler<TEventArgs>). 
  csharp
public class MyEventHandlerClass
{
    // The method to be referenced by the event handler
    public static void MyHandlerMethod(object sender, EventArgs e)
    {
        Console.WriteLine("Event handled by a static method.");
    }

    public void MyInstanceHandlerMethod(object sender, EventArgs e)
    {
        Console.WriteLine("Event handled by an instance method.");
    }
}
2a. Get the MethodInfo using reflection
  Use Type.GetMethod() to retrieve the MethodInfo for the method based on its name and binding flags. 
  csharp
  // The string representing the method name.
  string handlerMethodName = "MyHandlerMethod";
  string instanceHandlerMethodName = "MyInstanceHandlerMethod";
  
  // Get the Type containing the method.
  Type handlerType = typeof(MyEventHandlerClass);
  
  // Use reflection to get the MethodInfo for the static method.
  MethodInfo staticMethodInfo = handlerType.GetMethod(handlerMethodName, 
      BindingFlags.Public | BindingFlags.Static);
  
  2b // Get the MethodInfo for the instance method (requires an instance).
  MyEventHandlerClass myInstance = new MyEventHandlerClass();
  MethodInfo instanceMethodInfo = handlerType.GetMethod(instanceHandlerMethodName,
      BindingFlags.Public | BindingFlags.Instance);

3. Create the delegate from MethodInfo
  The Delegate.CreateDelegate() method creates a delegate instance from a MethodInfo. It requires the delegate type and, for instance methods, the target object. 
  csharp
  // Create a delegate for the static method.
  // No target object is needed for a static method.
  Delegate staticDelegate = Delegate.CreateDelegate(
      typeof(EventHandler), 
      staticMethodInfo);
  
  // Create a delegate for the instance method.
  // Pass the instance of the class as the target object.
  Delegate instanceDelegate = Delegate.CreateDelegate(
      typeof(EventHandler),
      myInstance,
      instanceMethodInfo);
4. Attach the delegate to an event
  Once you have the delegate, you can use it to subscribe to an event. 
  csharp
  // Create a class with an event to test.
  public class EventPublisher
  {
      public event EventHandler MyEvent;
  
      public void RaiseEvent()
      {
          MyEvent?.Invoke(this, EventArgs.Empty);
      }
  }
  
  public static void Main()
  {
      var publisher = new EventPublisher();
  
      // The delegate created from the static method string
      publisher.MyEvent += (EventHandler)staticDelegate;
  
      // The delegate created from the instance method string
      publisher.MyEvent += (EventHandler)instanceDelegate;
  
      // Raise the event to see both handlers work
      publisher.RaiseEvent();
  }
  Potential issues and considerations
  Security: Using reflection can expose methods not intended for external access. If the string input is from an untrusted source, this could create a security vulnerability.
  Performance: Reflection is slower than direct method calls. For performance-critical applications, avoid creating delegates from strings repeatedly. Cache the created delegates if necessary.
  Error handling: The code assumes that the method and class exist and that the method signature is correct. Production code should add error handling for null MethodInfo and casting exceptions.
  Instance vs. static methods: Remember to pass the target object for instance methods. Static methods do not require a target object and you pass null
      */
    
  // IMPORTANT: On the issue of security, we should verify that perhaps the wired events all come from
  // a specific source eg.  my.Exe\GUIHandlers.cs  
      
      
    /// <summary>
    /// A Control is a type of Entity that has event handlers that can be directly wired and invoked.
    /// Thus, Control mimic's windows forms way of handling events.  The intent is that like Forms
    /// you can wire distinct handlers in the EXE for each specific GUI element.
    /// IMPORTANT: As with delegates for ModelSelector nodes to determine which models or nested
    /// ModelSelectors to "select," these EventHanlder's can be assigned from the saved/serialized
    /// eventhandler names.
    /// </summary>
    public class Control : ModeledEntity, IInputCapture // or simply IControl really
    {
        //private enum ControlState
        //{
        //    None,
        //    MouseOver,
        //    Pressed,
        //}

        // note that these events can be handled by a single super controller (e.g. ManipulatorController which is comprised of
        // multiple controls) that can handle the events for multiple controls.  So we dont have a 1:1 controller and control necessarily
        // we can have 1:Many 
        // Note: a controller is needed when a bunch of dependant controls are required to work together.  However in the future perhaps
        // a single type of GroupControl can handle this functionality instead of this concept of a "Controller".  This will make scripting
        // in general more elegant as you don't need these special hardcoded "controller" objects
        public event EventHandler KeyboardCancel;
        public event EventHandler MouseMove;
        public event EventHandler MouseEnter;
        public event EventHandler MouseLeave;
        public event EventHandler MouseDown;
        public event EventHandler MouseUp;
        public event EventHandler MouseClick;
        public event EventHandler MouseDrag;

        protected bool _capture = false;
        protected bool _captureEnabled = true;


       
        public Control(string id)
            : base(id)
        {
        }

        #region ITraverer
        public override object Traverse(Traversers.ITraverser target, object data)
        {
            return target.Apply(this, data);
        }
        #endregion

        public bool InputCaptureEnable { get { return _captureEnabled; } set { _captureEnabled = value; } }
        public bool HasInputCapture {get { return _capture;}}

        // event's that come here are (for now at least) _always_ generated by the engine
        // itself from input management.  Or to put it another way, never user generated.
        // Thus there is no issue of having to map input event names with event handler script names
        // or any such thing.  
        // I've gone back and forth a few times on the value of "controllers" and I think that a controller is
        // in fact necessary for proper Model View IOController seperation.  IOController should not exist
        // in a button, but rather a controller should initialize to map event handlers to controls that exist.
        // Yes, this does beg the question of how do we properly handle that specifically with regard
        // to paging sections in and out and thus controls and their Controllers in and out but that could conceptually
        // for now, be done by proper naming the target entity (control) and the name of the event.
        public virtual void HandleEvent(EventType et, InputCaptureEventArgs args)
        {
            switch (et)
            {
                case EventType.MouseMove:
                    if (_capture)
                    {
                        if (MouseDrag != null)
                        {
                            MouseDrag.Invoke(this, args);
                        }
                    }
                    else if (MouseMove != null) 
                        MouseMove.Invoke(this, args);
                    break;
                case EventType.MouseEnter:
                    // TODO: fix after Entity.Model done-> _appearanceFlags = (int)ControlState.MouseOver;
                    if (MouseEnter != null) MouseEnter.Invoke(this, args);
                    break;
                case EventType.MouseLeave:
                    // TODO: fix after Entity.Model done-> _appearanceFlags = (int) ControlState.None; // TODO: but not if we're currently depresed right?
                    // i think we're ok, cuz when captured we'll never get here from the top of the switch statement
                    if (MouseLeave != null) MouseLeave.Invoke(this,args);
                    break;
                case EventType.MouseLeftClick:
                    if (MouseClick != null) MouseClick.Invoke(this, args);
                    break;
                // TODO: an open question: Should we track our own state for things like rollover?  well somewhat.
                    // i believe if we rely on the events we can just change state directly without intermediate variables
                    // But on the issue of mouse capturing, what windows does is manage which controls have focus and such
                // http://social.msdn.microsoft.com/Forums/en-US/winforms/thread/25df6446-65e7-4118-b236-cfbc350c2687/
                    // so we should do the same.  We can have a default manager for the basic core stuff like enter, leave, down, up, etc
                    // but then the other events we generate which a user will respond too such as Click() are the main ones to be customized
                case EventType.MouseDown :
                    _capture = true;
                    if (MouseDown != null) MouseDown.Invoke(this, args);
                    break;
                case EventType.MouseUp:
                    _capture = false;
                    // TODO: will this false popsitive MouseUp if control never received mouseDown but then a depressed mouse
                    // is hovered over and released?  it shouldnt
                    if (MouseUp != null) MouseUp.Invoke(this, args);
                    if (MouseClick != null) MouseClick.Invoke(this, args);
                    break;
            }
        }
    }
}