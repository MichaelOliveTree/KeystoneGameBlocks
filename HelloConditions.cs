using System;
using System.Collections.Generic;


public class HelloConditions
{
    static public void Main ()
    {
        Condition c = new Condition ();
        c.operandLeft = 25;
        c.operandRight = 50;
       
        bool result = c.Evaluate();
        Console.WriteLine ("HelloConditional - " + result.ToString()); 
    }
}



#region Game01.GameObjects
public class UnitedEarthCode
{
    
    // Order1


}

public struct CrewMemberServiceRecord
{
    
    
}

// combat specific, not diplomatic 
public class ExecutiveDirectives
{
    // Keystone.Simulation.Missions.Mission
	// Keystone.Simulaton.Missions.MissionData
	// Keystone.Simulation.Missions.Objective
	
    public Mission Mission;
    public Orders Orders;
    
    // Game01.GameObjects.ExecutiveDirectives.RulesOfEngagement
    public struct RulesOfEngagement
    {
        public bool FireOnFreighters; // usually always false
		public bool RetreatRatherThanFightIfPossible;
			//      - never fire first except during wartime
			//		- diplomacy first unless state of war
			//      - never fire on disabled ships or otherwise  non-threats
			//		- pre-emptive policy
			//		- disable priority
			//			- shields
			//			- weapons
			//			- engines
			//		- proportiality / proportional response
			//		- nuclear weapons only to deter opposing nuclear threat only (some ships may have a mission of always staying hidden and running silent and nuclear deterences in case of an attack on homeworld and homeworld is destroyed, the retaliatory strike option will still exist to carry out its mission
			//		- 
			
		
    }
    
    // Readyness, CapacityToAct;
    // CapabilityAssessment;
    // OutcomeAssessment;
       
    
}

#endregion // Game01.GameObjects
	
public class Query
{
	// should NOT need to be concurrent, correct?
	Dictionary<string, object> mKVPs = new Dictionary<string, object>();


	public void Add(string name, object value)
	{
		if (mKVPs == null) mKVPs new Dictionary<string, object>();
		
		mKVPs.Add (name, value);
	}

	
}


/// <summary>
/// Rules should be sorted from highest number of Conditions to lowest so that we always test against highest number first so we can potentially early-exit
/// <summary>
public class Rule
{
	public string Concept;
	public string Description;
	public Condition[] Conditions;
	public Response Response;
	public Remember Remember;
	public Trigger Trigger;

	public void Add(Condition c)
	{
		// following is using Keystone namespace but actually is in KeyStandardLibrary
		// Keystone.Extensions.ArrayExtensions.	
	}

	public void Remove (Condition c)
	{

	}
}

public class Condition
{
    public string Name;
    public string Description;
    public int operandLeft;
    public int operandRight;
    public int evalType;
    
    // geater than
    public bool Evaluate()
    {
        switch (evalType)
        {
            case 0:
                 return operandLeft < operandRight;
                 
            case 1:
                 return operandLeft > operandRight;
    
            case 2:
                return operandLeft == operandRight;
			default:
				throw new ArgumentOutOfRangeException("Condition.Evaluate() - Unexpected evalType '" + evalType.ToString() + "'");
    }
}


public class Node
{
    public List<Condition> Conditions;
    
}
