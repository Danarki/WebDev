using Microsoft.AspNetCore.Mvc;
using WebDev.Models.ViewModels;
using WebDev.Models;
using MySqlX.XDevAPI;

namespace WebDev.Controllers
{
    public class LobbyController : Controller
    {
        private WebAppContext _context;

        public LobbyController(WebAppContext context)
        {
            _context = context;
        }

        public void EnableAllPlayers(int GameID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                List<ConnectedUser> user = db.ConnectedUsers.Where(x => x.GameID == GameID).ToList();

                foreach (ConnectedUser cu in user)
                {
                    cu.IsDisabled = false;
                }

                db.SaveChanges();
            }
        }

        public IActionResult Game(int id)
        {
            GameViewModel gameViewModel = new GameViewModel();

            ConnectedUser user = _context.ConnectedUsers
                .Where(x => x.RoomID == id && x.UserID == HttpContext.Session.GetInt32("UserID")).FirstOrDefault();

            // check if user is in lobby and logged in
            if (user == null || (int)HttpContext.Session.GetInt32("UserID") == null)
            {
                return Redirect("/");
            }

            gameViewModel.UserID = user.UserID;

            gameViewModel.LobbyID = id;

            gameViewModel.UserToken = user.AuthToken;

            HttpContext.Session.SetInt32("InGame", 1);

            return View(gameViewModel);
        }

        public IActionResult Index(int id)
        {
            // check if room exists
            GameRoom room = _context.GameRooms.Where(x => x.ID == id).FirstOrDefault();

            if (room == null)
            {
                return Redirect("/");
            }

            string? token = null;

            // check if user is owner of lobby, then set owner token
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
