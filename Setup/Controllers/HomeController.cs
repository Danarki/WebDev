using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Dynamic;
using System.Runtime.InteropServices.JavaScript;
using MySqlConnector;
using System.Security.Policy;
using Microsoft.AspNetCore.Mvc.Filters;
using WebDev.Models;
using WebDev.Models.ViewModels;
using MySqlX.XDevAPI;
using Microsoft.AspNetCore.Http;

namespace WebDev.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private const string PageViews = "PageViews";

        private Developer _developer;

        private WebAppContext _context;

        private List<GameRoom> _gameRooms;

        public void IncreaseTrackerCookie()
        {
            string newValue = "1";

            if (Request.Cookies["tracker-cookie"] != null)
            {
                newValue = (int.Parse(Request.Cookies["tracker-cookie"]) + 1).ToString();
            }

            HttpContext.Response.Cookies.Append("tracker-cookie", newValue);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            IncreaseTrackerCookie();

           //if (HttpContext.Session.GetInt32("LoggedIn") != 1)
           //{
           //    RouteValueDictionary route = new RouteValueDictionary(new
           //    {
           //        Controller = "User",
           //        Action = "Index"
           //    });
           //
           //   // context.Result = new RedirectToRouteResult(route);
           //
           //    return;
           //}
        }

        public HomeController(ILogger<HomeController> logger, WebAppContext context)
        {
            _logger = logger;

            _context = context;

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
            @ViewData["CurrentPage"] = "Home";

            return View();
        }

        public IActionResult Profile()
        {
            @ViewData["CurrentPage"] = "Profile page";

            return View(_developer);
        }

        public IActionResult Contact()
        {
            @ViewData["CurrentPage"] = "Contact";

            int num1, num2, solution;

            Random random = new Random(Guid.NewGuid().GetHashCode());
            num1 = random.Next(1, 10);
            num2 = random.Next(1, 10);

            solution = num1 * num2;

            @ViewData["captcha-num1"] = num1;
            @ViewData["captcha-num2"] = num2;
            @ViewData["captcha-solution"] = solution;

            return View(_developer);
        }

        public IActionResult Privacy()
        {
            @ViewData["CurrentPage"] = "Privacy";

            return View();
        }

        public IActionResult LobbyOverview()
        {
            HttpContext.Session.SetInt32("LoggedIn", 1);
            HttpContext.Session.SetInt32("UserID", 2);

            @ViewData["CurrentPage"] = "Lobby's";

            var gameRooms = _context.GameRooms.ToList();

            List<LobbyOverviewViewModel> lobbyViewModels = new List<LobbyOverviewViewModel>();
            foreach (GameRoom gameRoom in gameRooms)
            {
                LobbyOverviewViewModel lobbyViewModel = new LobbyOverviewViewModel();

                lobbyViewModel.Game = 
                    _context.GameTypes.Where(x => x.ID == gameRoom.GameID).FirstOrDefault().Name ?? "Niet gevonden!";
                lobbyViewModel.Name = gameRoom.Name;
                lobbyViewModel.Id = gameRoom.ID;
                lobbyViewModel.OwnerName =
                    _context.Users.Where(x => x.ID == gameRoom.OwnerID).FirstOrDefault().Username ?? "Niet gevonden!";

                lobbyViewModels.Add(lobbyViewModel);
            }

            dynamic model = new ExpandoObject();

            model.LobbyRows = lobbyViewModels;

            model.GameTypes = _context.GameTypes.ToList();

            model.UserID = HttpContext.Session.GetInt32("UserID");

            return View(model);
        }

        public IActionResult Lobby(int? id)
        {
            HttpContext.Session.SetInt32("LoggedIn", 1);
            HttpContext.Session.SetInt32("UserID", 2);
            id = 3;
            //if (id == null)
            //{
            //    return Redirect("/");
            //}

            int? UserId = HttpContext.Session.GetInt32("UserID");
            GameRoom room = _context.GameRooms.Where(x => x.ID == id).FirstOrDefault();

            if (UserId == null || room == null)
            {
                return Redirect("/");
            }


            ConnectedUser connectedUser = new ConnectedUser();
            connectedUser.UserID = (int)UserId;
            connectedUser.RoomID = (int)id;

            connectedUser.Insert(_context);

            LobbyViewModel lobbyViewModel = new LobbyViewModel();
            List<ConnectedUser> connectedUsers = _context.ConnectedUsers.Where(x => x.RoomID == id).ToList();

            List<User> userList = new List<User>();

            foreach (var user in connectedUsers)
            {
                User foundUser = _context.Users.Where(x => x.ID == user.UserID)
                    .Select(u => new User() 
                    { 
                        ID = u.ID, 
                        Username = u.Username
                    }).FirstOrDefault();
                
                if (foundUser != null)
                    userList.Add(foundUser);
            }

            lobbyViewModel.UserList = userList;

            lobbyViewModel.OwnerID = room.OwnerID;

            return View(lobbyViewModel);
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