using System.Data.Entity;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class GameRoomHub : Hub
    {
        public int? userId { get; set; }
        public override Task OnConnectedAsync()
        {
            //using (var db = new WebAppContext())
            //{
            //    var context = Context.GetHttpContext();
            //    var i = context.Session.GetInt32("UserID"); ;
            //    var user = db.Users
            //        .Include(u => u.Rooms)
            //        .SingleOrDefault(u => u.ID == i);

            //            //    if (user != null)
            //    {
            //        foreach (GameRoom room in user.Rooms)
            //        {
            //            Groups.AddToGroupAsync(Context.ConnectionId, room.Name);
            //        }
            //    }
            //}

            //playerJoined();

            return base.OnConnectedAsync();
        }

        public async Task DeleteRoom(string token)
        {

        }

        public async Task Ping(int groupID)
        {
            await Clients.Group(groupID.ToString()).SendAsync("PingReturn", "pong");
        }

        public async Task GetGroupUsers(int groupID)
        {
            using (var db = new WebAppContext())
            {
                var connections = db.ConnectedUsers.Where(x => x.RoomID.ToString() == groupID.ToString()).ToList();

                if (connections != null)
                {
                    List<string> userNames = new List<string>();

                    foreach (var connection in connections)
                    {
                        var username = db.Users.Where(x => x.ID == connection.UserID).FirstOrDefault();

                        if (username != null)
                        {
                            userNames.Add(username.Username);
                        }
                    }
                    
                    string json = JsonConvert.SerializeObject(userNames);
                    await Clients.Group(groupID.ToString()).SendAsync("playerList", json);
                }
            }
        }

        public async Task playerJoined(string playerName, string groupID)
        {
            await Clients.Group(groupID).SendAsync("playerJoined", playerName + " joined the room");
        }

        public async Task playerLeft(string playerName, string groupID)
        {
            await Clients.Group(groupID).SendAsync("playerLeft", playerName + " left the room");
        }

        public void AddToRoom(int roomID)
        {
            HttpContext context = new HttpContextAccessor().HttpContext;

            using (var db = new WebAppContext())
            {
                var room = db.GameRooms.Where(x => x.ID == roomID).FirstOrDefault();

                if (room != null)
                {
                    var userId = context.Session.GetInt32("UserID");

                    User user = db.Users.Where(x => x.ID == userId).FirstOrDefault();

                    if (user != null)
                    {
                        ConnectedUser c = new ConnectedUser();
                        c.RoomID = roomID;
                        c.UserID = (int)userId;

                        var connectedCheck = db.ConnectedUsers.Where(x => x.UserID == userId && x.RoomID == roomID)
                            .FirstOrDefault();

                        if (connectedCheck == null)
                        {
                            db.ConnectedUsers.Add(c);
                            db.SaveChanges();
                        }

                        Groups.AddToGroupAsync(Context.ConnectionId, roomID.ToString());

                        playerJoined(user.Username, roomID.ToString());

                        GetGroupUsers(roomID);
                    }
                }
            }
        }

        public void RemoveFromRoom(int roomID)
        {
            HttpContext context = new HttpContextAccessor().HttpContext;

            using (var db = new WebAppContext())
            {
                var room = db.GameRooms.Where(x => x.ID == roomID).FirstOrDefault();

                if (room != null)
                {
                    var userId = context.Session.GetInt32("UserID");

                    User user = db.Users.Where(x => x.ID == userId).FirstOrDefault();

                    if (user != null)
                    {
                        ConnectedUser c = new ConnectedUser();
                        c.RoomID = roomID;
                        c.UserID = (int)userId;

                        var connectedCheck = db.ConnectedUsers.Where(x => x.UserID == userId && x.RoomID == roomID)
                            .FirstOrDefault();

                        if (connectedCheck != null)
                        {
                            db.ConnectedUsers.RemoveRange(
                                db.ConnectedUsers.Where(
                                    x => x.UserID == userId && x.RoomID == roomID
                                )
                            );

                            db.SaveChanges();
                        }

                        Groups.RemoveFromGroupAsync(Context.ConnectionId, roomID.ToString());

                        playerLeft(user.Username, roomID.ToString());
                    }
                }
            }
        }
    }
}
