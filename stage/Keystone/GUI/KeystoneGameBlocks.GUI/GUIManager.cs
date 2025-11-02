using Core.Controllers;

namespace GUI
{    
    public class GUIController : Controller // e.g. a type of "GUI Controller" like the FPSController and Edit Controllers
    {
        public static Core.Core Core;

        public GUIController(Core.Core core)
        {
            Core = core;
        }
        
    }
}
