public namespace Game01.Builders
{

    public interface IBuilder
    {

        public string BuildPersistString {get;}
        public bool StatsChanged {get;}
        public bool BuildChanged {get;};


        public void Update();

        public string ToString (IBuilder b);
        public IBuilder FromString(string persistString);
        

        
    }




}