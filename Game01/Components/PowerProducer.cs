namespace Game01.Components
{

    public struct PowerProducer 
    {
        public int Interfaces; // 32 bit flags for the various interfaces (Build and Runtime) used by this component
        public string EntityID; 

        /*
        Definition: 1 kWh == 1 kW of power sustained for 1 hour.
            Usage Example: A 2,500-watt clothes dryer used for 2 hours consumes 5 kWh (2.5kW x 2  hours).
        Average Consumption: The average U.S. household consumes approximately 899 kWh per month, or about 30 kWh per day.
        */
        public double Output;    // kWh    
    
    }
}