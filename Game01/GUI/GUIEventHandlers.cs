using System;
using Keystone.Events;
using Keystone.Entities;

namespace Game01.GUI
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
      
      
}