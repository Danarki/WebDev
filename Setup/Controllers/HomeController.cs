using Microsoft.AspNetCore.Mvc;
using Setup.Models;
using System.Diagnostics;

namespace Setup.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private const string PageViews = "PageViews";

        private Developer _developer;
        public void IncreaseTrackerCookie()
        {
            string newValue = "1";

            if (Request.Cookies["tracker-cookie"] != null)
            {
               newValue =  (int.Parse(Request.Cookies["tracker-cookie"]) + 1).ToString();
            }

            HttpContext.Response.Cookies.Append("tracker-cookie", newValue);
        }

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _developer = new Developer
            {
                FullName = "Daan Timmerman",
                Description = "Charles Robert Darwin (12 februari 1809 – 19 april 1882) was een Engels autodidact op het gebied van natuurlijke historie, biologie en geologie. Darwin ontleent zijn roem aan zijn theorie dat evolutie van soorten wordt gedreven door natuurlijke selectie. Het bestaan van evolutie werd omstreeks 1850 al door een groot deel van de wetenschappelijke gemeenschap geaccepteerd. De acceptatie van natuurlijke selectie als aandrijvend mechanisme liet langer op zich wachten, maar geldt tegenwoordig als onomstreden.",
                ImageSrcs = new List<string>{
                    "/images/person-icon.png",
                    "/images/Greg.jpg",
                },
                Skills = new List<string>
                {
                    "Fast learner",
                    "Greg Paul enjoyer",
                    "Chef"
                }
            };
        }

        public IActionResult Index()
        {
            IncreaseTrackerCookie();
            @ViewData["CurrentPage"] = "Home";
            return View();
        }

        public IActionResult Profile()
        {
            IncreaseTrackerCookie();
            @ViewData["CurrentPage"] = "Profile page";

            return View(_developer);
        }

        public IActionResult Contact()
        {
            IncreaseTrackerCookie();
            @ViewData["CurrentPage"] = "Contact";

            return View(_developer);
        }

        public IActionResult Privacy()
        {
            IncreaseTrackerCookie();
            @ViewData["CurrentPage"] = "Privacy";

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public void UpdatePageViewCookie()
        {
            var currentCookieValue = Request.Cookies[PageViews];

            if (currentCookieValue == null)
            {
                Response.Cookies.Append(PageViews, "1");
            }
            else
            {
                var newCookieValue = short.Parse(currentCookieValue) + 1;

                Response.Cookies.Append(PageViews, newCookieValue.ToString());
            }
        }
    }
}