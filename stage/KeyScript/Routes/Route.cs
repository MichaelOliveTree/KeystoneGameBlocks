using System;
using Keystone.Extensions;
using System.Collections.Generic ;

namespace KeyScript.Routes
{
    //public class RoutingTable
    //{
    //    private static List<Route> m;

    //    public static void Add(Route route)
    //    {
 
    //    }

    //}

    public class RouteEndPoint
    {
        public Route Route;
        public string EntityID;
        public string MethodName;
        public object Entity;     // managed only by Entity, not by Scripts
    }

    // A route just seems to be a mechanism where an Entity's script can route events to another Entity's script.
    // This is similar to Events except instead of the other Entity "subscribing" to another Entity's events, a Route
    // has the initial Entity saying "i also want this event sent to another Entity(s)."

    // NOTE: the Route describes the basic configuration and uses just strings
    // in the endpoint, no entity references.  This way we can easily restore
    // the endpoints during deserialization.  
    public class Route
    {
        public RouteEndPoint Source;
        public RouteEndPoint Target;

        public Route(RouteEndPoint source, RouteEndPoint target)
        {
            if (source == null) throw new ArgumentNullException();

            Source = source;
            source.Route = this;

            if (target != null)
            {
                Target = target;
                Target.Route = this;
            }
        }
    }


    public class RouteSet
    {
        private Route[] mRoutes;

        public Route[] Routes { get { return mRoutes; } }

        public void AddRoute(Route route)
        {
            mRoutes = mRoutes.ArrayAppend(route);
        }
    }
}
