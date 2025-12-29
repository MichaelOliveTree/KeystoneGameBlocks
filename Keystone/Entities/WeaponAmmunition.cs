public class clsWeaponAmmunition
{
    private float mvarWPS;
    private float mvarCPS;
    private float mvarVPS;
    private float mvarBoreSize;
    private string mvarAmmunitionType;
    private bool mvarLocked;
   
    private double mvarWeight;
    private double mvarCost;
    private double mvarVolume;
    private string mvarLocation;
    private string mvarParent;
    private string mvarKey;
    private int mvarDR;
    private bool mvarRuggedized;
    private double mvarSurfaceArea;
    private double mvarHitPoints;
    private short mvarTL;
    private short mvarDatatype;
    private short mvarParentDatatype;
    private string mvarDescription;
    private string mvarCustomDescription;
    private bool mvarCustom;
    private int mvarNumShots;
    private short mvarImage;
    private short mvarSelectedImage;
    private string mvarComment;
    private string mvarCName;
    private string mvarPrintOutput;
    private byte mvarZZInit;
    private string mvarLogicalParent;
   
    public string LogicalParent {
        get { LogicalParent = mvarLogicalParent; }
        set { mvarLogicalParent = value; }
    }
      
    public string PrintOutput {
        get { PrintOutput = mvarPrintOutput; }
        set { mvarPrintOutput = value; }
    }
   
    public string CName {
        get { CName = mvarCName; }
        set { mvarCName = value; }
    }

    public string Comment {
        get { Comment = mvarComment; }
        set { mvarComment = value; }
    }
      
    public short SelectedImage {
        get { SelectedImage = mvarSelectedImage; }
        set { mvarSelectedImage = value; }
    }   
   
    public short Image {
        get { Image = mvarImage; }
        set { mvarImage = value; }
    }  
   
    public bool Locked {
        get { Locked = mvarLocked; }
        set { mvarLocked = value; }
    }
    
    public string AmmunitionType {
        get { AmmunitionType = mvarAmmunitionType; }
        set { mvarAmmunitionType = value; }
    }
   
    public float BoreSize {
        get { BoreSize = mvarBoreSize; }          
        set { mvarBoreSize = value; }
    }
     
    public double CPS {
        get { CPS = mvarCPS; }
        set { mvarCPS = value; }
    }

    public double VPS {
        get { VPS = mvarVPS; }
        set { mvarVPS = value; }
    } 
   
    public double WPS {
        get { WPS = mvarWPS; }
        set { mvarWPS = value; }
    }

    public int NumShots {
        get { NumShots = mvarNumShots; }
        set { mvarNumShots = value; }
    }

    public bool Custom {
        get { Custom = mvarCustom; }
        set { mvarCustom = value; }
    }

    public string CustomDescription {
        get { CustomDescription = mvarCustomDescription; }
        set { mvarCustomDescription = value; }
    }

    public string Description {
        get { Description = mvarDescription; }
        set { mvarDescription = value; }
    }

    public short ParentDatatype {
        get { ParentDatatype = mvarParentDatatype; }
        set { mvarParentDatatype = value; }
    }
 
    public short Datatype {
        get { Datatype = mvarDatatype; }
        set { mvarDatatype = value; }
    }
 
    public short TL {
        get { TL = mvarTL; }
        set { mvarTL = value; }
    }

    public double HitPoints {
        get { HitPoints = mvarHitPoints; }
        set { mvarHitPoints = value; }
    }
   
    public double SurfaceArea {
        get { SurfaceArea = mvarSurfaceArea; }
        set { mvarSurfaceArea = value; }
    }

    public bool Ruggedized {
        get { Ruggedized = mvarRuggedized; }
        set { mvarRuggedized = value; }
    }
   
    public int DR {
        get { DR = mvarDR; }
        set { mvarDR = value; }
    }
   
    public string Key {
        get { Key = mvarKey; }
        set { mvarKey = value; }
    }
   
    public string Parent {
        get { Parent = mvarParent; }
        set { mvarParent = value; }
    }

    public string Location {
        get { Location = mvarLocation; }
        set { mvarLocation = value; }
    }
   
    public double Volume {
        get { Volume = mvarVolume; }
        set { mvarVolume = value; }
    }
   
    public double Cost {
        get { Cost = mvarCost; }
        set { mvarCost = value; }
    }

    public double Weight {
        get { Weight = mvarWeight; }
        set { mvarWeight = value; }
    }
      
    public bool LocationCheck()
    {
        //UPGRADE_NOTE: Module was upgraded to Module_Renamed. Click for more: 'ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
        object Module_Renamed;
        object Leg;
        object OpenMount;
        object Wing;
        object Arm;
        object Popturret;
        object Turret;
        object equipmentPod;
        object Pod;
        object Superstructure;
        object Body;
        object GroupComponent;
        object InfoPrint;
        object Veh;
        bool TempCheck;
        short InstallPoint;
       
        InstallPoint = Veh.Components(mvarParent).Datatype;
       
        if ((InstallPoint == GroupComponent) | (Veh.Components(mvarParent) is clsWeaponGun) | (InstallPoint == Body) | (InstallPoint == GroupComponent) | (InstallPoint == Superstructure) | (InstallPoint == Pod) | (InstallPoint == equipmentPod) | (InstallPoint == Turret) | (InstallPoint == Popturret) | (InstallPoint == Arm) | (InstallPoint == Wing) | (InstallPoint == OpenMount) | (InstallPoint == Leg) | (InstallPoint == Module_Renamed)) {
            TempCheck = true;
        }
        else {
             InfoPrint(1, "Ammunition must be assigned to an Artillery piece for stats. Later it can be placed in Body, Superstructure, Pod, equipment Pod,Turret, Popturret, Arm, Wing, Open Mount, Leg or Module.");
             TempCheck = false;
         }
        
         if (TempCheck)
             SetLogicalParent();
         return TempCheck;
     }
    
    
     private string GetLocation()
     {
         object Veh;
         // ERROR: Not supported in C: OnErrorStatement
         if (mvarLogicalParent == "")
             SetLogicalParent();
        
         return Veh.Components(mvarLogicalParent).Abbrev;
        
     }
    
     public void SetLogicalParent()
     {
         object GetLogicalParent;
        
         mvarLogicalParent = GetLogicalParent(mvarParent);
     }
    
    
    
     //UPGRADE_NOTE: Class_Initialize was upgraded to Class_Initialize_Renamed. Click for more: 'ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
     private void Class_Initialize_Renamed()
     {
         // set the default properties
         mvarCustom = false;
         mvarNumShots = 100;
     }
     public clsWeaponAmmunition() : base()
     {
         Class_Initialize_Renamed();
     }
    
     //UPGRADE_NOTE: Class_Terminate was upgraded to Class_Terminate_Renamed. Click for more: 'ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?keyword="A9E4979A-37FA-4718-9994-97DD76ED70A7"'
     private void Class_Terminate_Renamed()
     {
         //the class is being destroyed
     }
     protected override void Finalize()
     {
         Class_Terminate_Renamed();
         base.Finalize();
     }
    
     public void Init()
     {
        
        
     }
    
     public void StatsUpdate()
     {
         object p_sFormat;
         object CalcComponentHitpoints;
         object CalcSurfaceArea;
         object Veh;
        
         mvarZZInit = 1;
        
        
         mvarLocation = GetLocation;
        
         if (mvarLocked == false) {
            
             if (Veh.Components(mvarParent) is clsWeaponGun) {
                
                 mvarWPS = Veh.Components(mvarParent).WPS;
                
                 mvarCPS = Veh.Components(mvarParent).CPS;
                
                 mvarVPS = Veh.Components(mvarParent).VPS;
                
                 mvarBoreSize = Veh.Components(mvarParent).BoreSize;
                
                 mvarAmmunitionType = Veh.Components(mvarParent).AmmunitionType;
                
                 mvarWeight = System.Math.Round(mvarWPS * mvarNumShots, 2);
                 mvarCost = System.Math.Round(mvarCPS * mvarNumShots, 2);
                 mvarVolume = System.Math.Round(mvarVPS * mvarNumShots, 2);
                
                 mvarSurfaceArea = CalcSurfaceArea(mvarVolume);
                
                 mvarHitPoints = CalcComponentHitpoints(mvarSurfaceArea);
             }
         }
        
         mvarDescription = mvarNumShots + " x " + mvarBoreSize + "mm " + mvarAmmunitionType + " shots";
         if (mvarCustom) {
         }
         else {
             mvarDescription = mvarNumShots + " x " + mvarBoreSize + "mm " + mvarAmmunitionType + " shots";
             mvarCustomDescription = mvarDescription;
         }
        
        
         mvarPrintOutput = mvarCustomDescription + " (" + mvarLocation + ", HP " + mvarHitPoints + ", " + VB6.Format(mvarWeight, p_sFormat) + " lbs., " + VB6.Format(mvarVolume, p_sFormat) + " cf., " + "$" + VB6.Format(mvarCost, p_sFormat) + ")." + mvarComment;
        
     }
    
     public void QueryParent()
     {
         object Veh;
         // if the object has a parent, query it and check to see if
         // more stats/property updates are needed for other objects in the collection
         if (mvarParent != "") {
            
             Veh.Components(Parent).StatsUpdate();
         }
     }
 }
