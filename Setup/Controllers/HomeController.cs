using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc.Filters;
using WebDev.Models;
using WebDev.Models.ViewModels;
using System;
using System.Text;
using Google.Authenticator;
namespace WebDev.Controllers
{
    public class HomeController : Controller
    {
        private const string PageViews = "PageViews";

        private WebAppContext _context;

        // cookie tracker for assignment, tracks cookies
        public void IncreaseTrackerCookie()
        {
            string newValue = "1";

            if (Request.Cookies["tracker-cookie"] != null)
            {
                newValue = (int.Parse(Request.Cookies["tracker-cookie"]) + 1).ToString();
            }

            HttpContext.Response.Cookies.Append("tracker-cookie", newValue);
        }

        // this gets executed on every redirect
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            IncreaseTrackerCookie();

            int? loggedIn = HttpContext.Session.GetInt32("LoggedIn");
            object? routeData = context.RouteData.Values.Values.Skip(1).First();

            if (routeData.ToString() == "Privacy") // users may always navigate to the privacy page
            {
                return;
            }

            if (loggedIn != 1) // user is not logged in, redirect to login
            {
                RouteValueDictionary route = new RouteValueDictionary(new
                {
                    Controller = "User",
                    Action = "Index"
                });

                context.Result = new RedirectToRouteResult(route);
            }
        }

        public HomeController(WebAppContext context)
        {
            _context = context;
        }

        public IActionResult RoleManagement()
        {
            // check if role is not below moderator
            int? userRole = HttpContext.Session.GetInt32("Role");
            if (userRole == null || userRole <= 1)
            {
                return Redirect("/");
            }

            @ViewData["CurrentPage"] = "Role Management";

            // get all users and format user data to be readable
            List<User> userList = new List<User>();
            using (WebAppContext db = new WebAppContext())
            {
                userList = db.Users.ToList();
            }

            foreach (User user in userList)
            {
                string role = Enum.GetName(typeof(Role), user.Role);

                user.RoleName = role;
                user.RoleValue = (int)user.Role;
            }

            return View(userList);
        }

        public IActionResult Index()
        {
            @ViewData["CurrentPage"] = "Home";

            return View();
        }

        public IActionResult Profile()
        {
            @ViewData["CurrentPage"] = "Profile page";

            // could be used with different developers from database if more developers join in on creating this app
            return View(new Developer
            {
                FullName = "Daan Timmerman",
                Description =
                    "Daan Timmerman is a developer and student at Windesheim HBO-ICT. For his Web Development semester it seemed fun to make a website where users could play cardgames together. That's how Cardigo started! :)",
                ImageSrcs = new List<string>
                {
                    "/images/person-icon.png",
                    "/images/second-image-placeholder.jpg",
                },
                Skills = new List<string>
                {
                    "Software Developer",
                    "Fast learner",
                    "Speaks chinese on basic level",
                    "Beer enthousiast",
                }
            });
        }

        public IActionResult Contact()
        {
            @ViewData["CurrentPage"] = "Contact";

            // generate random equation for captcha
            int num1, num2, solution;

            Random random = new Random(BCrypt.Net.BCrypt.GenerateSalt().GetHashCode());
            num1 = random.Next(1, 10);
            num2 = random.Next(1, 10);

            solution = num1 * num2;

            @ViewData["captcha-num1"] = num1;
            @ViewData["captcha-num2"] = num2;
            @ViewData["captcha-solution"] = solution;

            return View();
        }

        public IActionResult Privacy()
        {
            @ViewData["CurrentPage"] = "Privacy";

            return View();
        }

        public IActionResult LobbyOverview()
        {
            @ViewData["CurrentPage"] = "Lobby's";

            // gather all romms that have not started playing yet
            List<GameRoom> gameRooms = _context.GameRooms.Where(x => !x.HasStarted).ToList();
            List<LobbyOverviewViewModel> lobbyViewModels = new List<LobbyOverviewViewModel>();

            foreach (GameRoom gameRoom in gameRooms)
            {
                LobbyOverviewViewModel lobbyViewModel = new LobbyOverviewViewModel();

                //Below line is obsolete, but remains for eventual later repurposing when multiple game types are implemented.
                //GameType type = _context.GameTypes.Where(x => x.ID == gameRoom.ID).FirstOrDefault();

                User owner = _context.Users.Where(x => x.ID == gameRoom.OwnerID).FirstOrDefault();

                lobbyViewModel.Game = "Blackjack"; // static blackjack for now
                lobbyViewModel.Name = gameRoom.Name;
                lobbyViewModel.Id = gameRoom.ID;

                string ownerName = "Niet gevonden!";

                if (owner != null)
                {
                    ownerName = owner.Username;
                }

                lobbyViewModel.OwnerName = ownerName;

                lobbyViewModels.Add(lobbyViewModel);
            }

            dynamic model = new ExpandoObject();

            model.LobbyRows = lobbyViewModels;

            model.GameTypes = _context.GameTypes.ToList();

            model.UserID = HttpContext.Session.GetInt32("UserID");

            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public void UpdatePageViewCookie()
        {
            string? currentCookieValue = Request.Cookies[PageViews];

            if (currentCookieValue == null)
            {
                Response.Cookies.Append(PageViews, "1");
            }
            else
            {
                int newCookieValue = short.Parse(currentCookieValue) + 1;

                Response.Cookies.Append(PageViews, newCookieValue.ToString());
            }
        }
    }
}