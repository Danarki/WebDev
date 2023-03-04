using Microsoft.AspNetCore.Mvc;

namespace WebDev.Controllers
{
    public class DatabaseController : Controller
    {
        public static WebAppContext Context { get; 
            set; }
    }
}
