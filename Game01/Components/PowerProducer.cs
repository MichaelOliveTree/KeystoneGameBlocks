namespace Game01.Components
{

    public struct PowerProducer 
    {
        public int Interfaces; // 32 bit flags for the various interfaces (Build and Runtime) used by this component
        public string EntityID; // Guid.NewGuid().ToString() results in a 36 character string.
             
    }
}