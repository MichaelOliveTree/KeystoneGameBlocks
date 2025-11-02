using System;

namespace KeyScript.Events
{
    public delegate void EventDelegate(string entityID, string eventName, object[] args);
    public delegate void PropertyChangedEventDelegate(string entityID);
    public delegate void AnimatioCompletedEventDelegate(string entityID);

    public struct EventSubscribers
    {
        public string[] SubscriberIDs;
        public KeyScript.Events.EventDelegate[] Handlers;

        public void Add(string subscriberID, EventDelegate handler)
        {
            SubscriberIDs = Keystone.Extensions.ArrayExtensions.ArrayAppend(SubscriberIDs, subscriberID);
            Handlers = Keystone.Extensions.ArrayExtensions.ArrayAppend(Handlers, handler);
        }
    }

    public struct PropertyChangedEventSubscribers
    {
        public string[] SubscriberIDs;
        public KeyScript.Events.PropertyChangedEventDelegate[] Handlers;

        public void Add(string subscriberID, PropertyChangedEventDelegate handler)
        {
            SubscriberIDs = Keystone.Extensions.ArrayExtensions.ArrayAppend(SubscriberIDs, subscriberID);
            Handlers = Keystone.Extensions.ArrayExtensions.ArrayAppend(Handlers, handler);
        }
    }

    public struct AnimationCompletedEventSubscriber
    {
        public string[] SubscriberIDs;
        public KeyScript.Events.AnimatioCompletedEventDelegate[] Handlers;

        public void Add(string subscriberID, AnimatioCompletedEventDelegate handler)
        {
            SubscriberIDs = Keystone.Extensions.ArrayExtensions.ArrayAppend(SubscriberIDs, subscriberID);
            Handlers = Keystone.Extensions.ArrayExtensions.ArrayAppend(Handlers, handler);
        }
    }
}
