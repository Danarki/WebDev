using Microsoft.AspNetCore.Mvc;
using WebDev.Models.ViewModels;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class LobbyController : Controller
    {
        private readonly ILogger<LobbyController> _logger;

        private WebAppContext _context;

        public LobbyController(ILogger<LobbyController> logger, WebAppContext context)
        {
            _logger = logger;

            _context = context;
        }

        public IActionResult Index(int id)
        {
           //HttpContext.Session.SetInt32("LoggedIn", 1);
           //HttpContext.Session.SetInt32("UserID", 2);

            GameRoom room = _context.GameRooms.Where(x => x.ID == id).FirstOrDefault();

            if (room == null)
            {
                return Redirect("/");
            }

            string? token = null;

            if (HttpContext.Session.GetInt32("UserID") == room.OwnerID)
            {
                token = room.OwnerToken;
            }

            LobbyViewModel lobbyViewModel = new LobbyViewModel();

            lobbyViewModel.RoomID = id;

            lobbyViewModel.OwnerID = room.OwnerID;

            lobbyViewModel.AdminToken = token;

            return View(lobbyViewModel);
        }
    }
}
