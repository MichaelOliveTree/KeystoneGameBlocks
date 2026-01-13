namespace Game01.Components
{

    public struct PowerConsumer  
    {
        public int Interfaces; // 32 bit flags for the various interfaces (Build and Runtime) used by this component
        public string EntityID; // Guid.NewGuid().ToString() results in a 36 character string.
                
        public bool Breaker;
        public float PowerRequirement;// per tick or per-use if "Continuous == false:
		public bool Continuous; // whether this component always consumes power when operating
		public bool HasVariablePerformance; // can run at reduced power, but with reduced performance (eg sensor will have lower range)
        public float MinimumPower;
        public float Priority;  // determines if there's insufficient power production, which consumers get higher priority to be powered during runtime 

		

        // runtime
        public float BreakerCycleDuration;
		public float StartTime;
		public float Duration;
		public bool Looping; // Repeating
		public float CooldownDuration; 
		public bool InCoolDown;
    }
}